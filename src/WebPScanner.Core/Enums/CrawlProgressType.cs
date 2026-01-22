namespace WebPScanner.Core.Enums;

/// <summary>
/// Type of crawl progress update.
/// </summary>
public enum CrawlProgressType
{
    /// <summary>
    /// Started processing a page.
    /// </summary>
    PageStarted,

    /// <summary>
    /// Finished processing a page.
    /// </summary>
    PageCompleted,

    /// <summary>
    /// Found a non-WebP image.
    /// </summary>
    ImageFound,

    /// <summary>
    /// Crawl completed successfully.
    /// </summary>
    CrawlCompleted,

    /// <summary>
    /// Crawl failed with an error.
    /// </summary>
    CrawlFailed
}
