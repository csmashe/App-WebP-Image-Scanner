using System.Text.Json;
// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace WebPScanner.Core.Entities;

/// <summary>
/// Represents a non-WebP image found during a website crawl.
/// Stores the image metadata and estimated WebP conversion savings.
/// </summary>
public class DiscoveredImage
{
    /// <summary>
    /// Unique identifier for this discovered image.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The scan job that discovered this image.
    /// </summary>
    public Guid ScanJobId { get; init; }

    /// <summary>
    /// Full URL of the image.
    /// </summary>
    public string ImageUrl { get; init; } = string.Empty;

    /// <summary>
    /// First page URL where this image was found.
    /// </summary>
    public string PageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Image MIME type (e.g., "image/png", "image/jpeg").
    /// </summary>
    public string MimeType { get; init; } = string.Empty;

    /// <summary>
    /// Original file size in bytes.
    /// </summary>
    public long FileSize { get; init; }

    /// <summary>
    /// Image width in pixels, if available.
    /// </summary>
    public int? Width { get; init; }

    /// <summary>
    /// Image height in pixels, if available.
    /// </summary>
    public int? Height { get; init; }

    /// <summary>
    /// Estimated file size after WebP conversion in bytes.
    /// </summary>
    public long EstimatedWebPSize { get; init; }

    /// <summary>
    /// Estimated percentage savings from WebP conversion.
    /// </summary>
    public double PotentialSavingsPercent { get; init; }

    /// <summary>
    /// When this image was discovered.
    /// </summary>
    public DateTime DiscoveredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// JSON array of all page URLs where this image was found.
    /// </summary>
    public string PageUrlsJson { get; set; } = "[]";

    /// <summary>
    /// Number of pages where this image was found.
    /// </summary>
    public int PageCount { get; set; } = 1;

    /// <summary>
    /// Navigation property to the parent scan job.
    /// </summary>
    public ScanJob ScanJob { get; init; } = null!;

    /// <summary>
    /// Gets the list of page URLs from the JSON storage.
    /// Returns a fallback list containing PageUrl if the JSON is null, empty, or invalid.
    /// </summary>
    public List<string> GetPageUrls()
    {
        try
        {
            var urls = JsonSerializer.Deserialize<List<string>>(PageUrlsJson);
            return urls is { Count: > 0 } ? urls : [PageUrl];
        }
        catch
        {
            return [PageUrl];
        }
    }

    /// <summary>
    /// Sets the page URLs and updates PageUrl to the first one.
    /// </summary>
    public void SetPageUrls(List<string> pageUrls)
    {
        if (pageUrls.Count == 0)
            return;

        PageUrl = pageUrls[0];
        PageCount = pageUrls.Count;
        PageUrlsJson = JsonSerializer.Serialize(pageUrls);
    }
}
