using WebPScanner.Core.Models;

namespace WebPScanner.Core.Interfaces;

/// <summary>
/// Tracks real-time statistics for scans currently in progress.
/// </summary>
public interface ILiveScanStatsTracker
{
    /// <summary>
    /// Start tracking stats for a new scan.
    /// </summary>
    void StartTracking(Guid scanId);

    /// <summary>
    /// Update page counts for a scan.
    /// </summary>
    void UpdatePages(Guid scanId, int pagesScanned, int pagesDiscovered);

    /// <summary>
    /// Add an image to the live stats.
    /// </summary>
    void AddImage(Guid scanId, string mimeType, string imageUrl, long fileSize, long estimatedWebPSize, double savingsPercent);

    /// <summary>
    /// Stop tracking and remove stats for a completed scan.
    /// </summary>
    void StopTracking(Guid scanId);

    /// <summary>
    /// Get combined stats from all active scans.
    /// </summary>
    (int TotalScans, int TotalPages, int TotalImages, long TotalOriginalSize, long TotalWebPSize, double TotalSavingsPercentSum,
        Dictionary<string, LiveImageTypeStat> ImageTypeStats, Dictionary<string, LiveCategoryStat> CategoryStats) GetCombinedLiveStats();
}
