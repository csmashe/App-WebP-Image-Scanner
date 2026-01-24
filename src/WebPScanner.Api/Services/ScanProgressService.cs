using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using WebPScanner.Api.Hubs;
using WebPScanner.Core.Configuration;
using WebPScanner.Core.DTOs;
using WebPScanner.Core.Interfaces;

namespace WebPScanner.Api.Services;

/// <summary>
/// Service for broadcasting scan progress updates via SignalR.
/// </summary>
public class ScanProgressService : IScanProgressService
{
    private readonly IHubContext<ScanProgressHub, IScanProgressClient> _hubContext;
    private readonly IScanJobRepository _scanJobRepository;
    private readonly IAggregateStatsService _aggregateStatsService;
    private readonly ILiveScanStatsTracker _liveScanStatsTracker;
    private readonly QueueOptions _queueOptions;
    private readonly ILogger<ScanProgressService> _logger;

    // Throttling for queue position broadcasts during page progress
    private DateTime _lastQueueBroadcast = DateTime.MinValue;
    private int _lastRemainingPages;
    private static readonly TimeSpan QueueBroadcastInterval = TimeSpan.FromSeconds(5);

    // Note: IQueueService is not injected here to avoid circular dependency.
    // The BroadcastQueuePositionsAsync uses the repository directly.

    public ScanProgressService(
        IHubContext<ScanProgressHub, IScanProgressClient> hubContext,
        IScanJobRepository scanJobRepository,
        IAggregateStatsService aggregateStatsService,
        ILiveScanStatsTracker liveScanStatsTracker,
        IOptions<QueueOptions> queueOptions,
        ILogger<ScanProgressService> logger)
    {
        _hubContext = hubContext;
        _scanJobRepository = scanJobRepository;
        _aggregateStatsService = aggregateStatsService;
        _liveScanStatsTracker = liveScanStatsTracker;
        _queueOptions = queueOptions.Value;
        _logger = logger;
    }

    public async Task BroadcastQueuePositionsAsync(CancellationToken cancellationToken = default)
    {
        // Get the true total queue count (not limited by the 100-item fetch)
        var totalInQueue = await _scanJobRepository.GetQueuedCountAsync(cancellationToken);
        var queuedJobsEnumerable = await _scanJobRepository.GetQueuedJobsOrderedByPriorityAsync(100, cancellationToken);
        var queuedJobs = queuedJobsEnumerable.ToList();

        _logger.LogDebug("Broadcasting queue positions to {Count} queued jobs (total in queue: {Total})", queuedJobs.Count, totalInQueue);

        // Get sorted remaining pages for active scans (closest to finishing first)
        var avgTimePerPageTicks = await _aggregateStatsService.GetAverageTimePerPageTicksAsync(cancellationToken);
        var activeScansRemaining = _liveScanStatsTracker.GetActiveScansRemainingPagesSorted();
        var defaultPagesPerSite = _queueOptions.DefaultEstimatedPagesPerSite;

        _logger.LogDebug("Queue wait estimate: avgTimePerPageTicks={AvgTicks}, activeScans={ActiveCount}, remainingPages=[{RemainingPages}], queuedJobs={QueuedCount}",
            avgTimePerPageTicks, activeScansRemaining.Count, string.Join(",", activeScansRemaining), queuedJobs.Count);

        var position = 1;
        foreach (var job in queuedJobs)
        {
            // Calculate estimated wait for this position using corrected formula
            var estimatedWaitSeconds = CalculateEstimatedWaitSeconds(
                position, activeScansRemaining, avgTimePerPageTicks, defaultPagesPerSite);

            var update = new QueuePositionUpdateDto
            {
                ScanId = job.ScanId,
                Position = position,
                TotalInQueue = totalInQueue,
                EstimatedWaitSeconds = estimatedWaitSeconds
            };

            var groupName = ScanProgressHub.GetGroupName(job.ScanId);
            await _hubContext.Clients.Group(groupName).QueuePositionUpdate(update);

            position++;
        }
    }

