namespace WebPScanner.Core.Configuration;

/// <summary>
/// Configuration options for email delivery.
/// </summary>
public class EmailOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Email";

    /// <summary>
    /// The SendGrid API key. Can be overridden by SENDGRID_API_KEY environment variable.
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>
    /// The sender email address.
    /// </summary>
    public string FromEmail { get; init; } = "noreply@example.com";

    /// <summary>
    /// The sender display name.
    /// </summary>
    public string FromName { get; init; } = "WebP Scanner";

    /// <summary>
    /// Maximum number of retry attempts for failed sends.
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Delay between retry attempts in minutes.
    /// </summary>
    public int RetryDelayMinutes { get; init; } = 5;

    /// <summary>
    /// Whether to enable email sending. Set to false to disable (useful for testing).
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Maximum attachment size in MB. Default is 10MB.
    /// </summary>
    public int MaxAttachmentSizeMb { get; init; } = 10;
}
