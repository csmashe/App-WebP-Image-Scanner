namespace WebPScanner.Core.Models;

/// <summary>
/// Result of an email send operation.
/// </summary>
public class EmailResult
{
    /// <summary>
    /// Whether the email was sent successfully.
    /// </summary>
    public bool Success { get; private init; }

    /// <summary>
    /// Error message if send failed.
    /// </summary>
    public string? ErrorMessage { get; private init; }

    /// <summary>
    /// The SendGrid message ID if available.
    /// </summary>
    public string? MessageId { get; private init; }

    /// <summary>
    /// Number of retry attempts made.
    /// </summary>
    public int RetryAttempts { get; private init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static EmailResult Succeeded(string? messageId = null, int retryAttempts = 0) =>
        new() { Success = true, MessageId = messageId, RetryAttempts = retryAttempts };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static EmailResult Failed(string errorMessage, int retryAttempts = 0) =>
        new() { Success = false, ErrorMessage = errorMessage, RetryAttempts = retryAttempts };
}
