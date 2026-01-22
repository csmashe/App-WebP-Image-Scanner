// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace WebPScanner.Core.DTOs;

/// <summary>
/// Snapshot of current scan progress state, returned to clients on reconnection.
/// Contains the latest known state of a scan to sync browser state after disconnect.
/// </summary>
public record ScanProgressSnapshot
{
    /// <summary>
    /// Current status of the scan (Queued, Processing, Completed, Failed).
    /// </summary>
    public string Status { get; init; } = "";

    /// <summary>
    /// Position in the queue (0 if not queued or currently processing).
    /// </summary>
    public int QueuePosition { get; init; }

    /// <summary>
    /// Number of pages that have been scanned.
    /// </summary>
    public int PagesScanned { get; init; }

    /// <summary>
    /// Total number of pages discovered (including unvisited).
    /// </summary>
    public int PagesDiscovered { get; init; }

    /// <summary>
    /// Number of non-WebP images found so far.
    /// </summary>
    public int NonWebPImagesCount { get; init; }

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public int ProgressPercent { get; init; }

    /// <summary>
    /// The URL currently being processed, if any.
    /// </summary>
    public string? CurrentUrl { get; init; }

    /// <summary>
    /// Error message if the scan failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
