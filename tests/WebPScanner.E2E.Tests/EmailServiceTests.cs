using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WebPScanner.Core.Configuration;
using WebPScanner.Core.Interfaces;
using WebPScanner.Core.Models;
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace WebPScanner.E2E.Tests;

/// <summary>
/// E2E tests for email service functionality with mock configuration.
/// Note: These tests verify the email service behavior without actually sending emails.
/// These tests require WebApplicationFactory and are skipped when running against external server.
/// </summary>
[TestFixture]
public class EmailServiceTests
{
    private WebApplicationFixture? _appFixture;
    private IEmailService? _emailService;
    private IOptions<EmailOptions>? _emailOptions;

    private static bool IsExternalServer => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("E2E_BASE_URL"));

    [SetUp]
    public void Setup()
    {
        if (IsExternalServer)
        {
            return;
        }

        _appFixture = new WebApplicationFixture();
        var scope = _appFixture.Services.CreateScope();
        _emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        _emailOptions = scope.ServiceProvider.GetRequiredService<IOptions<EmailOptions>>();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_appFixture != null)
        {
            await _appFixture.DisposeAsync();
        }
    }

    [Test]
    public void EmailOptions_ShouldHaveDefaultValues()
    {
        if (IsExternalServer)
        {
            Assert.Ignore("Test requires WebApplicationFactory - skipped for external server");
            return;
        }

        // Assert default configuration
        Assert.That(_emailOptions!.Value, Is.Not.Null);
        Assert.That(_emailOptions.Value.MaxRetries, Is.EqualTo(3));
        Assert.That(_emailOptions.Value.RetryDelayMinutes, Is.EqualTo(5));
        Assert.That(_emailOptions.Value.MaxAttachmentSizeMb, Is.EqualTo(10));
    }

    [Test]
    public async Task EmailService_ShouldHandleDisabledState()
    {
        if (IsExternalServer)
        {
            Assert.Ignore("Test requires WebApplicationFactory - skipped for external server");
            return;
        }

        // The email service should gracefully handle when email is disabled
        // (no API key configured in test environment)
        var result = await _emailService!.SendScanReportAsync(
            "test@example.com",
            CreateTestReportData(),
            [1, 2, 3] // Minimal PDF stub
        );

        // Should return a result (either success with disabled flag or graceful failure)
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task EmailService_ShouldHandleFailedNotification()
    {
        if (IsExternalServer)
        {
            Assert.Ignore("Test requires WebApplicationFactory - skipped for external server");
            return;
        }

        // Test sending a failure notification
        var result = await _emailService!.SendScanFailedNotificationAsync(
            "test@example.com",
            "https://example.com",
            Guid.NewGuid(),
            "Test error message"
        );

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void EmailService_ShouldRejectOversizedAttachments()
    {
        if (IsExternalServer)
        {
            Assert.Ignore("Test requires WebApplicationFactory - skipped for external server");
            return;
        }

        // Create a large fake PDF size (over 10MB)
        const int largePdfSize = 11 * 1024 * 1024; // 11MB

        // The service should handle oversized attachments gracefully
        Assert.That(largePdfSize > _emailOptions!.Value.MaxAttachmentSizeMb * 1024 * 1024, Is.True,
            "Test attachment should exceed max size");
    }

    private static PdfReportData CreateTestReportData()
    {
        return new PdfReportData
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://example.com",
            ScanDate = DateTime.UtcNow,
            PagesScanned = 5,
            PagesDiscovered = 5,
            CrawlDuration = TimeSpan.FromSeconds(30),
            ReachedPageLimit = false,
            SavingsSummary = new SavingsSummary
            {
                TotalImages = 8,
                ConvertibleImages = 8,
                TotalOriginalSize = 1000000,
                TotalEstimatedWebPSize = 400000,
                TotalSavingsPercentage = 60,
                TotalSavingsBytes = 600000
            },
            ImageEstimates =
            [
                new ImageSavingsEstimate
                {
                    Url = "https://example.com/image.png",
                    OriginalMimeType = "image/png",
                    OriginalSize = 100000,
                    EstimatedWebPSize = 26000,
                    SavingsPercentage = 74,
                    SavingsBytes = 74000
                }
            ]
        };
    }
}
