using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPScanner.Core.Configuration;
using WebPScanner.Core.Entities;
using WebPScanner.Core.Enums;
using WebPScanner.Core.Interfaces;

namespace WebPScanner.Core.Services;

/// <summary>
/// Manages the scan job queue with fair-share scheduling.
/// Uses slot-based priority ordering where all first-time submitters are processed
/// before second-time submitters, with priority aging to prevent starvation.
/// </summary>
public class QueueService : IQueueService
{
    private readonly IScanJobRepository _scanJobRepository;
    private readonly QueueOptions _options;
    private readonly ILogger<QueueService> _logger;

    // In-memory cooldown tracking (IP -> cooldown expiration time)
    private static readonly ConcurrentDictionary<string, DateTime> IpCooldowns = new();

    public QueueService(
        IScanJobRepository scanJobRepository,
        IOptions<QueueOptions> options,
        ILogger<QueueService> logger)
    {
        _scanJobRepository = scanJobRepository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ScanJob> EnqueueAsync(ScanJob scanJob, CancellationToken cancellationToken = default)
    {
        // Calculate priority score using fair-share slot-based ordering:
        // - Primary key: SubmissionCount (lower = higher priority, ensures interleaving)
        // - Secondary key: CreatedAt (FIFO within the same slot)
        // This creates "slots" where all 1st jobs run before 2nd jobs, etc.
        var slotPriority = scanJob.SubmissionCount * _options.FairnessSlotTicks;
        var timePriority = scanJob.CreatedAt.Ticks;
        scanJob.PriorityScore = slotPriority + timePriority;
        scanJob.Status = ScanStatus.Queued;

        await _scanJobRepository.AddAsync(scanJob, cancellationToken);

        // Calculate queue position after adding
        scanJob.QueuePosition = await GetPositionAsync(scanJob.ScanId, cancellationToken);

        _logger.LogInformation(
            "Scan job {ScanId} enqueued at position {Position} (slot {Slot}, priority {Priority})",
            scanJob.ScanId, scanJob.QueuePosition, scanJob.SubmissionCount, scanJob.PriorityScore);

        return scanJob;
    }

    public async Task<ScanJob?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        var processingCount = await _scanJobRepository.GetProcessingCountAsync(cancellationToken);
        if (processingCount >= _options.MaxConcurrentScans)
        {
            _logger.LogDebug(
                "Cannot dequeue: {ProcessingCount}/{MaxConcurrent} scans currently processing",
                processingCount, _options.MaxConcurrentScans);
            return null;
        }

        var queuedJobs = await _scanJobRepository.GetQueuedJobsOrderedByPriorityAsync(1, cancellationToken);
        var nextJob = queuedJobs.FirstOrDefault();

        if (nextJob == null)
        {
            _logger.LogDebug("Queue is empty, nothing to dequeue");
            return null;
        }

        if (!string.IsNullOrEmpty(nextJob.SubmitterIp) && IsIpInCooldown(nextJob.SubmitterIp))
        {
            _logger.LogDebug(
                "Scan job {ScanId} submitter IP {IP} is in cooldown, skipping",
                nextJob.ScanId, nextJob.SubmitterIp);

            // Try to find another job not in cooldown
            var allQueued = await _scanJobRepository.GetQueuedJobsOrderedByPriorityAsync(_options.MaxQueueSize, cancellationToken);
            nextJob = allQueued.FirstOrDefault(j =>
                string.IsNullOrEmpty(j.SubmitterIp) || !IsIpInCooldownInternal(j.SubmitterIp));

            if (nextJob == null)
            {
                _logger.LogDebug("All queued jobs are from IPs in cooldown");
                return null;
            }
        }

        nextJob.Status = ScanStatus.Processing;
        nextJob.StartedAt = DateTime.UtcNow;
        nextJob.QueuePosition = 0;
        await _scanJobRepository.UpdateAsync(nextJob, cancellationToken);

        _logger.LogInformation(
            "Scan job {ScanId} dequeued and started processing for {Url}",
            nextJob.ScanId, nextJob.TargetUrl);

        return nextJob;
    }

    private async Task<int> GetPositionAsync(Guid scanId, CancellationToken cancellationToken = default)
    {
        return await _scanJobRepository.GetQueuePositionAsync(scanId, cancellationToken);
    }

    private async Task<int> GetQueueLengthAsync(CancellationToken cancellationToken = default)
    {
        return await _scanJobRepository.GetQueuedCountAsync(cancellationToken);
    }

    public async Task<bool> CanEnqueueAsync(CancellationToken cancellationToken = default)
    {
        var queueLength = await GetQueueLengthAsync(cancellationToken);
        return queueLength < _options.MaxQueueSize;
    }

    public async Task<bool> HasIpReachedQueueLimitAsync(string ip, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(ip))
            return false;

