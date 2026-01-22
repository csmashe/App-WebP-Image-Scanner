using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebPScanner.Core.DTOs;
using WebPScanner.Core.Entities;
using WebPScanner.Core.Enums;
using WebPScanner.Core.Interfaces;
using WebPScanner.Data.Context;

namespace WebPScanner.Data.Services;

/// <summary>
/// Service for managing aggregated statistics across all scans.
/// </summary>
public class AggregateStatsService : IAggregateStatsService
{
    private readonly WebPScannerDbContext _context;
    private readonly ILiveScanStatsTracker _liveStatsTracker;
    private readonly ILogger<AggregateStatsService> _logger;

    public AggregateStatsService(
        WebPScannerDbContext context,
        ILiveScanStatsTracker liveStatsTracker,
        ILogger<AggregateStatsService> logger)
    {
        _context = context;
        _liveStatsTracker = liveStatsTracker;
        _logger = logger;
    }

    public async Task UpdateStatsFromCompletedScanAsync(Guid scanId, CancellationToken cancellationToken = default)
    {
        var scanJob = await _context.ScanJobs
            .Include(s => s.DiscoveredImages)
            .FirstOrDefaultAsync(s => s.ScanId == scanId, cancellationToken);

        if (scanJob is not { Status: ScanStatus.Completed })
        {
            _logger.LogWarning("Cannot update aggregate stats: Scan {ScanId} not found or not completed", scanId);
            return;
        }

        var images = scanJob.DiscoveredImages.ToList();

        // Pre-aggregate all data before the retry loop since it's immutable
        var pagesScanned = scanJob.PagesScanned;
        var imageCount = images.Count;
        var totalOriginalSize = images.Sum(i => i.FileSize);
        var totalEstimatedWebPSize = images.Sum(i => i.EstimatedWebPSize);
        var totalSavingsPercentSum = images.Sum(i => Math.Max(0, i.PotentialSavingsPercent));

        // Clamp negative savings to zero (consistent with LiveScanStatsTracker.AddImage)
        var categoryAggregates = images
            .GroupBy(i => DetermineCategory(i.ImageUrl))
            .ToDictionary(
                g => g.Key,
                g => (
                    Count: g.Count(),
                    TotalSavingsBytes: g.Sum(i => Math.Max(0, i.FileSize - i.EstimatedWebPSize)),
                    SavingsPercentSum: g.Sum(i => Math.Max(0, i.PotentialSavingsPercent))
                ));

        var imageTypeAggregates = images
            .GroupBy(i => i.MimeType)
            .ToDictionary(
                g => g.Key,
                g => (
                    DisplayName: GetDisplayName(g.Key),
                    Count: g.Count(),
                    TotalSizeBytes: g.Sum(i => i.FileSize),
                    PotentialSavingsBytes: g.Sum(i => Math.Max(0, i.FileSize - i.EstimatedWebPSize)),
                    SavingsPercentSum: g.Sum(i => Math.Max(0, i.PotentialSavingsPercent))
                ));

        const int maxRetries = 5;
        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await UpdateMainAggregateStatsAsync(
                    pagesScanned, imageCount, totalOriginalSize,
                    totalEstimatedWebPSize, totalSavingsPercentSum, cancellationToken);

                foreach (var (category, stats) in categoryAggregates)
                {
                    await UpsertCategoryStatAsync(category, stats.Count, stats.TotalSavingsBytes,
                        stats.SavingsPercentSum, cancellationToken);
                }

                foreach (var (mimeType, stats) in imageTypeAggregates)
                {
                    await UpsertImageTypeStatAsync(mimeType, stats.DisplayName, stats.Count,
                        stats.TotalSizeBytes, stats.PotentialSavingsBytes, stats.SavingsPercentSum,
                        cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);

#pragma warning disable CA1873
                _logger.LogInformation(
#pragma warning restore CA1873
                    "Updated aggregate stats from scan {ScanId}: +{Pages} pages, +{Images} images, +{Savings} bytes savings",
                    scanId, pagesScanned, imageCount, totalOriginalSize - totalEstimatedWebPSize);

                return; // Success
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync(cancellationToken);

                _logger.LogWarning(ex,
                    "Concurrency conflict updating aggregate stats (attempt {Attempt}/{MaxRetries})",
                    attempt + 1, maxRetries);

                // Detach all tracked entities to force fresh reload on next attempt
                foreach (var entry in _context.ChangeTracker.Entries().ToList())
                {
                    entry.State = EntityState.Detached;
                }

                if (attempt == maxRetries - 1)
                {
                    _logger.LogError("Failed to update aggregate stats after {MaxRetries} attempts", maxRetries);
                    throw;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(50 * Math.Pow(2, attempt)), cancellationToken);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }

    private async Task UpdateMainAggregateStatsAsync(
        int pagesScanned, int imageCount, long totalOriginalSize,
        long totalEstimatedWebPSize, double totalSavingsPercentSum,
        CancellationToken cancellationToken)
    {
        var aggregateStats = await _context.AggregateStats
            .FirstOrDefaultAsync(a => a.Id == 1, cancellationToken);

        if (aggregateStats == null)
        {
            aggregateStats = new AggregateStats { Id = 1 };
            _context.AggregateStats.Add(aggregateStats);
        }

        aggregateStats.TotalScans++;
        aggregateStats.TotalPagesCrawled += pagesScanned;
        aggregateStats.TotalImagesFound += imageCount;
        aggregateStats.TotalOriginalSizeBytes += totalOriginalSize;
        aggregateStats.TotalEstimatedWebPSizeBytes += totalEstimatedWebPSize;
        aggregateStats.TotalSavingsPercentSum += totalSavingsPercentSum;
        aggregateStats.LastUpdated = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertCategoryStatAsync(
        string category, int count, long totalSavingsBytes, double savingsPercentSum,
        CancellationToken cancellationToken)
    {
        // Use raw SQL for atomic upsert (INSERT ... ON CONFLICT for SQLite)
        await _context.Database.ExecuteSqlInterpolatedAsync($"""

                                                                         INSERT INTO AggregateCategoryStats (AggregateStatsId, Category, Count, TotalSavingsBytes, SavingsPercentSum)
                                                                         VALUES (1, {category}, {count}, {totalSavingsBytes}, {savingsPercentSum})
                                                                         ON CONFLICT(Category) DO UPDATE SET
                                                                             Count = Count + excluded.Count,
                                                                             TotalSavingsBytes = TotalSavingsBytes + excluded.TotalSavingsBytes,
                                                                             SavingsPercentSum = SavingsPercentSum + excluded.SavingsPercentSum
                                                             """,
            cancellationToken);
    }

    private async Task UpsertImageTypeStatAsync(
        string mimeType, string displayName, int count, long totalSizeBytes,
        long potentialSavingsBytes, double savingsPercentSum,
        CancellationToken cancellationToken)
    {
        // Use raw SQL for atomic upsert (INSERT ... ON CONFLICT for SQLite)
        await _context.Database.ExecuteSqlInterpolatedAsync($"""

                                                                         INSERT INTO AggregateImageTypeStats (AggregateStatsId, MimeType, DisplayName, Count, TotalSizeBytes, PotentialSavingsBytes, SavingsPercentSum)
                                                                         VALUES (1, {mimeType}, {displayName}, {count}, {totalSizeBytes}, {potentialSavingsBytes}, {savingsPercentSum})
                                                                         ON CONFLICT(MimeType) DO UPDATE SET
                                                                             Count = Count + excluded.Count,
                                                                             TotalSizeBytes = TotalSizeBytes + excluded.TotalSizeBytes,
                                                                             PotentialSavingsBytes = PotentialSavingsBytes + excluded.PotentialSavingsBytes,
                                                                             SavingsPercentSum = SavingsPercentSum + excluded.SavingsPercentSum
                                                             """,
            cancellationToken);
    }

    public async Task<AggregateStatsDto> GetCombinedStatsAsync(CancellationToken cancellationToken = default)
    {
        // Get aggregated stats (historical data from completed scans)
        var aggregateStats = await _context.AggregateStats
            .Include(a => a.CategoryStats)
            .Include(a => a.ImageTypeStats)
            .FirstOrDefaultAsync(a => a.Id == 1, cancellationToken);

        // Live stats from in-memory tracker (scans currently in progress)
        var (liveScans, livePages, liveImages, liveOriginalSize, liveEstimatedWebPSize, liveSavingsPercentSum,
            liveImageTypeStats, liveCategoryStats) = _liveStatsTracker.GetCombinedLiveStats();

        var totalImagesFound = (aggregateStats?.TotalImagesFound ?? 0) + liveImages;
        var totalSavingsPercentSum = (aggregateStats?.TotalSavingsPercentSum ?? 0) + liveSavingsPercentSum;

        var mergedCategories = new Dictionary<string, (int Count, long TotalSavingsBytes, double SavingsPercentSum)>();

        if (aggregateStats?.CategoryStats != null)
        {
            foreach (var cat in aggregateStats.CategoryStats)
            {
                mergedCategories[cat.Category] = (cat.Count, cat.TotalSavingsBytes, cat.SavingsPercentSum);
            }
        }

        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var kvp in liveCategoryStats)
        {
            var liveCat = kvp.Value;
            if (mergedCategories.TryGetValue(liveCat.Category, out var existing))
            {
                mergedCategories[liveCat.Category] = (
                    existing.Count + liveCat.Count,
                    existing.TotalSavingsBytes + liveCat.TotalSavingsBytes,
                    existing.SavingsPercentSum + liveCat.SavingsPercentSum
                );
            }
            else
            {
                mergedCategories[liveCat.Category] = (liveCat.Count, liveCat.TotalSavingsBytes, liveCat.SavingsPercentSum);
            }
        }

        var mergedImageTypes = new Dictionary<string, (string DisplayName, int Count, long TotalSizeBytes, long PotentialSavingsBytes, double SavingsPercentSum)>();

        if (aggregateStats?.ImageTypeStats != null)
        {
            foreach (var type in aggregateStats.ImageTypeStats)
            {
                mergedImageTypes[type.MimeType] = (type.DisplayName, type.Count, type.TotalSizeBytes, type.PotentialSavingsBytes, type.SavingsPercentSum);
            }
        }
        // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
        foreach (var kvp in liveImageTypeStats)
        {
            var liveType = kvp.Value;
            if (mergedImageTypes.TryGetValue(liveType.MimeType, out var existing))
            {
                mergedImageTypes[liveType.MimeType] = (
                    existing.DisplayName,
                    existing.Count + liveType.Count,
                    existing.TotalSizeBytes + liveType.TotalSizeBytes,
                    existing.PotentialSavingsBytes + liveType.PotentialSavingsBytes,
                    existing.SavingsPercentSum + liveType.SavingsPercentSum
                );
            }
            else
            {
                mergedImageTypes[liveType.MimeType] = (GetDisplayName(liveType.MimeType), liveType.Count, liveType.TotalSizeBytes, liveType.PotentialSavingsBytes, liveType.SavingsPercentSum);
            }
        }

        var result = new AggregateStatsDto
        {
            TotalScans = (aggregateStats?.TotalScans ?? 0) + liveScans,
            TotalPagesCrawled = (aggregateStats?.TotalPagesCrawled ?? 0) + livePages,
            TotalImagesFound = totalImagesFound,
            TotalOriginalSizeBytes = (aggregateStats?.TotalOriginalSizeBytes ?? 0) + liveOriginalSize,
            TotalEstimatedWebPSizeBytes = (aggregateStats?.TotalEstimatedWebPSizeBytes ?? 0) + liveEstimatedWebPSize,
            AverageSavingsPercent = totalImagesFound > 0
                ? totalSavingsPercentSum / totalImagesFound
                : 0,
            ImageTypeBreakdown = mergedImageTypes
                .Select(kvp => new ImageTypeStat
                {
                    MimeType = kvp.Key,
                    DisplayName = kvp.Value.DisplayName,
                    Count = kvp.Value.Count,
                    TotalSizeBytes = kvp.Value.TotalSizeBytes,
                    PotentialSavingsBytes = kvp.Value.PotentialSavingsBytes,
                    SavingsPercent = kvp.Value.Count > 0 ? kvp.Value.SavingsPercentSum / kvp.Value.Count : 0
                })
                .OrderByDescending(t => t.PotentialSavingsBytes)
                .ToList(),
            TopCategories = mergedCategories
                .Select(kvp => new CategoryStat
                {
                    Category = kvp.Key,
                    Count = kvp.Value.Count,
                    TotalSavingsBytes = kvp.Value.TotalSavingsBytes,
                    SavingsPercent = kvp.Value.Count > 0 ? kvp.Value.SavingsPercentSum / kvp.Value.Count : 0
                })
                .OrderByDescending(c => c.TotalSavingsBytes)
                .ToList()
        };

        return result;
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

    private static string GetDisplayName(string mimeType)
    {
        return mimeType.ToLowerInvariant() switch
        {
            "image/jpeg" or "image/jpg" => "JPEG",
            "image/png" => "PNG",
            "image/gif" => "GIF",
            "image/bmp" => "BMP",
            "image/tiff" => "TIFF",
            _ => mimeType.Replace("image/", "").ToUpperInvariant()
        };
    }
}
