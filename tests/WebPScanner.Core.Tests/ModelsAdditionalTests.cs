using WebPScanner.Core.Configuration;
using WebPScanner.Core.Models;

namespace WebPScanner.Core.Tests;

/// <summary>
/// Additional tests for models and configuration classes.
/// </summary>
public class ModelsAdditionalTests
{
    #region PdfReportData Tests

    [Test]
    public void PdfReportData_CanSetAllProperties()
    {
        var scanId = Guid.NewGuid();
        var scanDate = DateTime.UtcNow;

        var data = new PdfReportData
        {
            ScanId = scanId,
            TargetUrl = "https://example.com",
            ScanDate = scanDate,
            PagesScanned = 50,
            PagesDiscovered = 100,
            CrawlDuration = TimeSpan.FromMinutes(5),
            ReachedPageLimit = true,
            SavingsSummary = new SavingsSummary(),
            ImageEstimates =
            [
                new ImageSavingsEstimate
                {
                    Url = "https://example.com/img.jpg",
                    OriginalMimeType = "image/jpeg"
                }
            ],
            ImageToPagesMap = new Dictionary<string, List<string>>
            {
                { "https://example.com/img.jpg", ["https://example.com/page"] }
            }
        };

        Assert.That(data.ScanId, Is.EqualTo(scanId));
        Assert.That(data.TargetUrl, Is.EqualTo("https://example.com"));
        Assert.That(data.ScanDate, Is.EqualTo(scanDate));
        Assert.That(data.PagesScanned, Is.EqualTo(50));
        Assert.That(data.PagesDiscovered, Is.EqualTo(100));
        Assert.That(data.CrawlDuration, Is.EqualTo(TimeSpan.FromMinutes(5)));
        Assert.That(data.ReachedPageLimit, Is.True);
        Assert.That(data.SavingsSummary, Is.Not.Null);
        Assert.That(data.ImageEstimates, Has.Count.EqualTo(1));
        Assert.That(data.ImageToPagesMap, Has.Count.EqualTo(1));
    }

    [Test]
    public void PdfReportData_DefaultValues()
    {
        var data = new PdfReportData();

        Assert.That(data.ScanId, Is.EqualTo(Guid.Empty));
        Assert.That(data.TargetUrl, Is.EqualTo(string.Empty));
        Assert.That(data.PagesScanned, Is.EqualTo(0));
        Assert.That(data.PagesDiscovered, Is.EqualTo(0));
        Assert.That(data.CrawlDuration, Is.EqualTo(TimeSpan.Zero));
        Assert.That(data.ReachedPageLimit, Is.False);
        Assert.That(data.SavingsSummary, Is.Not.Null);
        Assert.That(data.ImageEstimates, Is.Not.Null);
        Assert.That(data.ImageEstimates, Is.Empty);
        Assert.That(data.ImageToPagesMap, Is.Not.Null);
        Assert.That(data.ImageToPagesMap, Is.Empty);
    }

    #endregion

    #region SavingsSummary Tests

    [Test]
    public void SavingsSummary_CanSetAllProperties()
    {
        var summary = new SavingsSummary
        {
            TotalImages = 100,
            ConvertibleImages = 80,
            TotalOriginalSize = 10485760,
            TotalEstimatedWebPSize = 5242880,
            TotalSavingsBytes = 5242880,
            TotalSavingsPercentage = 50.0,
            ByType = new Dictionary<string, TypeSavingsSummary>
            {
                {
                    "image/jpeg", new TypeSavingsSummary
                    {
                        MimeType = "image/jpeg",
                        Count = 50,
                        TotalOriginalSize = 5000000,
                        TotalEstimatedWebPSize = 3750000,
                        TotalSavingsBytes = 1250000,
                        SavingsPercentage = 25.0
                    }
                }
            }
        };

        Assert.That(summary.TotalImages, Is.EqualTo(100));
        Assert.That(summary.ConvertibleImages, Is.EqualTo(80));
        Assert.That(summary.TotalOriginalSize, Is.EqualTo(10485760));
        Assert.That(summary.TotalEstimatedWebPSize, Is.EqualTo(5242880));
        Assert.That(summary.TotalSavingsBytes, Is.EqualTo(5242880));
        Assert.That(summary.TotalSavingsPercentage, Is.EqualTo(50.0));
        Assert.That(summary.ByType, Has.Count.EqualTo(1));
    }

    [Test]
    public void SavingsSummary_DefaultValues()
    {
        var summary = new SavingsSummary();

        Assert.That(summary.TotalImages, Is.EqualTo(0));
        Assert.That(summary.ConvertibleImages, Is.EqualTo(0));
        Assert.That(summary.TotalOriginalSize, Is.EqualTo(0));
        Assert.That(summary.TotalEstimatedWebPSize, Is.EqualTo(0));
        Assert.That(summary.TotalSavingsBytes, Is.EqualTo(0));
        Assert.That(summary.TotalSavingsPercentage, Is.EqualTo(0));
        Assert.That(summary.ByType, Is.Not.Null);
        Assert.That(summary.ByType, Is.Empty);
    }

