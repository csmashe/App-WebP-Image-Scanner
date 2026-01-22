namespace WebPScanner.Core.DTOs;

/// <summary>
/// Data transfer object for health check response with queue statistics.
/// </summary>
public class HealthResponseDto
{
    /// <summary>
    /// The overall health status of the service.
    /// </summary>
    public string Status { get; init; } = "Healthy";

    /// <summary>
    /// Number of jobs currently in the queue.
    /// </summary>
    public int QueuedJobs { get; init; }

    /// <summary>
    /// Number of jobs currently being processed.
    /// </summary>
    public int ProcessingJobs { get; init; }

    /// <summary>
    /// The current timestamp.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
