namespace WebPScanner.Core.Entities;

/// <summary>
/// Stores aggregated statistics from all completed scans.
/// This is a singleton row that gets updated as scans complete and data is purged.
/// </summary>
public class AggregateStats
{
    /// <summary>
    /// Single row ID (always 1).
    /// </summary>
    public int Id { get; init; } = 1;

    /// <summary>
    /// Total number of completed scans (all time).
    /// </summary>
    public int TotalScans { get; set; }

    /// <summary>
    /// Total pages crawled across all scans.
    /// </summary>
    public int TotalPagesCrawled { get; set; }

    /// <summary>
    /// Total non-WebP images found across all scans.
    /// </summary>
    public int TotalImagesFound { get; set; }

    /// <summary>
    /// Total original file size in bytes.
    /// </summary>
    public long TotalOriginalSizeBytes { get; set; }

    /// <summary>
    /// Total estimated WebP size in bytes.
    /// </summary>
    public long TotalEstimatedWebPSizeBytes { get; set; }

    /// <summary>
    /// Running sum of savings percentages (divide by TotalImagesFound for average).
    /// </summary>
    public double TotalSavingsPercentSum { get; set; }

    /// <summary>
    /// Last time the aggregate stats were updated.
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Concurrency token for optimistic locking.
    /// </summary>
    public byte[] RowVersion { get; init; } = [];

    /// <summary>
    /// Navigation property for category stats.
    /// </summary>
    public ICollection<AggregateCategoryStat> CategoryStats { get; init; } = new List<AggregateCategoryStat>();

    /// <summary>
    /// Navigation property for image type stats.
    /// </summary>
    public ICollection<AggregateImageTypeStat> ImageTypeStats { get; init; } = new List<AggregateImageTypeStat>();
}
