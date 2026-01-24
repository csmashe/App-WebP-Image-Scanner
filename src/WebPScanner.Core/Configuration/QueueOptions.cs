namespace WebPScanner.Core.Configuration;

public class QueueOptions
{
    public const string SectionName = "Queue";

    /// <summary>
    /// Maximum number of scans that can run concurrently.
    /// </summary>
    public int MaxConcurrentScans { get; init; } = 2;

    /// <summary>
    /// Maximum number of jobs allowed in the queue.
    /// </summary>
    public int MaxQueueSize { get; init; } = 100;

    /// <summary>
    /// Maximum number of queued jobs allowed per IP address (0 = unlimited).
    /// </summary>
    public int MaxQueuedJobsPerIp { get; init; } = 20;

    /// <summary>
    /// Slot multiplier for fair-share queue ordering.
    /// Jobs are ordered primarily by their submission count (1st job, 2nd job, etc.),
    /// then by creation time within the same slot. Higher values = stronger interleaving.
    /// </summary>
    public long FairnessSlotTicks { get; init; } = TimeSpan.TicksPerHour;

    /// <summary>
    /// Boost in seconds subtracted from priority score for jobs waiting longer.
    /// This prevents starvation by gradually increasing priority of older jobs.
    /// </summary>
    public int PriorityAgingBoostSeconds { get; init; } = 30;

    /// <summary>
    /// Cooldown period in seconds after a scan completes before the same IP can submit again.
    /// init to 0 to disable cooldown.
    /// </summary>
    public int CooldownAfterScanSeconds { get; init; }

    /// <summary>
    /// Interval in seconds for the background queue processor to check for new jobs.
    /// </summary>
    public int ProcessingIntervalSeconds { get; init; } = 5;

    /// <summary>
    /// Default estimated number of pages for sites that haven't started scanning yet.
    /// Used for queue wait time estimation. Once a scan begins, the actual discovered
    /// page count replaces this estimate. Set to a reasonable average - larger sites
    /// will show corrected estimates once scanning starts.
    /// </summary>
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global - init required for config binding
    public int DefaultEstimatedPagesPerSite { get; init; } = 100;
}
