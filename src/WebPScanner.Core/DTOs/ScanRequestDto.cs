using System.ComponentModel.DataAnnotations;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace WebPScanner.Core.DTOs;

/// <summary>
/// Data transfer object for scan submission requests.
/// </summary>
public class ScanRequestDto
{
    /// <summary>
    /// The URL of the website to scan for non-WebP images.
    /// Must be a valid HTTP or HTTPS URL.
    /// </summary>
    [Required(ErrorMessage = "URL is required.")]
    [Url(ErrorMessage = "Invalid URL format.")]
    [RegularExpression("^(?i)https?://", ErrorMessage = "Only HTTP and HTTPS URLs are allowed.")]
    public string Url { get; init; } = string.Empty;

    /// <summary>
    /// The email address where the scan report will be sent.
    /// Optional - if not provided, no email will be sent.
    /// </summary>
    [EmailAddress(ErrorMessage = "Invalid email address format.")]
    [MaxLength(254, ErrorMessage = "Email address is too long.")]
    public string? Email { get; init; }

    /// <summary>
    /// Whether to convert discovered non-WebP images to WebP format
    /// and provide a downloadable zip file.
    /// </summary>
    public bool ConvertToWebP { get; init; }
}
