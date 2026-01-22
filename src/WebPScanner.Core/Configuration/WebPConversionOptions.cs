// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace WebPScanner.Core.Configuration;

/// <summary>
/// Configuration options for WebP image conversion.
/// </summary>
public class WebPConversionOptions
{
    public const string SectionName = "WebPConversion";

    /// <summary>
    /// Directory where converted image zips are stored.
    /// Default is "converted-images" in the app's base directory.
    /// </summary>
    public string StorageDirectory { get; init; } = "converted-images";

    /// <summary>
    /// How long converted image zips are retained before deletion (in hours).
    /// Default is 6 hours.
    /// </summary>
    public int RetentionHours { get; init; } = 6;

    /// <summary>
    /// How often to run the cleanup job (in minutes).
    /// Default is 15 minutes.
    /// </summary>
    public int CleanupIntervalMinutes { get; init; } = 15;

    /// <summary>
    /// WebP quality setting (0-100). Higher is better quality but larger file size.
    /// Default is 80.
    /// </summary>
    public int WebPQuality { get; init; } = 80;

    /// <summary>
    /// Maximum number of images to convert per scan.
    /// Prevents excessive resource usage. Default is 500.
    /// </summary>
    public int MaxImagesPerScan { get; init; } = 500;

    /// <summary>
    /// Maximum total size of original images to download (in MB).
    /// Default is 500 MB.
    /// </summary>
    public int MaxTotalDownloadSizeMb { get; init; } = 500;

    /// <summary>
    /// Maximum size of a single image to download (in MB).
    /// Prevents memory spikes from unexpectedly large images. Default is 50 MB.
    /// </summary>
    public int MaxSingleImageSizeMb { get; init; } = 50;

    /// <summary>
    /// Timeout for downloading each image (in seconds).
    /// Default is 30 seconds.
    /// </summary>
    public int ImageDownloadTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Maximum concurrent image downloads.
    /// Default is 5.
    /// </summary>
    public int MaxConcurrentDownloads { get; init; } = 5;

    /// <summary>
    /// Whether to include original filename in the zip or use sanitized names.
    /// Default is true.
    /// </summary>
    public bool PreserveOriginalFilenames { get; init; } = true;
}
