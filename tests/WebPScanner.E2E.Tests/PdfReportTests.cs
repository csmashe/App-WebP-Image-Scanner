using Microsoft.Extensions.DependencyInjection;
using WebPScanner.Core.Interfaces;
using WebPScanner.Core.Models;

namespace WebPScanner.E2E.Tests;

/// <summary>
/// E2E tests for PDF report generation output.
/// These tests require WebApplicationFactory and are skipped when running against external server.
/// </summary>
[TestFixture]
public class PdfReportTests
{
    private WebApplicationFixture? _appFixture;
    private IPdfReportService? _pdfService;

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
        _pdfService = scope.ServiceProvider.GetRequiredService<IPdfReportService>();
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
    public void PdfReport_ShouldGenerateValidPdf()
    {
        if (IsExternalServer)
        {
            Assert.Ignore("Test requires WebApplicationFactory - skipped for external server");
            return;
        }

        // Arrange
        var reportData = CreateTestReportData();

        // Act
        var pdfBytes = _pdfService!.GenerateReport(reportData);

        // Assert
        Assert.That(pdfBytes, Is.Not.Null);
        Assert.That(pdfBytes.Length > 0, Is.True, "PDF should have content");

        // Check PDF magic number (%PDF-)
        Assert.That(pdfBytes[0], Is.EqualTo((byte)'%'));
        Assert.That(pdfBytes[1], Is.EqualTo((byte)'P'));
        Assert.That(pdfBytes[2], Is.EqualTo((byte)'D'));
        Assert.That(pdfBytes[3], Is.EqualTo((byte)'F'));
    }

    [Test]
    public void PdfReport_ShouldGenerateReportUnder5MB()
    {
        if (IsExternalServer)
        {
            Assert.Ignore("Test requires WebApplicationFactory - skipped for external server");
            return;
        }

        // Arrange - Create a report with many images
        var reportData = CreateLargeTestReportData(500);

        // Act
        var pdfBytes = _pdfService!.GenerateReport(reportData);

        // Assert
        Assert.That(pdfBytes, Is.Not.Null);
        var sizeInMb = pdfBytes.Length / (1024.0 * 1024.0);
        Assert.That(sizeInMb < 5, Is.True, $"PDF should be under 5MB, was {sizeInMb:F2}MB");
    }

    [Test]
    public void PdfReport_ShouldHandleEmptyImageList()
    {
        if (IsExternalServer)
        {
            Assert.Ignore("Test requires WebApplicationFactory - skipped for external server");
            return;
        }

        // Arrange
        var reportData = CreateTestReportData(0);

        // Act
        var pdfBytes = _pdfService!.GenerateReport(reportData);

        // Assert
        Assert.That(pdfBytes, Is.Not.Null);
        Assert.That(pdfBytes.Length > 0, Is.True, "PDF should generate even with no images");
    }

    [Test]
    public void PdfReport_ShouldHandleSpecialCharactersInUrl()
    {
        if (IsExternalServer)
        {
            Assert.Ignore("Test requires WebApplicationFactory - skipped for external server");
            return;
        }

        // Arrange
        var reportData = CreateTestReportData();
        reportData.TargetUrl = "https://example.com/path?query=value&special=<script>";

        // Act
        var pdfBytes = _pdfService!.GenerateReport(reportData);

        // Assert
        Assert.That(pdfBytes, Is.Not.Null);
        Assert.That(pdfBytes.Length > 0, Is.True);
    }

    [Test]
    public void PdfReport_ShouldWriteToStream()
    {
        if (IsExternalServer)
        {
            Assert.Ignore("Test requires WebApplicationFactory - skipped for external server");
            return;
        }

        // Arrange
        var reportData = CreateTestReportData();
        using var stream = new MemoryStream();

        // Act
        _pdfService!.GenerateReportToStream(reportData, stream);

        // Assert
        Assert.That(stream.Length > 0, Is.True, "Stream should have content");
    }

