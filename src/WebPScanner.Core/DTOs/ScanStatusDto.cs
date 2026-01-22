using WebPScanner.Core.Enums;

namespace WebPScanner.Core.DTOs;

/// <summary>
/// Data transfer object for scan status response.
/// </summary>
public class ScanStatusDto
{
    /// <summary>
    /// The unique identifier for the scan job.
    /// </summary>
    public Guid ScanId { get; init; }

    /// <summary>
    /// The current status of the scan.
    /// </summary>
    public ScanStatus Status { get; init; }

    /// <summary>
    /// The current position in the queue (1-based). Null if not queued.
    /// </summary>
    public int? QueuePosition { get; init; }

    /// <summary>
    /// The target URL being scanned.
    /// </summary>
    public string TargetUrl { get; init; } = string.Empty;

    /// <summary>
    /// Number of pages discovered so far.
    /// </summary>
    public int PagesDiscovered { get; init; }

    /// <summary>
    /// Number of pages scanned so far.
    /// </summary>
    public int PagesScanned { get; init; }

    /// <summary>
    /// Number of non-WebP images found so far.
    /// </summary>
    public int NonWebPImagesFound { get; init; }

    /// <summary>
    /// When the scan job was created.
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// When the scan started processing. Null if not yet started.
    /// </summary>
    public DateTime? StartedAt { get; init; }

    /// <summary>
    /// When the scan completed. Null if not yet completed.
    /// </summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>
    /// Error message if the scan failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
