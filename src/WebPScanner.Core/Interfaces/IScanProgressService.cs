using WebPScanner.Core.DTOs;

namespace WebPScanner.Core.Interfaces;

/// <summary>
/// Service for broadcasting scan progress updates to connected clients.
/// </summary>
public interface IScanProgressService
{
    /// <summary>
    /// Sends queue position updates to all clients in the queue.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastQueuePositionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Notifies clients that a scan has started.
    /// </summary>
    /// <param name="notification">The scan started notification data.</param>
    Task SendScanStartedAsync(ScanStartedDto notification);

    /// <summary>
    /// Sends page progress update to clients subscribed to a specific scan.
    /// </summary>
    /// <param name="progress">The page progress data.</param>
    Task SendPageProgressAsync(PageProgressDto progress);

    /// <summary>
    /// Sends image found notification to clients subscribed to a specific scan.
    /// </summary>
    /// <param name="imageFound">The image found notification data.</param>
    Task SendImageFoundAsync(ImageFoundDto imageFound);

    /// <summary>
    /// Notifies clients that a scan has completed successfully.
    /// </summary>
    /// <param name="notification">The scan complete notification data.</param>
    Task SendScanCompleteAsync(ScanCompleteDto notification);

    /// <summary>
    /// Notifies clients that a scan has failed.
    /// </summary>
    /// <param name="notification">The scan failed notification data.</param>
    Task SendScanFailedAsync(ScanFailedDto notification);

    /// <summary>
    /// Broadcasts updated aggregate stats to all connected clients.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BroadcastStatsUpdateAsync(CancellationToken cancellationToken = default);
}
