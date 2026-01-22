using WebPScanner.Core.Entities;

namespace WebPScanner.Core.Interfaces;

/// <summary>
/// Repository interface for managing crawl checkpoints.
/// </summary>
public interface ICrawlCheckpointRepository
{
    /// <summary>
    /// Gets the checkpoint for a specific scan job.
    /// </summary>
    /// <param name="scanJobId">The scan job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The checkpoint if found, null otherwise.</returns>
    Task<CrawlCheckpoint?> GetByScanJobIdAsync(Guid scanJobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves or updates a checkpoint.
    /// </summary>
    /// <param name="checkpoint">The checkpoint to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveCheckpointAsync(CrawlCheckpoint checkpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the checkpoint for a specific scan job.
    /// </summary>
    /// <param name="scanJobId">The scan job ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteByScanJobIdAsync(Guid scanJobId, CancellationToken cancellationToken = default);
}
