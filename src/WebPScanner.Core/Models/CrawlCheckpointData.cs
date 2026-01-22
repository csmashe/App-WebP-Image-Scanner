namespace WebPScanner.Core.Models;

/// <summary>
/// Data passed to checkpoint callback for saving crawl state.
/// </summary>
public class CrawlCheckpointData
{
    /// <summary>
    /// URLs that have been visited.
    /// </summary>
    public IReadOnlyCollection<string> VisitedUrls { get; init; } = [];

    /// <summary>
    /// URLs pending to be visited.
    /// </summary>
    public IReadOnlyCollection<string> PendingUrls { get; init; } = [];

    /// <summary>
    /// Number of pages visited.
    /// </summary>
    public int PagesVisited { get; init; }

    /// <summary>
    /// Number of pages discovered.
    /// </summary>
    public int PagesDiscovered { get; init; }

    /// <summary>
    /// Number of non-WebP images found so far.
    /// </summary>
    public int NonWebPImagesFound { get; init; }

    /// <summary>
    /// Current URL being processed.
    /// </summary>
    public string? CurrentUrl { get; init; }
}
