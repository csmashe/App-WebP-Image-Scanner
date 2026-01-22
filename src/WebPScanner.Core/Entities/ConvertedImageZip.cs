namespace WebPScanner.Core.Entities;

/// <summary>
/// Tracks a zip file containing converted WebP images for download.
/// </summary>
public class ConvertedImageZip
{
    /// <summary>
    /// Unique download identifier used in the download URL.
    /// </summary>
    public Guid DownloadId { get; init; }

    /// <summary>
    /// The scan job this zip was generated for.
    /// </summary>
    public Guid ScanJobId { get; init; }

    /// <summary>
    /// Path to the zip file on disk.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Original filename for the download (e.g., "webp-images-example.com.zip").
    /// </summary>
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// Size of the zip file in bytes.
    /// </summary>
    public long FileSizeBytes { get; init; }

    /// <summary>
    /// Number of images included in the zip.
    /// </summary>
    public int ImageCount { get; init; }

    /// <summary>
    /// When the zip was created. Used for retention policy (6 hours).
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the zip expires and should be deleted.
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    // Navigation property
    public ScanJob? ScanJob { get; init; }
}
