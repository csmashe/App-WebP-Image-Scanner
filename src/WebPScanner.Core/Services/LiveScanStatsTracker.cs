using System.Collections.Concurrent;
using WebPScanner.Core.Interfaces;
using WebPScanner.Core.Models;

namespace WebPScanner.Core.Services;

/// <summary>
/// Tracks real-time statistics for scans that are currently in progress.
/// This allows the aggregate stats to include in-memory data before it's persisted to the database.
/// </summary>
public class LiveScanStatsTracker : ILiveScanStatsTracker
{
    private readonly ConcurrentDictionary<Guid, LiveScanStats> _scanStats = new();

    public void StartTracking(Guid scanId)
    {
        _scanStats[scanId] = new LiveScanStats();
    }

    public void UpdatePages(Guid scanId, int pagesScanned, int pagesDiscovered)
    {
	    if (!_scanStats.TryGetValue(scanId, out var stats))
	    {
		    return;
	    }

	    lock (stats.Lock)
	    {
		    stats.PagesScanned = pagesScanned;
		    stats.PagesDiscovered = pagesDiscovered;
	    }
    }

    public void AddImage(Guid scanId, string mimeType, string imageUrl, long fileSize, long estimatedWebPSize, double savingsPercent)
    {
	    if (!_scanStats.TryGetValue(scanId, out var stats))
	    {
		    return;
	    }

	    // Clamp negative savings to zero (can happen if WebP would be larger than original)
	    var savingsBytes = Math.Max(0, fileSize - estimatedWebPSize);
	    var clampedSavingsPercent = Math.Max(0, savingsPercent);

	    lock (stats.Lock)
	    {
		    stats.ImagesFound++;
		    stats.TotalOriginalSizeBytes += fileSize;
		    stats.TotalEstimatedWebPSizeBytes += estimatedWebPSize;
		    stats.TotalSavingsPercentSum += clampedSavingsPercent;

		    // Track by mime type
		    if (stats.ImageTypeStats.TryGetValue(mimeType, out var existingTypeStat))
		    {
			    existingTypeStat.Count++;
			    existingTypeStat.TotalSizeBytes += fileSize;
			    existingTypeStat.PotentialSavingsBytes += savingsBytes;
			    existingTypeStat.SavingsPercentSum += clampedSavingsPercent;
		    }
		    else
		    {
			    stats.ImageTypeStats[mimeType] = new LiveImageTypeStat
			    {
				    MimeType = mimeType,
				    Count = 1,
				    TotalSizeBytes = fileSize,
				    PotentialSavingsBytes = savingsBytes,
				    SavingsPercentSum = clampedSavingsPercent
			    };
		    }

		    // Track by category
		    var category = DetermineCategory(imageUrl);
		    if (stats.CategoryStats.TryGetValue(category, out var existingCatStat))
		    {
			    existingCatStat.Count++;
			    existingCatStat.TotalSavingsBytes += savingsBytes;
			    existingCatStat.SavingsPercentSum += clampedSavingsPercent;
		    }
		    else
		    {
			    stats.CategoryStats[category] = new LiveCategoryStat
			    {
				    Category = category,
				    Count = 1,
				    TotalSavingsBytes = savingsBytes,
				    SavingsPercentSum = clampedSavingsPercent
			    };
		    }
	    }
    }

    public void StopTracking(Guid scanId)
    {
        _scanStats.TryRemove(scanId, out _);
    }

