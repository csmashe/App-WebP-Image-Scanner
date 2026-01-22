namespace WebPScanner.Core.Models;

/// <summary>
/// Represents the result of crawling a single page.
/// </summary>
public class PageCrawlResult
{
    /// <summary>
    /// The URL that was crawled.
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// Whether the page was crawled successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// HTTP status code of the response.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Error message if the crawl failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// List of same-domain URLs discovered on this page.
    /// </summary>
    public List<string> DiscoveredUrls { get; set; } = [];

    /// <summary>
    /// List of images detected on this page.
    /// </summary>
    public List<DetectedImage> DetectedImages { get; set; } = [];

    /// <summary>
    /// Whether the page appears to be a login/auth page.
    /// </summary>
    public bool IsAuthenticationPage { get; set; }

    /// <summary>
    /// Time taken to crawl this page.
    /// </summary>
    public TimeSpan CrawlDuration { get; set; }
}

/// <summary>
/// Represents an image detected during crawling.
/// </summary>
public class DetectedImage
{
    /// <summary>
    /// URL of the image.
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// MIME type of the image (e.g., image/jpeg, image/png).
    /// </summary>
    public string MimeType { get; init; } = string.Empty;

    /// <summary>
    /// Size of the image in bytes.
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// Width of the image in pixels, if available.
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// Height of the image in pixels, if available.
    /// </summary>
    public int? Height { get; init; }
}

/// <summary>
/// Represents the complete result of a website crawl.
/// </summary>
public class CrawlResult
{
    /// <summary>
    /// The base URL that was crawled.
    /// </summary>
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Whether the crawl completed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the crawl failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Total number of pages crawled.
    /// </summary>
    public int PagesScanned { get; set; }

    /// <summary>
    /// Total number of pages discovered.
    /// </summary>
    public int PagesDiscovered { get; set; }

    /// <summary>
    /// List of all detected images.
    /// </summary>
    public List<DetectedImage> DetectedImages { get; set; } = [];

    /// <summary>
    /// List of non-WebP raster images (the main result).
    /// </summary>
    public List<DetectedImage> NonWebPImages { get; set; } = [];

    /// <summary>
    /// Total crawl duration.
    /// </summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>
    /// Indicates if the crawl was stopped due to reaching the page limit.
    /// </summary>
    public bool ReachedPageLimit { get; set; }

    /// <summary>
    /// Map of image URL to all page URLs where it was found.
    /// </summary>
    public Dictionary<string, List<string>> ImageToPagesMap { get; set; } = new();
}