    [Test]
    public void SavingsSummary_HasDisclaimer()
    {
        var summary = new SavingsSummary();

        Assert.That(summary.Disclaimer, Is.Not.Null);
        Assert.That(summary.Disclaimer, Does.Contain("estimate").IgnoreCase);
    }

    #endregion

    #region TypeSavingsSummary Tests

    [Test]
    public void TypeSavingsSummary_CanSetAllProperties()
    {
        var typeSummary = new TypeSavingsSummary
        {
            MimeType = "image/png",
            Count = 25,
            TotalOriginalSize = 2500000,
            TotalEstimatedWebPSize = 650000,
            TotalSavingsBytes = 1850000,
            SavingsPercentage = 74.0,
            ConversionRatio = 0.26
        };

        Assert.That(typeSummary.MimeType, Is.EqualTo("image/png"));
        Assert.That(typeSummary.Count, Is.EqualTo(25));
        Assert.That(typeSummary.TotalOriginalSize, Is.EqualTo(2500000));
        Assert.That(typeSummary.TotalEstimatedWebPSize, Is.EqualTo(650000));
        Assert.That(typeSummary.TotalSavingsBytes, Is.EqualTo(1850000));
        Assert.That(typeSummary.SavingsPercentage, Is.EqualTo(74.0));
        Assert.That(typeSummary.ConversionRatio, Is.EqualTo(0.26));
    }

    [Test]
    public void TypeSavingsSummary_DefaultValues()
    {
        var typeSummary = new TypeSavingsSummary();

        Assert.That(typeSummary.MimeType, Is.EqualTo(string.Empty));
        Assert.That(typeSummary.Count, Is.EqualTo(0));
        Assert.That(typeSummary.TotalOriginalSize, Is.EqualTo(0));
        Assert.That(typeSummary.TotalEstimatedWebPSize, Is.EqualTo(0));
        Assert.That(typeSummary.TotalSavingsBytes, Is.EqualTo(0));
        Assert.That(typeSummary.SavingsPercentage, Is.EqualTo(0));
        Assert.That(typeSummary.ConversionRatio, Is.EqualTo(0));
    }

    #endregion

    #region ImageSavingsEstimate Tests

    [Test]
    public void ImageSavingsEstimate_CanSetAllProperties()
    {
        var estimate = new ImageSavingsEstimate
        {
            Url = "https://example.com/image.jpg",
            OriginalMimeType = "image/jpeg",
            OriginalSize = 100000,
            EstimatedWebPSize = 75000,
            SavingsBytes = 25000,
            SavingsPercentage = 25.0,
            ConversionRatio = 0.75
        };

        Assert.That(estimate.Url, Is.EqualTo("https://example.com/image.jpg"));
        Assert.That(estimate.OriginalMimeType, Is.EqualTo("image/jpeg"));
        Assert.That(estimate.OriginalSize, Is.EqualTo(100000));
        Assert.That(estimate.EstimatedWebPSize, Is.EqualTo(75000));
        Assert.That(estimate.SavingsBytes, Is.EqualTo(25000));
        Assert.That(estimate.SavingsPercentage, Is.EqualTo(25.0));
        Assert.That(estimate.ConversionRatio, Is.EqualTo(0.75));
    }

    [Test]
    public void ImageSavingsEstimate_DefaultValues()
    {
        var estimate = new ImageSavingsEstimate();

        Assert.That(estimate.Url, Is.EqualTo(string.Empty));
        Assert.That(estimate.OriginalMimeType, Is.EqualTo(string.Empty));
        Assert.That(estimate.OriginalSize, Is.EqualTo(0));
        Assert.That(estimate.EstimatedWebPSize, Is.EqualTo(0));
        Assert.That(estimate.SavingsBytes, Is.EqualTo(0));
        Assert.That(estimate.SavingsPercentage, Is.EqualTo(0));
        Assert.That(estimate.ConversionRatio, Is.EqualTo(0));
    }

    #endregion

    #region EmailOptions Tests

    [Test]
    public void EmailOptions_HasCorrectDefaults()
    {
        var options = new EmailOptions();

        Assert.That(options.ApiKey, Is.Null);
        Assert.That(options.FromEmail, Is.EqualTo("noreply@example.com"));
        Assert.That(options.FromName, Is.EqualTo("WebP Scanner"));
        Assert.That(options.MaxRetries, Is.EqualTo(3));
        Assert.That(options.RetryDelayMinutes, Is.EqualTo(5));
        Assert.That(options.Enabled, Is.True);
        Assert.That(options.MaxAttachmentSizeMb, Is.EqualTo(10));
    }

    [Test]
    public void EmailOptions_SectionNameIsCorrect()
    {
        Assert.That(EmailOptions.SectionName, Is.EqualTo("Email"));
    }

