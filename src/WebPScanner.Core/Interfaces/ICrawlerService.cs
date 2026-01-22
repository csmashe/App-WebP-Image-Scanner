using WebPScanner.Core.Entities;
using WebPScanner.Core.Models;

namespace WebPScanner.Core.Interfaces;

/// <summary>
/// Service for crawling websites and detecting images.
/// </summary>
public interface ICrawlerService
{
    /// <summary>
    /// Crawls a website starting from the given URL.
    /// </summary>
    /// <param name="scanJob">The scan job containing the target URL.</param>
    /// <param name="checkpoint">Optional checkpoint to resume from.</param>
    /// <param name="progressCallback">Optional async callback for progress updates.</param>
    /// <param name="checkpointCallback">Optional callback to save checkpoints periodically.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the crawl operation.</returns>
    Task<CrawlResult> CrawlAsync(
        ScanJob scanJob,
        CrawlCheckpoint? checkpoint = null,
        Func<CrawlProgress, Task>? progressCallback = null,
        Func<CrawlCheckpointData, Task>? checkpointCallback = null,
        CancellationToken cancellationToken = default);

}
