namespace WebPScanner.Core.Models;

/// <summary>
/// Represents detailed savings estimate for a single image.
/// </summary>
public class ImageSavingsEstimate
{
    /// <summary>
    /// URL of the image.
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// Original MIME type of the image.
    /// </summary>
    public string OriginalMimeType { get; init; } = string.Empty;

    /// <summary>
    /// Original size in bytes.
    /// </summary>
    public long OriginalSize { get; init; }

    /// <summary>
    /// Estimated WebP size in bytes.
    /// </summary>
    public long EstimatedWebPSize { get; init; }

    /// <summary>
    /// Potential savings in bytes.
    /// </summary>
    public long SavingsBytes { get; init; }

    /// <summary>
    /// Potential savings as a percentage (0-100).
    /// </summary>
    public double SavingsPercentage { get; init; }

    /// <summary>
    /// The conversion ratio used for this image type.
    /// </summary>
    public double ConversionRatio { get; init; }
}

/// <summary>
/// Represents aggregate savings statistics for a scan.
/// </summary>
public class SavingsSummary
{
    /// <summary>
    /// Total number of images analyzed.
    /// </summary>
    public int TotalImages { get; init; }

    /// <summary>
    /// Number of images that can be converted to WebP.
    /// </summary>
    public int ConvertibleImages { get; set; }

    /// <summary>
    /// Total original size of all convertible images in bytes.
    /// </summary>
    public long TotalOriginalSize { get; set; }

    /// <summary>
    /// Total estimated WebP size in bytes.
    /// </summary>
    public long TotalEstimatedWebPSize { get; set; }

    /// <summary>
    /// Total potential savings in bytes.
    /// </summary>
    public long TotalSavingsBytes { get; set; }

    /// <summary>
    /// Overall potential savings as a percentage.
    /// </summary>
    public double TotalSavingsPercentage { get; set; }

    /// <summary>
    /// Breakdown by image type.
    /// </summary>
    public Dictionary<string, TypeSavingsSummary> ByType { get; set; } = new();

    /// <summary>
    /// Disclaimer about estimation accuracy.
    /// </summary>
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    public string Disclaimer { get; set; } = "Savings estimates are approximate and based on empirical conversion ratios. Actual savings may vary depending on image content and compression settings.";
}

/// <summary>
/// Savings summary for a specific image type.
/// </summary>
public class TypeSavingsSummary
{
    /// <summary>
    /// The MIME type.
    /// </summary>
    public string MimeType { get; init; } = string.Empty;

    /// <summary>
    /// Number of images of this type.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Total original size in bytes.
    /// </summary>
    public long TotalOriginalSize { get; set; }

    /// <summary>
    /// Total estimated WebP size in bytes.
    /// </summary>
    public long TotalEstimatedWebPSize { get; set; }

    /// <summary>
    /// Total savings in bytes.
    /// </summary>
    public long TotalSavingsBytes { get; set; }

    /// <summary>
    /// Savings percentage for this type.
    /// </summary>
    public double SavingsPercentage { get; set; }

    /// <summary>
    /// The conversion ratio used for this type.
    /// </summary>
    public double ConversionRatio { get; set; }
}
