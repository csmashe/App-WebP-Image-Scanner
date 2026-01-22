namespace WebPScanner.Core.DTOs;

/// <summary>
/// Data transfer object for application configuration response.
/// </summary>
public class AppConfigResponseDto
{
    /// <summary>
    /// Indicates whether email functionality is enabled.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public bool EmailEnabled { get; init; }
}
