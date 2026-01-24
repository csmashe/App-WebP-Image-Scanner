using WebPScanner.Core.DTOs;

namespace WebPScanner.Core.Interfaces;

/// <summary>
/// Service for managing aggregated statistics across all scans.
/// </summary>
public interface IAggregateStatsService
{
    /// <summary>
    /// Updates the aggregate stats when a scan completes.
    /// Adds the scan's data to the running totals.
    /// </summary>
    Task UpdateStatsFromCompletedScanAsync(Guid scanId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the combined stats from aggregate tables plus any live (non-purged) data.
    /// </summary>
    Task<AggregateStatsDto> GetCombinedStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the average time per page in ticks based on historical scan data.
    /// Returns 0 if no data is available yet.
    /// </summary>
    Task<long> GetAverageTimePerPageTicksAsync(CancellationToken cancellationToken = default);
}