    public (int TotalScans, int TotalPages, int TotalImages, long TotalOriginalSize, long TotalWebPSize, double TotalSavingsPercentSum,
        Dictionary<string, LiveImageTypeStat> ImageTypeStats, Dictionary<string, LiveCategoryStat> CategoryStats) GetCombinedLiveStats()
    {
        var totalScans = 0;
        var totalPages = 0;
        var totalImages = 0;
        long totalOriginalSize = 0;
        long totalWebPSize = 0;
        double totalSavingsPercentSum = 0;
        var imageTypeStats = new Dictionary<string, LiveImageTypeStat>();
        var categoryStats = new Dictionary<string, LiveCategoryStat>();

        foreach (var kvp in _scanStats)
        {
            var stats = kvp.Value;

            // Take a consistent snapshot of this scan's stats under lock
            int pagesScanned;
            int imagesFound;
            long originalSize;
            long webPSize;
            double savingsPercentSum;
            List<KeyValuePair<string, LiveImageTypeStat>> typeStatsSnapshot;
            List<KeyValuePair<string, LiveCategoryStat>> catStatsSnapshot;

            lock (stats.Lock)
            {
                pagesScanned = stats.PagesScanned;
                imagesFound = stats.ImagesFound;
                originalSize = stats.TotalOriginalSizeBytes;
                webPSize = stats.TotalEstimatedWebPSizeBytes;
                savingsPercentSum = stats.TotalSavingsPercentSum;

                // Snapshot the nested collections with their current values
                typeStatsSnapshot = stats.ImageTypeStats.Select(kv => new KeyValuePair<string, LiveImageTypeStat>(
                    kv.Key,
                    new LiveImageTypeStat
                    {
                        MimeType = kv.Value.MimeType,
                        Count = kv.Value.Count,
                        TotalSizeBytes = kv.Value.TotalSizeBytes,
                        PotentialSavingsBytes = kv.Value.PotentialSavingsBytes,
                        SavingsPercentSum = kv.Value.SavingsPercentSum
                    })).ToList();

                catStatsSnapshot = stats.CategoryStats.Select(kv => new KeyValuePair<string, LiveCategoryStat>(
                    kv.Key,
                    new LiveCategoryStat
                    {
                        Category = kv.Value.Category,
                        Count = kv.Value.Count,
                        TotalSavingsBytes = kv.Value.TotalSavingsBytes,
                        SavingsPercentSum = kv.Value.SavingsPercentSum
                    })).ToList();
            }

            totalScans++;
            totalPages += pagesScanned;
            totalImages += imagesFound;
            totalOriginalSize += originalSize;
            totalWebPSize += webPSize;
            totalSavingsPercentSum += savingsPercentSum;

            // Merge image type stats from snapshot
            foreach (var typeStat in typeStatsSnapshot)
            {
                if (imageTypeStats.TryGetValue(typeStat.Key, out var existing))
                {
                    existing.Count += typeStat.Value.Count;
                    existing.TotalSizeBytes += typeStat.Value.TotalSizeBytes;
                    existing.PotentialSavingsBytes += typeStat.Value.PotentialSavingsBytes;
                    existing.SavingsPercentSum += typeStat.Value.SavingsPercentSum;
                }
                else
                {
                    imageTypeStats[typeStat.Key] = typeStat.Value;
                }
            }

            // Merge category stats from snapshot
            foreach (var catStat in catStatsSnapshot)
            {
                if (categoryStats.TryGetValue(catStat.Key, out var existing))
                {
                    existing.Count += catStat.Value.Count;
                    existing.TotalSavingsBytes += catStat.Value.TotalSavingsBytes;
                    existing.SavingsPercentSum += catStat.Value.SavingsPercentSum;
                }
                else
                {
                    categoryStats[catStat.Key] = catStat.Value;
                }
            }
        }

        return (totalScans, totalPages, totalImages, totalOriginalSize, totalWebPSize, totalSavingsPercentSum, imageTypeStats, categoryStats);
    }

    public int GetTotalRemainingPagesForActiveScans()
    {
        var totalRemaining = 0;

        foreach (var kvp in _scanStats)
        {
            var stats = kvp.Value;
            lock (stats.Lock)
            {
                // Remaining pages = discovered - scanned (minimum 0)
                var remaining = Math.Max(0, stats.PagesDiscovered - stats.PagesScanned);
                totalRemaining += remaining;
            }
        }

        return totalRemaining;
    }

    public List<int> GetActiveScansRemainingPagesSorted()
    {
        var remainingPages = new List<int>();

        foreach (var kvp in _scanStats)
        {
            var stats = kvp.Value;
            lock (stats.Lock)
            {
                var remaining = Math.Max(0, stats.PagesDiscovered - stats.PagesScanned);
                remainingPages.Add(remaining);
            }
        }

        // Sort ascending so the scan closest to finishing is first
        remainingPages.Sort();
        return remainingPages;
    }

    private static string DetermineCategory(string url)
    {
        var lowerUrl = url.ToLowerInvariant();

        if (lowerUrl.Contains("hero") || lowerUrl.Contains("banner") || lowerUrl.Contains("slider"))
            return "Hero & Banners";
        if (lowerUrl.Contains("thumb") || lowerUrl.Contains("thumbnail"))
            return "Thumbnails";
        if (lowerUrl.Contains("product"))
            return "Product Images";
        if (lowerUrl.Contains("blog") || lowerUrl.Contains("post") || lowerUrl.Contains("article"))
            return "Blog & Articles";
        if (lowerUrl.Contains("logo") || lowerUrl.Contains("icon") || lowerUrl.Contains("favicon"))
            return "Logos & Icons";
        if (lowerUrl.Contains("avatar") || lowerUrl.Contains("profile") || lowerUrl.Contains("user"))
            return "User Avatars";
        if (lowerUrl.Contains("background") || lowerUrl.Contains("bg"))
            return "Backgrounds";

        return "Other Images";
    }
}
