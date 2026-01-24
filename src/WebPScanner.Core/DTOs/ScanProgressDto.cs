namespace WebPScanner.Core.DTOs;

/// <summary>
/// DTO for queue position update notifications.
/// </summary>
public class QueuePositionUpdateDto
{
    /// <summary>
    /// The scan job ID.
    /// </summary>
    public Guid ScanId { get; init; }

    /// <summary>
    /// The current position in the queue (1-based).
    /// </summary>
    public int Position { get; init; }

    /// <summary>
    /// Total number of jobs in the queue.
    /// </summary>
    public int TotalInQueue { get; init; }

    /// <summary>
    /// Estimated wait time in seconds before this scan starts.
    /// Calculated as: avg_time_per_page × (remaining pages for processing scans + queued_sites_ahead × default_pages_estimate).
    /// Returns null if no historical data is available for estimation.
    /// </summary>
    // ReSharper disable once UnusedMember.Global - Used by JSON serializer
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public int? EstimatedWaitSeconds { get; init; }
}

/// <summary>
/// DTO for scan started notifications.
/// </summary>
public class ScanStartedDto
{
    /// <summary>
    /// The scan job ID.
    /// </summary>
    public Guid ScanId { get; init; }

    /// <summary>
    /// The URL being scanned.
    /// </summary>
    public string TargetUrl { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when the scan started.
    /// </summary>
    public DateTime StartedAt { get; init; }
}

/// <summary>
/// DTO for page progress notifications.
/// </summary>
public class PageProgressDto
{
    /// <summary>
    /// The scan job ID.
    /// </summary>
    public Guid ScanId { get; init; }

    /// <summary>
    /// The URL of the page currently being scanned.
    /// </summary>
    public string CurrentUrl { get; init; } = string.Empty;

    /// <summary>
    /// Number of pages scanned so far.
    /// </summary>
    public int PagesScanned { get; init; }

    /// <summary>
    /// Total number of pages discovered.
    /// </summary>
    public int PagesDiscovered { get; init; }

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public int ProgressPercent { get; init; }
}

/// <summary>
/// DTO for image found notifications.
/// </summary>
public class ImageFoundDto
{
    /// <summary>
    /// The scan job ID.
    /// </summary>
    public Guid ScanId { get; init; }

    /// <summary>
    /// URL of the image.
    /// </summary>
    public string ImageUrl { get; init; } = string.Empty;

    /// <summary>
    /// MIME type of the image.
    /// </summary>
    public string MimeType { get; init; } = string.Empty;

    /// <summary>
    /// Size of the image in bytes.
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// Whether this is a non-WebP raster image (target for optimization).
    /// </summary>
    public bool IsNonWebP { get; init; }

    /// <summary>
    /// Running count of non-WebP images found so far.
    /// </summary>
    public int TotalNonWebPCount { get; init; }

    /// <summary>
    /// The page URL where the image was found.
    /// </summary>
    public string PageUrl { get; init; } = string.Empty;
}

/// <summary>
/// DTO for scan complete notifications.
/// </summary>
public class ScanCompleteDto
{
    /// <summary>
    /// The scan job ID.
    /// </summary>
    public Guid ScanId { get; init; }

    /// <summary>
    /// Total number of pages scanned.
    /// </summary>
    public int TotalPagesScanned { get; init; }

    /// <summary>
    /// Total number of images detected.
    /// </summary>
    public int TotalImagesFound { get; init; }

    /// <summary>
    /// Total number of non-WebP raster images found.
    /// </summary>
    public int NonWebPImagesCount { get; init; }

    /// <summary>
    /// Total scan duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Timestamp when the scan completed.
    /// </summary>
    public DateTime CompletedAt { get; init; }

    /// <summary>
    /// Whether the page limit was reached.
    /// </summary>
    public bool ReachedPageLimit { get; init; }
}

/// <summary>
/// DTO for scan failed notifications.
/// </summary>
public class ScanFailedDto
{
    /// <summary>
    /// The scan job ID.
    /// </summary>
    public Guid ScanId { get; init; }

    /// <summary>
    /// Error message describing why the scan failed.
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp when the failure occurred.
    /// </summary>
    public DateTime FailedAt { get; init; }
}
