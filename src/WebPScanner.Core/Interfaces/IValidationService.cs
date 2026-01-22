using WebPScanner.Core.Models;

namespace WebPScanner.Core.Interfaces;

/// <summary>
/// Service for validating user inputs including URLs and email addresses.
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Validates both URL and email for a scan request.
    /// </summary>
    /// <param name="url">The URL to validate.</param>
    /// <param name="email">The email address to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A validation result indicating success or failure with error messages.</returns>
    Task<ValidationResult> ValidateScanRequestAsync(string? url, string? email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a hostname for SSRF vulnerabilities by checking if it resolves to private/internal IPs.
    /// Use this for defense-in-depth at crawl time to prevent DNS rebinding attacks.
    /// </summary>
    /// <param name="host">The hostname to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A validation result indicating if the host is safe to connect to.</returns>
    Task<ValidationResult> ValidateHostSsrfAsync(string host, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an IP address is private or reserved (not safe for external connections).
    /// Use this for post-connection validation to detect DNS rebinding attacks.
    /// </summary>
    /// <param name="ipAddress">The IP address to check.</param>
    /// <returns>True if the IP is private/reserved and should be blocked, false if safe.</returns>
    bool IsPrivateOrReservedIp(string ipAddress);
}
