namespace WebPScanner.Core.Enums;

/// <summary>
/// Status of a scan job in its lifecycle.
/// </summary>
public enum ScanStatus
{
    /// <summary>
    /// Scan is waiting in the queue to be processed.
    /// </summary>
    Queued,

    /// <summary>
    /// Scan is currently being processed.
    /// </summary>
    Processing,

    /// <summary>
    /// Scan completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Scan failed with an error.
    /// </summary>
    Failed
}
