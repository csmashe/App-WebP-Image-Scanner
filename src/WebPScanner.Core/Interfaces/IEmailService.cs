using WebPScanner.Core.Models;

namespace WebPScanner.Core.Interfaces;

/// <summary>
/// Service interface for sending email reports.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a scan report email with PDF attachment.
    /// </summary>
    /// <param name="email">Recipient email address.</param>
    /// <param name="reportData">Data for the scan report.</param>
    /// <param name="pdfReport">PDF report as byte array.</param>
    /// <param name="convertedImagesDownloadUrl">Optional URL to download converted WebP images.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure with details.</returns>
    Task<EmailResult> SendScanReportAsync(
        string email,
        PdfReportData reportData,
        byte[] pdfReport,
        string? convertedImagesDownloadUrl = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification that a scan has failed.
    /// </summary>
    /// <param name="email">Recipient email address.</param>
    /// <param name="targetUrl">The URL that was being scanned.</param>
    /// <param name="scanId">The scan ID.</param>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure with details.</returns>
    Task<EmailResult> SendScanFailedNotificationAsync(
        string email,
        string targetUrl,
        Guid scanId,
        string errorMessage,
        CancellationToken cancellationToken = default);
}
