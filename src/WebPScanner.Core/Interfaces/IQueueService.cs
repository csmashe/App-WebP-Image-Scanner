using WebPScanner.Core.Entities;

namespace WebPScanner.Core.Interfaces;

/// <summary>
/// Manages the scan job queue including enqueueing, dequeueing, priority calculation,
/// IP-based rate limiting, and cooldown tracking.
/// </summary>
public interface IQueueService
{
    /// <summary>
    /// Enqueues a scan job for processing.
    /// </summary>
    /// <param name="scanJob">The scan job to enqueue.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated scan job with queue position and priority score.</returns>
    Task<ScanJob> EnqueueAsync(ScanJob scanJob, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeues the next scan job for processing based on priority.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next scan job to process, or null if queue is empty or max concurrent scans reached.</returns>
    Task<ScanJob?> DequeueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the queue can accept new jobs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the queue has capacity.</returns>
    Task<bool> CanEnqueueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the IP has reached the maximum number of queued jobs.
    /// </summary>
    /// <param name="ip">The submitter IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the IP has reached the limit.</returns>
    Task<bool> HasIpReachedQueueLimitAsync(string ip, CancellationToken cancellationToken = default);

    /// <summary>
    /// Recalculates priority scores for all queued jobs applying aging boost.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of scan IDs that had their positions changed.</returns>
    Task<IReadOnlyList<Guid>> RecalculatePrioritiesWithAgingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the submitter IP is in cooldown period.
    /// </summary>
    /// <param name="ip">The submitter IP address.</param>
    /// <returns>True if the IP is in cooldown.</returns>
    bool IsIpInCooldown(string ip);

    /// <summary>
    /// Records a cooldown entry for an IP after scan completion.
    /// </summary>
    /// <param name="ip">The submitter IP address.</param>
    void RecordCooldown(string ip);

    /// <summary>
    /// Marks a scan job as completed or failed.
    /// </summary>
    /// <param name="scanId">The scan job ID.</param>
    /// <param name="success">Whether the scan completed successfully.</param>
    /// <param name="errorMessage">Error message if failed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CompleteJobAsync(Guid scanId, bool success, string? errorMessage = null, CancellationToken cancellationToken = default);
}
