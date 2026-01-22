// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength
namespace WebPScanner.Core.Entities;

/// <summary>
/// Represents a checkpoint of crawl state for resume functionality.
/// Stores the URLs visited and pending so a scan can be resumed after interruption.
/// </summary>
public class CrawlCheckpoint
{
    /// <summary>
    /// Unique identifier for the checkpoint.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The scan job this checkpoint belongs to.
    /// </summary>
    public Guid ScanJobId { get; init; }

    /// <summary>
    /// JSON-serialized list of URLs that have been visited.
    /// </summary>
    public string VisitedUrlsJson { get; set; } = "[]";

    /// <summary>
    /// JSON-serialized list of URLs pending to be visited.
    /// </summary>
    public string PendingUrlsJson { get; set; } = "[]";

    /// <summary>
    /// Number of pages that have been visited at this checkpoint.
    /// </summary>
    public int PagesVisited { get; set; }

    /// <summary>
    /// Number of pages discovered (visited + pending) at this checkpoint.
    /// </summary>
    public int PagesDiscovered { get; set; }

    /// <summary>
    /// Number of non-WebP images found at this checkpoint.
    /// </summary>
    public int NonWebPImagesFound { get; set; }

    /// <summary>
    /// The URL currently being processed when the checkpoint was created.
    /// </summary>
    public string? CurrentUrl { get; set; }

    /// <summary>
    /// When the checkpoint was first created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the checkpoint was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the scan job.
    /// </summary>
    public ScanJob? ScanJob { get; init; }
}
