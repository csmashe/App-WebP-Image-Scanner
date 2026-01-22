using WebPScanner.Core.Enums;

namespace WebPScanner.Core.Models;

/// <summary>
/// Progress information for crawl operations.
/// </summary>
public class CrawlProgress
{
    /// <summary>
    /// Current URL being processed (image URL for ImageFound, page URL for other types).
    /// </summary>
    public string CurrentUrl { get; init; } = string.Empty;

    /// <summary>
    /// The page URL where an image was discovered (only set for ImageFound type).
    /// </summary>
    public string? PageUrl { get; init; }

    /// <summary>
    /// Number of pages scanned so far.
    /// </summary>
    public int PagesScanned { get; init; }

    /// <summary>
    /// Total number of pages discovered.
    /// </summary>
    public int PagesDiscovered { get; init; }

    /// <summary>
    /// Number of non-WebP images found so far.
    /// </summary>
    public int NonWebPImagesFound { get; init; }

    /// <summary>
    /// Progress type for the update.
    /// </summary>
    public CrawlProgressType Type { get; init; }

    /// <summary>
    /// Image details when Type is ImageFound.
    /// </summary>
    public CrawlProgressImageDetails? ImageDetails { get; init; }
}
