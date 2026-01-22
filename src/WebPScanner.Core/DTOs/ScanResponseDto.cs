namespace WebPScanner.Core.DTOs;

/// <summary>
/// Data transfer object for scan submission response.
/// </summary>
public class ScanResponseDto
{
    /// <summary>
    /// The unique identifier for the scan job.
    /// </summary>
    public Guid ScanId { get; init; }

    /// <summary>
    /// The current position in the queue (1-based).
    /// </summary>
    public int QueuePosition { get; init; }

    /// <summary>
    /// A message describing the scan status.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Whether WebP conversion was requested for this scan.
    /// </summary>
    public bool ConvertToWebP { get; init; }
}
