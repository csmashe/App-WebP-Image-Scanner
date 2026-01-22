using WebPScanner.Core.Utilities;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace WebPScanner.Core.DTOs;

/// <summary>
/// Aggregate statistics from all completed scans.
/// </summary>
public class AggregateStatsDto
{
    /// <summary>
    /// Total number of completed scans.
    /// </summary>
    public int TotalScans { get; init; }

    /// <summary>
    /// Total number of pages crawled across all scans.
    /// </summary>
    public int TotalPagesCrawled { get; set; }

    /// <summary>
    /// Total number of non-WebP images found across all scans.
    /// </summary>
    public int TotalImagesFound { get; init; }

    /// <summary>
    /// Total file size of all non-WebP images in bytes.
    /// </summary>
    public long TotalOriginalSizeBytes { get; init; }

    /// <summary>
    /// Total estimated WebP size in bytes.
    /// </summary>
    public long TotalEstimatedWebPSizeBytes { get; init; }

    /// <summary>
    /// Total potential savings in bytes (original - estimated WebP), clamped to zero.
    /// </summary>
    public long TotalSavingsBytes => Math.Max(0, TotalOriginalSizeBytes - TotalEstimatedWebPSizeBytes);

    /// <summary>
    /// Total savings formatted as a human-readable string (e.g., "312 MB").
    /// </summary>
    // ReSharper disable once UnusedMember.Global - used in JSON serialization
    public string TotalSavingsFormatted => ByteFormatUtility.FormatBytes(TotalSavingsBytes);

    /// <summary>
    /// Average savings percentage across all images.
    /// </summary>
    public double AverageSavingsPercent { get; set; }

    /// <summary>
    /// Breakdown of images by MIME type with their savings.
    /// </summary>
    public List<ImageTypeStat> ImageTypeBreakdown { get; set; } = [];

    /// <summary>
    /// Top categories by potential savings.
    /// </summary>
    public List<CategoryStat> TopCategories { get; set; } = [];
}

/// <summary>
/// Statistics for a specific image MIME type.
/// </summary>
public class ImageTypeStat
{
    /// <summary>
    /// The MIME type (e.g., "image/png", "image/jpeg").
    /// </summary>
    public string MimeType { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable name (e.g., "PNG", "JPEG").
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Number of images of this type.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Combined file size of all images of this type in bytes.
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    /// Estimated bytes saved by converting to WebP.
    /// </summary>
    public long PotentialSavingsBytes { get; init; }

    /// <summary>
    /// Average savings percentage for this image type.
    /// </summary>
    public double SavingsPercent { get; set; }
}

/// <summary>
/// Statistics for an image category (based on URL patterns).
/// </summary>
public class CategoryStat
{
    /// <summary>
    /// Category name (e.g., "Product Images", "Banners", "Icons").
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Number of images in this category.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Total potential savings in bytes for this category.
    /// </summary>
    public long TotalSavingsBytes { get; init; }

    /// <summary>
    /// Average savings percentage for images in this category.
    /// </summary>
    public double SavingsPercent { get; set; }
}
