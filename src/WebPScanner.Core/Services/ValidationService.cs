using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using WebPScanner.Core.Interfaces;
using WebPScanner.Core.Models;
using WebPScanner.Core.Utilities;

namespace WebPScanner.Core.Services;

/// <summary>
/// Implementation of validation service for URLs and email addresses.
/// Includes SSRF prevention by blocking private/internal IP ranges.
/// </summary>
public partial class ValidationService : IValidationService
{
    // RFC 5322 compliant email regex (simplified but effective)
    [GeneratedRegex(@"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    private static async Task<ValidationResult> ValidateUrlAsync(string? url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return ValidationResult.Failure("URL is required.");
        }

        // Try to parse the URL
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return ValidationResult.Failure("Invalid URL format.");
        }

        // Only allow http and https schemes
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return ValidationResult.Failure("Only HTTP and HTTPS URLs are allowed.");
        }

        // Check for localhost variants
        if (IsLocalhost(uri.Host))
        {
            return ValidationResult.Failure("Localhost URLs are not allowed.");
        }

        // Resolve hostname and check for private/internal IPs (SSRF prevention)
        var ssrfResult = await CheckForSsrfVulnerabilityAsync(uri.Host, cancellationToken);
        return !ssrfResult.IsValid ? ssrfResult : ValidationResult.Success();
    }

    private static ValidationResult ValidateEmail(string? email)
    {
        // Email is optional - if not provided, skip validation
        if (string.IsNullOrWhiteSpace(email))
        {
            return ValidationResult.Success();
        }

        if (email.Length > 254)
        {
            return ValidationResult.Failure("Email address is too long.");
        }

        if (!EmailRegex().IsMatch(email))
        {
            return ValidationResult.Failure("Invalid email address format.");
        }

        // Check for valid TLD (at least 2 characters after last dot)
        var lastDotIndex = email.LastIndexOf('.');
        if (lastDotIndex == -1 || email.Length - lastDotIndex - 1 < 2)
        {
            return ValidationResult.Failure("Invalid email address format.");
        }

        return ValidationResult.Success();
    }

    public async Task<ValidationResult> ValidateScanRequestAsync(string? url, string? email, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        var urlResult = await ValidateUrlAsync(url, cancellationToken);
        if (!urlResult.IsValid)
        {
            errors.AddRange(urlResult.Errors);
        }

        var emailResult = ValidateEmail(email);
        if (!emailResult.IsValid)
        {
            errors.AddRange(emailResult.Errors);
        }

        return errors.Count > 0
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }

    public Task<ValidationResult> ValidateHostSsrfAsync(string host, CancellationToken cancellationToken = default)
    {
        return CheckForSsrfVulnerabilityAsync(host, cancellationToken);
    }

    public bool IsPrivateOrReservedIp(string ipAddress)
    {
        return IpRangeHelper.IsPrivateOrReservedIp(ipAddress);
    }

    private static bool IsLocalhost(string host)
    {
        var lowerHost = host.ToLowerInvariant();
        return lowerHost == "localhost" ||
               lowerHost == "127.0.0.1" ||
               lowerHost == "::1" ||
               lowerHost.EndsWith(".localhost") ||
               lowerHost == "[::1]";
    }

    private static async Task<ValidationResult> CheckForSsrfVulnerabilityAsync(string host, CancellationToken cancellationToken)
    {
        // First check if host is an IP address directly
        if (IPAddress.TryParse(host, out var directIp))
        {
	        return IpRangeHelper.IsPrivateOrReservedIp(directIp) ?
		        ValidationResult.Failure("Private or internal IP addresses are not allowed.") : ValidationResult.Success();
        }

        // For hostnames, try to resolve to IP addresses asynchronously
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, cancellationToken);
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var address in addresses)
            {
                if (IpRangeHelper.IsPrivateOrReservedIp(address))
                {
                    return ValidationResult.Failure("URL resolves to a private or internal IP address.");
                }
            }
        }
        catch (SocketException)
        {
            return ValidationResult.Failure("Unable to resolve hostname.");
        }

        return ValidationResult.Success();
    }
}
