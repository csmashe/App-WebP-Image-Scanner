namespace WebPScanner.Core.Models;

/// <summary>
/// Details about an image found during crawling.
/// </summary>
public class CrawlProgressImageDetails
{
    /// <summary>
    /// Image MIME type (e.g., "image/png", "image/jpeg").
    /// </summary>
    public string MimeType { get; init; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSize { get; init; }

    /// <summary>
    /// Image width in pixels.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Image height in pixels.
    /// </summary>
    public int Height { get; init; }
}