    [Test]
    public void EmailOptions_CanSetAllProperties()
    {
        var options = new EmailOptions
        {
            ApiKey = "SG.test-api-key",
            FromEmail = "scan@mydomain.com",
            FromName = "My Scanner",
            MaxRetries = 5,
            RetryDelayMinutes = 10,
            Enabled = false,
            MaxAttachmentSizeMb = 20
        };

        Assert.That(options.ApiKey, Is.EqualTo("SG.test-api-key"));
        Assert.That(options.FromEmail, Is.EqualTo("scan@mydomain.com"));
        Assert.That(options.FromName, Is.EqualTo("My Scanner"));
        Assert.That(options.MaxRetries, Is.EqualTo(5));
        Assert.That(options.RetryDelayMinutes, Is.EqualTo(10));
        Assert.That(options.Enabled, Is.False);
        Assert.That(options.MaxAttachmentSizeMb, Is.EqualTo(20));
    }

    #endregion

    #region SecurityOptions Tests

    [Test]
    public void SecurityOptions_HasCorrectDefaults()
    {
        var options = new SecurityOptions();

        Assert.That(options.MaxRequestsPerMinute, Is.EqualTo(100));
        Assert.That(options.EnforceHttps, Is.True);
        Assert.That(options.MaxScanDurationMinutes, Is.EqualTo(10));
        Assert.That(options.MaxMemoryPerScanMb, Is.EqualTo(512));
        Assert.That(options.RateLimitExemptIps, Is.Not.Null);
        Assert.That(options.RateLimitExemptIps, Is.Empty);
        Assert.That(options.EnableRequestSizeLimit, Is.True);
        Assert.That(options.MaxRequestBodySizeBytes, Is.EqualTo(1024 * 100)); // 100KB
    }

    [Test]
    public void SecurityOptions_SectionNameIsCorrect()
    {
        Assert.That(SecurityOptions.SectionName, Is.EqualTo("Security"));
    }

    [Test]
    public void SecurityOptions_CanSetAllProperties()
    {
        var options = new SecurityOptions
        {
            MaxRequestsPerMinute = 60,
            EnforceHttps = true,
            MaxScanDurationMinutes = 60,
            MaxMemoryPerScanMb = 1024,
            RateLimitExemptIps = ["10.0.0.0/8", "192.168.1.1"],
            EnableRequestSizeLimit = false,
            MaxRequestBodySizeBytes = 2097152
        };

        Assert.That(options.MaxRequestsPerMinute, Is.EqualTo(60));
        Assert.That(options.EnforceHttps, Is.True);
        Assert.That(options.MaxScanDurationMinutes, Is.EqualTo(60));
        Assert.That(options.MaxMemoryPerScanMb, Is.EqualTo(1024));
        Assert.That(options.RateLimitExemptIps.Length, Is.EqualTo(2));
        Assert.That(options.EnableRequestSizeLimit, Is.False);
        Assert.That(options.MaxRequestBodySizeBytes, Is.EqualTo(2097152));
    }

    #endregion

    #region CrawlerOptions Extended Tests

    [Test]
    public void CrawlerOptions_CanSetAllProperties()
    {
        var options = new CrawlerOptions
        {
            MaxPagesPerScan = 200,
            PageTimeoutSeconds = 60,
            NetworkIdleTimeoutMs = 5000,
            DelayBetweenPagesMs = 1000,
            MaxRetries = 5,
            RespectRobotsTxt = false,
            ChromiumPath = "/usr/bin/chromium",
            UserAgent = "CustomBot/2.0",
            EnableSandbox = true,
            RestrictToTargetDomain = false,
            BlockTrackingDomains = false,
            MaxRequestsPerPage = 500,
            MaxRequestSizeBytes = 104857600,
            AllowedExternalDomains = ["cdn.example.com", "api.example.com"]
        };

        Assert.That(options.MaxPagesPerScan, Is.EqualTo(200));
        Assert.That(options.PageTimeoutSeconds, Is.EqualTo(60));
        Assert.That(options.NetworkIdleTimeoutMs, Is.EqualTo(5000));
        Assert.That(options.DelayBetweenPagesMs, Is.EqualTo(1000));
        Assert.That(options.MaxRetries, Is.EqualTo(5));
        Assert.That(options.RespectRobotsTxt, Is.False);
        Assert.That(options.ChromiumPath, Is.EqualTo("/usr/bin/chromium"));
        Assert.That(options.UserAgent, Is.EqualTo("CustomBot/2.0"));
        Assert.That(options.EnableSandbox, Is.True);
        Assert.That(options.RestrictToTargetDomain, Is.False);
        Assert.That(options.BlockTrackingDomains, Is.False);
        Assert.That(options.MaxRequestsPerPage, Is.EqualTo(500));
        Assert.That(options.MaxRequestSizeBytes, Is.EqualTo(104857600));
        Assert.That(options.AllowedExternalDomains.Length, Is.EqualTo(2));
    }

    #endregion
}