        // 0 means unlimited
        if (_options.MaxQueuedJobsPerIp <= 0)
            return false;

        var ipJobCount = await _scanJobRepository.GetJobCountByIpAsync(ip, cancellationToken);
        return ipJobCount >= _options.MaxQueuedJobsPerIp;
    }

    public async Task<IReadOnlyList<Guid>> RecalculatePrioritiesWithAgingAsync(CancellationToken cancellationToken = default)
    {
        var queuedJobs = (await _scanJobRepository.GetAllQueuedJobsAsync(cancellationToken)).ToList();
        if (queuedJobs.Count == 0)
            return [];

        // Capture old positions before recalculation
        var oldPositions = new Dictionary<Guid, int>();
        for (var i = 0; i < queuedJobs.Count; i++)
        {
            oldPositions[queuedJobs[i].ScanId] = i + 1;
        }

        // Recalculate priority scores with aging boost
        var now = DateTime.UtcNow;
        foreach (var job in queuedJobs)
        {
            var waitTime = now - job.CreatedAt;
            // Guard against non-positive PriorityAgingBoostSeconds to avoid division by zero
            var agingBoostTicks = _options.PriorityAgingBoostSeconds > 0
                ? (long)(waitTime.TotalSeconds / _options.PriorityAgingBoostSeconds) * TimeSpan.TicksPerSecond
                : 0;

            // Recalculate using fair-share slot-based ordering with aging boost
            // Slot priority ensures interleaving (1st jobs before 2nd jobs, etc.)
            // Aging boost prevents starvation of older jobs
            var slotPriority = job.SubmissionCount * _options.FairnessSlotTicks;
            var timePriority = job.CreatedAt.Ticks;
            job.PriorityScore = slotPriority + timePriority - agingBoostTicks;
        }

        // Update all jobs in the database
        await _scanJobRepository.UpdateManyAsync(queuedJobs, cancellationToken);

        // Re-fetch to get new order and determine which positions changed
        var reorderedJobs = (await _scanJobRepository.GetAllQueuedJobsAsync(cancellationToken)).ToList();
        var changedScanIds = new List<Guid>();

        for (var i = 0; i < reorderedJobs.Count; i++)
        {
            var job = reorderedJobs[i];
            var newPosition = i + 1;
            if (!oldPositions.TryGetValue(job.ScanId, out var oldPosition) || oldPosition == newPosition)
            {
	            continue;
            }

            changedScanIds.Add(job.ScanId);
            job.QueuePosition = newPosition;
        }

        if (changedScanIds.Count <= 0)
        {
	        return changedScanIds;
        }

        await _scanJobRepository.UpdateManyAsync(reorderedJobs, cancellationToken);
        _logger.LogInformation(
	        "Priority aging recalculated: {ChangedCount} job positions changed",
	        changedScanIds.Count);

        return changedScanIds;
    }

    public bool IsIpInCooldown(string ip)
    {
        return IsIpInCooldownInternal(ip);
    }

    private static bool IsIpInCooldownInternal(string ip)
    {
        // Clean up expired cooldowns
        var expiredKeys = IpCooldowns
            .Where(kvp => kvp.Value < DateTime.UtcNow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            IpCooldowns.TryRemove(key, out _);
        }

        if (IpCooldowns.TryGetValue(ip, out var expirationTime))
        {
            return expirationTime > DateTime.UtcNow;
        }

        return false;
    }

    public void RecordCooldown(string ip)
    {
        // Skip cooldown if disabled (0 seconds)
        if (_options.CooldownAfterScanSeconds <= 0)
            return;

        var cooldownExpiration = DateTime.UtcNow.AddSeconds(_options.CooldownAfterScanSeconds);
        IpCooldowns.AddOrUpdate(ip, cooldownExpiration, (_, _) => cooldownExpiration);

        _logger.LogDebug(
            "Recorded cooldown for IP {IP} until {Expiration}",
            ip, cooldownExpiration);
    }

    public async Task CompleteJobAsync(Guid scanId, bool success, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        var job = await _scanJobRepository.GetByIdAsync(scanId, cancellationToken);
        if (job == null)
        {
            _logger.LogWarning("Cannot complete scan job {ScanId}: job not found", scanId);
            return;
        }

        job.Status = success ? ScanStatus.Completed : ScanStatus.Failed;
        job.CompletedAt = DateTime.UtcNow;
        job.ErrorMessage = errorMessage;

        await _scanJobRepository.UpdateAsync(job, cancellationToken);

        // Record cooldown for the submitter IP
        if (!string.IsNullOrEmpty(job.SubmitterIp))
        {
            RecordCooldown(job.SubmitterIp);
        }

        _logger.LogInformation(
            "Scan job {ScanId} completed with status {Status}",
            scanId, job.Status);
    }
}