    /// <summary>
    /// Calculates estimated wait time for a queue position, accounting for parallel scan progress.
    /// </summary>
    /// <remarks>
    /// The calculation works as follows:
    /// - Queue #1 waits for the active scan closest to finishing (task1)
    /// - Queue #2 waits for Queue #1's time + min(task2 - task1, defaultPages) because task2 makes progress while waiting
    /// - Queue #3+ waits for previous queue's time + defaultPages (newly started scans estimated at default)
    /// </remarks>
    private static int? CalculateEstimatedWaitSeconds(
        int queuePosition,
        List<int> activeScansRemaining,
        long avgTimePerPageTicks,
        int defaultPagesPerSite)
    {
        if (avgTimePerPageTicks <= 0)
            return null;

        var activeCount = activeScansRemaining.Count;
        if (activeCount == 0)
        {
            // No active scans - this shouldn't happen normally, but handle it
            // Queue #1 starts immediately, subsequent positions wait for default pages each
            var pagesAhead = (queuePosition - 1) * defaultPagesPerSite;
            return (int)(avgTimePerPageTicks * pagesAhead / TimeSpan.TicksPerSecond);
        }

        // Track cumulative wait and simulate the queue progression
        long totalWaitPages = 0;

        // We need to track which "slots" are occupied and their remaining pages
        // Start with actual active scans, then add estimated pages for newly started scans
        var slots = new List<int>(activeScansRemaining);

        for (var pos = 1; pos <= queuePosition; pos++)
        {
            if (slots.Count == 0)
            {
                // No slots occupied - scan at this position starts immediately
                // Add default pages for this scan to slots
                slots.Add(defaultPagesPerSite);
                slots.Sort();
                continue;
            }

            // Wait for the slot with fewest remaining pages (will finish first)
            var task1Pages = slots[0];
            totalWaitPages += task1Pages;

            // All other slots make progress while we wait for task1
            for (var i = 1; i < slots.Count; i++)
            {
                slots[i] -= task1Pages;
            }

            // Remove the completed slot (task1 finished)
            slots.RemoveAt(0);

            // The scan at this position starts, add it with default estimated pages
            slots.Add(defaultPagesPerSite);
            slots.Sort();
        }

        return (int)(avgTimePerPageTicks * totalWaitPages / TimeSpan.TicksPerSecond);
    }

    public async Task SendScanStartedAsync(ScanStartedDto notification)
    {
        var groupName = ScanProgressHub.GetGroupName(notification.ScanId);

        _logger.LogInformation(
            "Scan started for {ScanId}: {TargetUrl}",
            notification.ScanId, notification.TargetUrl);

        await _hubContext.Clients.Group(groupName).ScanStarted(notification);
    }

    public async Task SendPageProgressAsync(PageProgressDto progress)
    {
        var groupName = ScanProgressHub.GetGroupName(progress.ScanId);

        _logger.LogDebug(
            "Page progress for scan {ScanId}: {PagesScanned}/{PagesDiscovered} ({ProgressPercent}%)",
            progress.ScanId, progress.PagesScanned, progress.PagesDiscovered, progress.ProgressPercent);

        await _hubContext.Clients.Group(groupName).PageProgress(progress);

        // Also broadcast queue position updates periodically so estimated wait time stays current
        await TryBroadcastQueuePositionsThrottledAsync();
    }

    private async Task TryBroadcastQueuePositionsThrottledAsync()
    {
        var now = DateTime.UtcNow;
        var currentRemainingPages = _liveScanStatsTracker.GetTotalRemainingPagesForActiveScans();

        // Broadcast if enough time has passed OR remaining pages changed significantly (by 5 or more)
        var timeSinceLastBroadcast = now - _lastQueueBroadcast;
        var pagesChanged = Math.Abs(currentRemainingPages - _lastRemainingPages);

        if (timeSinceLastBroadcast >= QueueBroadcastInterval || pagesChanged >= 5)
        {
            _lastQueueBroadcast = now;
            _lastRemainingPages = currentRemainingPages;

            try
            {
                await BroadcastQueuePositionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to broadcast queue positions during page progress");
            }
        }
    }

    public async Task SendImageFoundAsync(ImageFoundDto imageFound)
    {
        var groupName = ScanProgressHub.GetGroupName(imageFound.ScanId);

        _logger.LogDebug(
            "Image found for scan {ScanId}: {ImageUrl} ({MimeType}, {Size} bytes, NonWebP: {IsNonWebP})",
            imageFound.ScanId, imageFound.ImageUrl, imageFound.MimeType, imageFound.Size, imageFound.IsNonWebP);

        await _hubContext.Clients.Group(groupName).ImageFound(imageFound);
    }

    public async Task SendScanCompleteAsync(ScanCompleteDto notification)
    {
        var groupName = ScanProgressHub.GetGroupName(notification.ScanId);

        _logger.LogInformation(
            "Scan complete for {ScanId}: {TotalPagesScanned} pages, {NonWebPImagesCount} non-WebP images, duration: {Duration}",
            notification.ScanId, notification.TotalPagesScanned, notification.NonWebPImagesCount, notification.Duration);

        await _hubContext.Clients.Group(groupName).ScanComplete(notification);
    }

    public async Task SendScanFailedAsync(ScanFailedDto notification)
    {
        var groupName = ScanProgressHub.GetGroupName(notification.ScanId);

        _logger.LogWarning(
            "Scan failed for {ScanId}: {ErrorMessage}",
            notification.ScanId, notification.ErrorMessage);

        await _hubContext.Clients.Group(groupName).ScanFailed(notification);
    }

    public async Task BroadcastStatsUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _aggregateStatsService.GetCombinedStatsAsync(cancellationToken);

            _logger.LogInformation(
                "Broadcasting stats update: {TotalScans} scans, {TotalImages} images, {TotalSavings} bytes savings",
                stats.TotalScans, stats.TotalImagesFound, stats.TotalSavingsBytes);

            await _hubContext.Clients.Group(ScanProgressHub.StatsGroupName).StatsUpdate(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast stats update");
        }
    }
}
