namespace WebPScanner.Core.Entities;

/// <summary>
/// Stores aggregated statistics for an image category (based on URL patterns).
/// </summary>
public class AggregateCategoryStat
{
    public int Id { get; init; }

    /// <summary>
    /// Foreign key to AggregateStats (always 1).
    /// </summary>
    public int AggregateStatsId { get; init; } = 1;

    /// <summary>
    /// Category name (e.g., "Hero & Banners", "Thumbnails").
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Total number of images in this category.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Total potential savings in bytes for this category.
    /// </summary>
    public long TotalSavingsBytes { get; init; }

    /// <summary>
    /// Running sum of savings percentages (divide by Count for average).
    /// </summary>
    public double SavingsPercentSum { get; init; }

    /// <summary>
    /// Navigation property.
    /// </summary>
    public AggregateStats AggregateStats { get; init; } = null!;
}
