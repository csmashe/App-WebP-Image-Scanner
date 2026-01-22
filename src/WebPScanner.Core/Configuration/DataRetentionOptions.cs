// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace WebPScanner.Core.Configuration;

public class DataRetentionOptions
{
    public const string SectionName = "DataRetention";

    /// <summary>
    /// Number of hours to retain scan data before purging.
    /// Set to 0 to disable automatic purging.
    /// Default is 168 hours (7 days).
    /// </summary>
    public int RetentionHours { get; init; } = 168;

    /// <summary>
    /// Interval in minutes between retention cleanup runs.
    /// Default is 60 minutes (1 hour).
    /// </summary>
    public int CleanupIntervalMinutes { get; init; } = 60;

    /// <summary>
    /// Maximum number of scans to delete per cleanup run.
    /// This prevents long-running cleanup operations.
    /// Default is 100.
    /// </summary>
    public int MaxDeletesPerRun { get; init; } = 100;
}
