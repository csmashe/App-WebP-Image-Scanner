using System.Collections.Concurrent;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace WebPScanner.Core.Models;

/// <summary>
/// Tracks statistics for a single scan in progress.
/// </summary>
public class LiveScanStats
{
    /// <summary>
    /// Lock object for synchronizing all mutations to this instance and its nested collections.
    /// </summary>
    public object Lock { get; } = new();

    /// <summary>
    /// Number of pages scanned so far.
    /// </summary>
    public int PagesScanned { get; set; }

    /// <summary>
    /// Total number of pages discovered (including those not yet scanned).
    /// </summary>
    public int PagesDiscovered { get; set; }

    /// <summary>
    /// Number of non-WebP images found so far.
    /// </summary>
    public int ImagesFound { get; set; }

    /// <summary>
    /// Total size of all discovered images in bytes.
    /// </summary>
    public long TotalOriginalSizeBytes { get; set; }

    /// <summary>
    /// Total estimated size after WebP conversion in bytes.
    /// </summary>
    public long TotalEstimatedWebPSizeBytes { get; set; }

    /// <summary>
    /// Sum of savings percentages for calculating averages.
    /// </summary>
    public double TotalSavingsPercentSum { get; set; }

    /// <summary>
    /// Statistics grouped by image MIME type.
    /// </summary>
    public ConcurrentDictionary<string, LiveImageTypeStat> ImageTypeStats { get; } = new();

    /// <summary>
    /// Statistics grouped by image category (e.g., thumbnails, hero images).
    /// </summary>
    public ConcurrentDictionary<string, LiveCategoryStat> CategoryStats { get; } = new();
}

/// <summary>
/// Statistics for a specific image type (MIME type) in a live scan.
/// </summary>
public class LiveImageTypeStat
{
    /// <summary>
    /// The MIME type (e.g., "image/png", "image/jpeg").
    /// </summary>
    public string MimeType { get; init; } = string.Empty;

    /// <summary>
    /// Number of images of this type.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Total size of all images of this type in bytes.
    /// </summary>
    public long TotalSizeBytes { get; set; }

    /// <summary>
    /// Total potential savings in bytes if converted to WebP.
    /// </summary>
    public long PotentialSavingsBytes { get; set; }

    /// <summary>
    /// Sum of savings percentages for calculating averages.
    /// </summary>
    public double SavingsPercentSum { get; set; }
}

/// <summary>
/// Statistics for a specific image category in a live scan.
/// </summary>
public class LiveCategoryStat
{
    /// <summary>
    /// The category name (e.g., "Thumbnails", "Hero &amp; Banners").
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Number of images in this category.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Total potential savings in bytes for this category.
    /// </summary>
    public long TotalSavingsBytes { get; set; }

    /// <summary>
    /// Sum of savings percentages for calculating averages.
    /// </summary>
    public double SavingsPercentSum { get; set; }
}