    [Test]
    public void PdfReport_ShouldIncludeAllImageTypes()
    {
        if (IsExternalServer)
        {
            Assert.Ignore("Test requires WebApplicationFactory - skipped for external server");
            return;
        }

        // Arrange
        var reportData = CreateReportWithAllImageTypes();

        // Act
        var pdfBytes = _pdfService!.GenerateReport(reportData);

        // Assert
        Assert.That(pdfBytes, Is.Not.Null);
        Assert.That(pdfBytes.Length > 0, Is.True);
    }

    private static PdfReportData CreateTestReportData(int imageCount = 10)
    {
        var images = Enumerable.Range(1, imageCount).Select(i => new ImageSavingsEstimate
        {
            Url = $"https://example.com/image{i}.png",
            OriginalMimeType = "image/png",
            OriginalSize = 100000 + i * 1000,
            EstimatedWebPSize = 26000 + i * 260,
            SavingsPercentage = 74,
            SavingsBytes = 74000 + i * 740,
            ConversionRatio = 0.26
        }).ToList();

        var summary = new SavingsSummary
        {
            TotalImages = imageCount,
            ConvertibleImages = imageCount,
            TotalOriginalSize = images.Sum(i => i.OriginalSize),
            TotalEstimatedWebPSize = images.Sum(i => i.EstimatedWebPSize),
            TotalSavingsPercentage = imageCount > 0 ? 74 : 0,
            TotalSavingsBytes = images.Sum(i => i.SavingsBytes)
        };

        return new PdfReportData
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://example.com",
            ScanDate = DateTime.UtcNow,
            PagesScanned = 10,
            PagesDiscovered = 15,
            CrawlDuration = TimeSpan.FromSeconds(30),
            ReachedPageLimit = false,
            SavingsSummary = summary,
            ImageEstimates = images
        };
    }

    private static PdfReportData CreateLargeTestReportData(int imageCount)
    {
        return CreateTestReportData(imageCount);
    }

    private static PdfReportData CreateReportWithAllImageTypes()
    {
        var images = new List<ImageSavingsEstimate>
        {
            new() { Url = "https://example.com/photo.jpeg", OriginalMimeType = "image/jpeg", OriginalSize = 200000, EstimatedWebPSize = 150000, SavingsPercentage = 25, SavingsBytes = 50000, ConversionRatio = 0.75 },
            new() { Url = "https://example.com/logo.png", OriginalMimeType = "image/png", OriginalSize = 100000, EstimatedWebPSize = 26000, SavingsPercentage = 74, SavingsBytes = 74000, ConversionRatio = 0.26 },
            new() { Url = "https://example.com/anim.gif", OriginalMimeType = "image/gif", OriginalSize = 500000, EstimatedWebPSize = 250000, SavingsPercentage = 50, SavingsBytes = 250000, ConversionRatio = 0.50 },
            new() { Url = "https://example.com/legacy.bmp", OriginalMimeType = "image/bmp", OriginalSize = 1000000, EstimatedWebPSize = 100000, SavingsPercentage = 90, SavingsBytes = 900000, ConversionRatio = 0.10 },
            new() { Url = "https://example.com/scan.tiff", OriginalMimeType = "image/tiff", OriginalSize = 800000, EstimatedWebPSize = 120000, SavingsPercentage = 85, SavingsBytes = 680000, ConversionRatio = 0.15 }
        };

        return new PdfReportData
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://example.com",
            ScanDate = DateTime.UtcNow,
            PagesScanned = 5,
            PagesDiscovered = 5,
            CrawlDuration = TimeSpan.FromSeconds(15),
            ReachedPageLimit = false,
            SavingsSummary = new SavingsSummary
            {
                TotalImages = 5,
                ConvertibleImages = 5,
                TotalOriginalSize = images.Sum(i => i.OriginalSize),
                TotalEstimatedWebPSize = images.Sum(i => i.EstimatedWebPSize),
                TotalSavingsPercentage = 65,
                TotalSavingsBytes = images.Sum(i => i.SavingsBytes)
            },
            ImageEstimates = images
        };
    }
}
