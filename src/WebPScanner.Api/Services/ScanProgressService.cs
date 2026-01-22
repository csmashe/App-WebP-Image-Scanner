using Microsoft.AspNetCore.SignalR;
using WebPScanner.Api.Hubs;
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
    private readonly ILogger<ScanProgressService> _logger;

    // Note: IQueueService is not injected here to avoid circular dependency.
    // The BroadcastQueuePositionsAsync uses the repository directly.

    public ScanProgressService(
        IHubContext<ScanProgressHub, IScanProgressClient> hubContext,
        IScanJobRepository scanJobRepository,
        IAggregateStatsService aggregateStatsService,
        ILogger<ScanProgressService> logger)
    {
        _hubContext = hubContext;
        _scanJobRepository = scanJobRepository;
        _aggregateStatsService = aggregateStatsService;
        _logger = logger;
    }

    public async Task BroadcastQueuePositionsAsync(CancellationToken cancellationToken = default)
    {
        // Get the true total queue count (not limited by the 100-item fetch)
        var totalInQueue = await _scanJobRepository.GetQueuedCountAsync(cancellationToken);
        var queuedJobsEnumerable = await _scanJobRepository.GetQueuedJobsOrderedByPriorityAsync(100, cancellationToken);
        var queuedJobs = queuedJobsEnumerable.ToList();

        _logger.LogDebug("Broadcasting queue positions to {Count} queued jobs (total in queue: {Total})", queuedJobs.Count, totalInQueue);

        var position = 1;
        foreach (var job in queuedJobs)
        {
            var update = new QueuePositionUpdateDto
            {
                ScanId = job.ScanId,
                Position = position,
                TotalInQueue = totalInQueue
            };

            var groupName = ScanProgressHub.GetGroupName(job.ScanId);
            await _hubContext.Clients.Group(groupName).QueuePositionUpdate(update);

            position++;
        }
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
