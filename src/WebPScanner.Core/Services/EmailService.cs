using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using WebPScanner.Core.Configuration;
using WebPScanner.Core.Interfaces;
using WebPScanner.Core.Models;
using WebPScanner.Core.Utilities;

namespace WebPScanner.Core.Services;

/// <summary>
/// Email service implementation using SendGrid.
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailService> _logger;
    private readonly ISendGridClient? _sendGridClient;

    public EmailService(
        IOptions<EmailOptions> options,
        ILogger<EmailService> logger,
        ISendGridClient? sendGridClient = null)
    {
        _options = options.Value;
        _logger = logger;

        // Use injected client if provided (for testing), otherwise create from API key
        if (sendGridClient != null)
        {
            _sendGridClient = sendGridClient;
        }
        else
        {
            // Check environment variable first, then fall back to config
            var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY") ?? _options.ApiKey;
            if (!string.IsNullOrEmpty(apiKey))
            {
                _sendGridClient = new SendGridClient(apiKey);
            }
        }
    }

    /// <inheritdoc />
    public async Task<EmailResult> SendScanReportAsync(
        string email,
        PdfReportData reportData,
        byte[] pdfReport,
        string? convertedImagesDownloadUrl = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Email sending is disabled. Skipping email to {Email}", MaskEmail(email));
            return EmailResult.Succeeded("disabled");
        }

        if (_sendGridClient == null)
        {
            _logger.LogWarning("SendGrid API key not configured. Cannot send email to {Email}", MaskEmail(email));
            return EmailResult.Failed("SendGrid API key not configured");
        }

        // Check attachment size (use long arithmetic to avoid overflow)
        var attachmentSizeBytes = pdfReport.Length;
        var maxSizeBytes = (long)_options.MaxAttachmentSizeMb * 1024 * 1024;
        if (attachmentSizeBytes > maxSizeBytes)
        {
            _logger.LogWarning(
                "PDF attachment size ({Size} bytes) exceeds maximum ({Max} bytes) for scan {ScanId}",
                attachmentSizeBytes, maxSizeBytes, reportData.ScanId);
            return EmailResult.Failed($"PDF attachment size exceeds maximum of {_options.MaxAttachmentSizeMb}MB");
        }

        var subject = $"WebP Scan Report: {TruncateUrl(reportData.TargetUrl, 50)}";
        var htmlContent = BuildScanReportHtml(reportData, convertedImagesDownloadUrl);
        var plainTextContent = BuildScanReportPlainText(reportData, convertedImagesDownloadUrl);

        var msg = new SendGridMessage
        {
            From = new EmailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            PlainTextContent = plainTextContent,
            HtmlContent = htmlContent
        };
        msg.AddTo(new EmailAddress(email));

        // Add PDF attachment
        var attachment = new Attachment
        {
            Content = Convert.ToBase64String(pdfReport),
            Filename = $"webp-scan-report-{reportData.ScanId:N}.pdf",
            Type = "application/pdf",
            Disposition = "attachment"
        };
        msg.AddAttachment(attachment);

        return await SendWithRetryAsync(msg, reportData.ScanId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EmailResult> SendScanFailedNotificationAsync(
        string email,
        string targetUrl,
        Guid scanId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Email sending is disabled. Skipping failure notification to {Email}", MaskEmail(email));
            return EmailResult.Succeeded("disabled");
        }

        if (_sendGridClient == null)
        {
            _logger.LogWarning("SendGrid API key not configured. Cannot send failure notification to {Email}", MaskEmail(email));
            return EmailResult.Failed("SendGrid API key not configured");
        }

        var subject = $"WebP Scan Failed: {TruncateUrl(targetUrl, 50)}";
        var htmlContent = BuildScanFailedHtml(targetUrl, scanId, errorMessage);
        var plainTextContent = BuildScanFailedPlainText(targetUrl, scanId, errorMessage);

        var msg = new SendGridMessage
        {
            From = new EmailAddress(_options.FromEmail, _options.FromName),
            Subject = subject,
            PlainTextContent = plainTextContent,
            HtmlContent = htmlContent
        };
        msg.AddTo(new EmailAddress(email));

        return await SendWithRetryAsync(msg, scanId, cancellationToken);
    }

    private async Task<EmailResult> SendWithRetryAsync(
        SendGridMessage message,
        Guid scanId,
        CancellationToken cancellationToken)
    {
        var attempts = 0;
        var maxAttempts = _options.MaxRetries + 1;
        var retryDelayMs = _options.RetryDelayMinutes * 60 * 1000;

        while (attempts < maxAttempts)
        {
            attempts++;

            try
            {
                _logger.LogInformation(
                    "Sending email for scan {ScanId} (attempt {Attempt}/{MaxAttempts})",
                    scanId, attempts, maxAttempts);

                var response = await _sendGridClient!.SendEmailAsync(message, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var messageId = response.Headers.TryGetValues("X-Message-Id", out var values)
                        ? values.FirstOrDefault()
                        : null;

                    _logger.LogInformation(
                        "Email sent successfully for scan {ScanId}. MessageId: {MessageId}, StatusCode: {StatusCode}",
                        scanId, messageId, response.StatusCode);

                    return EmailResult.Succeeded(messageId, attempts - 1);
                }

                // Read response body length for diagnostics (body content may contain PII, so don't log it)
                var responseBody = await response.Body.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "SendGrid returned error for scan {ScanId}. StatusCode: {StatusCode}, BodyLength: {BodyLength}",
                    scanId, response.StatusCode, responseBody.Length);

                // Don't retry on client errors (4xx) except for rate limiting (429)
                if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500 && (int)response.StatusCode != 429)
                {
                    return EmailResult.Failed($"SendGrid error: {response.StatusCode}", attempts - 1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception sending email for scan {ScanId} (attempt {Attempt})", scanId, attempts);

                if (attempts >= maxAttempts)
                {
                    return EmailResult.Failed($"Failed after {attempts} attempts: {ex.Message}", attempts - 1);
                }
            }

            // Wait before retry (but not on the last attempt)
            if (attempts >= maxAttempts)
            {
                continue;
            }

            _logger.LogInformation(
                "Retrying email for scan {ScanId} in {DelayMinutes} minutes...",
                scanId, _options.RetryDelayMinutes);

            try
            {
                await Task.Delay(retryDelayMs, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return EmailResult.Failed("Operation cancelled during retry delay", attempts - 1);
            }
        }

        return EmailResult.Failed($"Failed after {attempts} attempts", attempts - 1);
    }

    private static string BuildScanReportHtml(PdfReportData data, string? convertedImagesDownloadUrl = null)
    {
        var savingsFormatted = ByteFormatUtility.FormatBytes(data.SavingsSummary.TotalSavingsBytes);
        var savingsPercent = data.SavingsSummary.TotalSavingsPercentage.ToString("F1");

        var downloadSection = string.IsNullOrEmpty(convertedImagesDownloadUrl)
            ? ""
            : $"""

                       <div style="background: #ECFDF5; padding: 20px; border-radius: 8px; border-left: 4px solid #10B981; margin: 20px 0; text-align: center;">
                           <strong style="color: #065F46;">📦 Your Converted WebP Images Are Ready!</strong>
                           <p style="color: #047857; margin: 10px 0;">We've converted your images to WebP format. Download them now:</p>
                           <a href="{EscapeHtml(convertedImagesDownloadUrl)}" class="cta" style="background: #10B981; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block; font-weight: 600;">Download WebP Images</a>
                           <p style="color: #6B7280; font-size: 12px; margin-top: 15px;">⚠️ This link expires in 6 hours. Download your images before it expires!</p>
                       </div>
               """;

        return $$"""

                 <!DOCTYPE html>
                 <html>
                 <head>
                     <meta charset="utf-8">
                     <meta name="viewport" content="width=device-width, initial-scale=1.0">
                     <style>
                         body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }
                         .header { background: linear-gradient(135deg, #4F46E5, #7C3AED); color: white; padding: 30px; border-radius: 8px 8px 0 0; }
                         .header h1 { margin: 0 0 10px 0; font-size: 24px; }
                         .content { background: #f9fafb; padding: 30px; border: 1px solid #e5e7eb; }
                         .stats { display: flex; justify-content: space-around; margin: 20px 0; flex-wrap: wrap; }
                         .stat { text-align: center; padding: 15px; background: white; border-radius: 8px; min-width: 100px; margin: 5px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }
                         .stat-value { font-size: 28px; font-weight: bold; color: #4F46E5; }
                         .stat-label { font-size: 12px; color: #6b7280; text-transform: uppercase; }
                         .cta { background: #10B981; color: white; padding: 15px 25px; text-decoration: none; border-radius: 6px; display: inline-block; margin-top: 20px; font-weight: 600; }
                         .footer { padding: 20px; text-align: center; font-size: 12px; color: #6b7280; }
                         .highlight { background: #FEF3C7; padding: 15px; border-radius: 6px; border-left: 4px solid #F59E0B; margin: 20px 0; }
                     </style>
                 </head>
                 <body>
                     <div class="header">
                         <h1>🖼️ WebP Scan Complete</h1>
                         <p style="margin: 0; opacity: 0.9;">Your scan results for {{EscapeHtml(data.TargetUrl)}}</p>
                     </div>
                     <div class="content">
                         <p>Good news! Your website scan has completed successfully. Here's a quick summary:</p>

                         <div class="stats">
                             <div class="stat">
                                 <div class="stat-value">{{data.SavingsSummary.ConvertibleImages}}</div>
                                 <div class="stat-label">Images Found</div>
                             </div>
                             <div class="stat">
                                 <div class="stat-value">{{savingsFormatted}}</div>
                                 <div class="stat-label">Potential Savings</div>
                             </div>
                             <div class="stat">
                                 <div class="stat-value">{{savingsPercent}}%</div>
                                 <div class="stat-label">Size Reduction</div>
                             </div>
                         </div>
                 {{downloadSection}}
                         <div class="highlight">
                             <strong>💡 Quick Tip:</strong> Converting to WebP format can significantly improve your page load times and reduce bandwidth costs.
                         </div>

                         <p>We've attached a detailed PDF report with:</p>
                         <ul>
                             <li>Complete list of images that can be optimized</li>
                             <li>Estimated savings per image</li>
                             <li>Recommendations for conversion</li>
                         </ul>

                         <p>Pages scanned: {{data.PagesScanned}} | Duration: {{data.CrawlDuration.TotalSeconds:F0}} seconds</p>
                     </div>
                     <div class="footer">
                         <p>This report was generated by WebP Scanner - a free, open-source tool.</p>
                         <p style="color: #9CA3AF; font-size: 11px;">Savings estimates are approximate and based on empirical conversion ratios.</p>
                     </div>
                 </body>
                 </html>
                 """;
    }

    private static string BuildScanReportPlainText(PdfReportData data, string? convertedImagesDownloadUrl = null)
    {
        var savingsFormatted = ByteFormatUtility.FormatBytes(data.SavingsSummary.TotalSavingsBytes);
        var savingsPercent = data.SavingsSummary.TotalSavingsPercentage.ToString("F1");

        var downloadSection = string.IsNullOrEmpty(convertedImagesDownloadUrl)
            ? ""
            : $"""


               DOWNLOAD YOUR CONVERTED WEBP IMAGES
               ===================================
               Your images have been converted to WebP format!
               Download them here: {convertedImagesDownloadUrl}

               Note: This link expires in 6 hours.

               """;

        return $"""

                WebP Scan Complete
                ==================

                Your scan results for: {data.TargetUrl}

                Summary:
                - Images found: {data.SavingsSummary.ConvertibleImages}
                - Potential savings: {savingsFormatted}
                - Size reduction: {savingsPercent}%

                Pages scanned: {data.PagesScanned}
                Duration: {data.CrawlDuration.TotalSeconds:F0} seconds
                {downloadSection}
                Please see the attached PDF report for detailed findings and recommendations.

                ---
                This report was generated by WebP Scanner - a free, open-source tool.
                Savings estimates are approximate and based on empirical conversion ratios.

                """;
    }

    private static string BuildScanFailedHtml(string targetUrl, Guid scanId, string errorMessage)
    {
        return $$"""

                 <!DOCTYPE html>
                 <html>
                 <head>
                     <meta charset="utf-8">
                     <meta name="viewport" content="width=device-width, initial-scale=1.0">
                     <style>
                         body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; }
                         .header { background: linear-gradient(135deg, #DC2626, #EF4444); color: white; padding: 30px; border-radius: 8px 8px 0 0; }
                         .header h1 { margin: 0 0 10px 0; font-size: 24px; }
                         .content { background: #f9fafb; padding: 30px; border: 1px solid #e5e7eb; }
                         .error-box { background: #FEE2E2; padding: 15px; border-radius: 6px; border-left: 4px solid #DC2626; margin: 20px 0; }
                         .footer { padding: 20px; text-align: center; font-size: 12px; color: #6b7280; }
                     </style>
                 </head>
                 <body>
                     <div class="header">
                         <h1>❌ WebP Scan Failed</h1>
                         <p style="margin: 0; opacity: 0.9;">We couldn't complete the scan for {{EscapeHtml(targetUrl)}}</p>
                     </div>
                     <div class="content">
                         <p>Unfortunately, we encountered an error while scanning your website.</p>

                         <div class="error-box">
                             <strong>Error Details:</strong><br>
                             {{EscapeHtml(errorMessage)}}
                         </div>

                         <p><strong>Common reasons for scan failures:</strong></p>
                         <ul>
                             <li>The website is behind authentication</li>
                             <li>The website blocked our crawler</li>
                             <li>Network connectivity issues</li>
                             <li>The website took too long to respond</li>
                         </ul>

                         <p>You can try submitting a new scan request. If the problem persists, the website may not be compatible with our scanner.</p>

                         <p style="font-size: 12px; color: #6b7280;">Scan ID: {{scanId}}</p>
                     </div>
                     <div class="footer">
                         <p>This notification was sent by WebP Scanner - a free, open-source tool.</p>
                     </div>
                 </body>
                 </html>
                 """;
    }

    private static string BuildScanFailedPlainText(string targetUrl, Guid scanId, string errorMessage)
    {
        return $"""

                WebP Scan Failed
                ================

                We couldn't complete the scan for: {targetUrl}

                Error Details:
                {errorMessage}

                Common reasons for scan failures:
                - The website is behind authentication
                - The website blocked our crawler
                - Network connectivity issues
                - The website took too long to respond

                You can try submitting a new scan request. If the problem persists, the website may not be compatible with our scanner.

                Scan ID: {scanId}

                ---
                This notification was sent by WebP Scanner - a free, open-source tool.

                """;
    }

    private static string EscapeHtml(string text)
    {
        return System.Web.HttpUtility.HtmlEncode(text);
    }

    private static string TruncateUrl(string url, int maxLength)
    {
        if (string.IsNullOrEmpty(url) || url.Length <= maxLength)
            return url;

        // Guard against small maxLength values that would cause negative slice indices
        if (maxLength <= 3)
            return url.Length <= maxLength ? url : new string('.', maxLength);

        return url[..(maxLength - 3)] + "...";
    }

    /// <summary>
    /// Masks an email address for safe logging (PII protection).
    /// Example: "john.doe@example.com" becomes "jo***@ex***.com"
    /// </summary>
    private static string MaskEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
            return "[empty]";

        var atIndex = email.IndexOf('@');
        if (atIndex < 1)
            return "***@***";

        var localPart = email[..atIndex];
        var domainPart = email[(atIndex + 1)..];

        // Mask local part: show first 2 chars (or 1 if shorter), then ***
        var maskedLocal = localPart.Length <= 2
            ? localPart[..1] + "***"
            : localPart[..2] + "***";

        // Mask domain: show first 2 chars, then ***, then TLD
        var lastDotIndex = domainPart.LastIndexOf('.');
        if (lastDotIndex < 1)
        {
            // No TLD found, just mask the whole domain
            return $"{maskedLocal}@***";
        }

        var domainName = domainPart[..lastDotIndex];
        var tld = domainPart[lastDotIndex..]; // includes the dot

        var maskedDomain = domainName.Length <= 2
            ? domainName[..1] + "***"
            : domainName[..2] + "***";

        return $"{maskedLocal}@{maskedDomain}{tld}";
    }
}
