using WebPScanner.Core.DTOs;

namespace WebPScanner.Api.Hubs;

/// <summary>
/// Client-side methods that can be invoked by the server.
/// This interface documents the methods clients should implement.
/// </summary>
public interface IScanProgressClient
{
    /// <summary>
    /// Called when the queue position for a scan changes.
    /// </summary>
    Task QueuePositionUpdate(QueuePositionUpdateDto update);

    /// <summary>
    /// Called when a scan starts processing.
    /// </summary>
    Task ScanStarted(ScanStartedDto notification);

    /// <summary>
    /// Called when progress is made scanning pages.
    /// </summary>
    Task PageProgress(PageProgressDto progress);

    /// <summary>
    /// Called when an image is found during scanning.
    /// </summary>
    Task ImageFound(ImageFoundDto imageFound);

    /// <summary>
    /// Called when a scan completes successfully.
    /// </summary>
    Task ScanComplete(ScanCompleteDto notification);

    /// <summary>
    /// Called when a scan fails.
    /// </summary>
    Task ScanFailed(ScanFailedDto notification);

    /// <summary>
    /// Called when aggregate stats are updated.
    /// </summary>
    Task StatsUpdate(AggregateStatsDto stats);
}
