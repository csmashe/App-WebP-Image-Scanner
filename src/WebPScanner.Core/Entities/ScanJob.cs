using WebPScanner.Core.Enums;

namespace WebPScanner.Core.Entities;

/// <summary>
/// Represents a website scan request submitted by a user.
/// Tracks the scan lifecycle from queued through completion, including crawl progress and results.
/// </summary>
public class ScanJob
{
    /// <summary>
    /// Unique identifier for this scan job.
    /// </summary>
    public Guid ScanId { get; init; }

    /// <summary>
    /// The website URL to scan.
    /// </summary>
    public string TargetUrl { get; init; } = string.Empty;

    /// <summary>
    /// Optional email address to notify when scan completes.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Current status of the scan job.
    /// </summary>
    public ScanStatus Status { get; set; } = ScanStatus.Queued;

    /// <summary>
    /// Position in the processing queue (1-based, 0 if not queued).
    /// </summary>
    public int QueuePosition { get; set; }

    /// <summary>
    /// IP address of the user who submitted this scan.
    /// </summary>
    public string? SubmitterIp { get; set; }

    /// <summary>
    /// Number of scans submitted by this IP. Used for fair queue priority calculation.
    /// </summary>
    public int SubmissionCount { get; set; }

    /// <summary>
    /// When the scan was submitted.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the scan started processing.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the scan finished (success or failure).
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Number of pages successfully crawled.
    /// </summary>
    public int PagesScanned { get; set; }

    /// <summary>
    /// Total number of pages discovered during crawl.
    /// </summary>
    public int PagesDiscovered { get; set; }

    /// <summary>
    /// Number of non-WebP images found.
    /// </summary>
    public int NonWebPImagesFound { get; set; }

    /// <summary>
    /// Error message if the scan failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Priority score for fair queue ordering. Lower scores are processed first.
    /// Calculated as: SubmissionCount * 1000 - (seconds waiting / 60).
    /// This balances fairness (fewer submissions = higher priority) with starvation prevention.
    /// </summary>
    public long PriorityScore { get; set; }

    /// <summary>
    /// Whether to convert discovered images to WebP format and provide a download zip.
    /// </summary>
    public bool ConvertToWebP { get; init; }

    /// <summary>
    /// Collection of non-WebP images found during the scan.
    /// </summary>
    public ICollection<DiscoveredImage> DiscoveredImages { get; init; } = new List<DiscoveredImage>();
}
