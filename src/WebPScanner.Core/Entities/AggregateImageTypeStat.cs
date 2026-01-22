namespace WebPScanner.Core.Entities;

/// <summary>
/// Stores aggregated statistics for an image MIME type.
/// </summary>
public class AggregateImageTypeStat
{
    public int Id { get; init; }

    /// <summary>
    /// Foreign key to AggregateStats (always 1).
    /// </summary>
    public int AggregateStatsId { get; init; } = 1;

    /// <summary>
    /// MIME type (e.g., "image/jpeg", "image/png").
    /// </summary>
    public string MimeType { get; init; } = string.Empty;

    /// <summary>
    /// Display name (e.g., "JPEG", "PNG").
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Total number of images of this type.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Total original file size in bytes.
    /// </summary>
    public long TotalSizeBytes { get; init; }

    /// <summary>
    /// Total potential savings in bytes.
    /// </summary>
    public long PotentialSavingsBytes { get; init; }

    /// <summary>
    /// Running sum of savings percentages (divide by Count for average).
    /// </summary>
    public double SavingsPercentSum { get; init; }

    /// <summary>
    /// Navigation property.
    /// </summary>
    public AggregateStats AggregateStats { get; init; } = null!;
}
