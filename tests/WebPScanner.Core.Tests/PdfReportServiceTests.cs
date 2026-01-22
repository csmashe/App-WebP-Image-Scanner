using WebPScanner.Core.Models;
using WebPScanner.Core.Services;

namespace WebPScanner.Core.Tests;

public class PdfReportServiceTests
{
    private PdfReportService _pdfService = null!;

    [SetUp]
    public void SetUp()
    {
        _pdfService = new PdfReportService();
    }

    #region GenerateReport Tests

    [Test]
    public void GenerateReport_WithValidData_ReturnsPdfBytes()
    {
        // Arrange
        var reportData = CreateSampleReportData();

        // Act
        var pdfBytes = _pdfService.GenerateReport(reportData);

        // Assert
        Assert.That(pdfBytes, Is.Not.Null);
        Assert.That(pdfBytes.Length > 0, Is.True);
        // PDF magic bytes: %PDF
        Assert.That(pdfBytes[0], Is.EqualTo((byte)'%'));
        Assert.That(pdfBytes[1], Is.EqualTo((byte)'P'));
        Assert.That(pdfBytes[2], Is.EqualTo((byte)'D'));
        Assert.That(pdfBytes[3], Is.EqualTo((byte)'F'));
    }

    [Test]
    public void GenerateReport_WithEmptyImageEstimates_Succeeds()
    {
        // Arrange
        var reportData = new PdfReportData
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://example.com",
            ScanDate = DateTime.UtcNow,
            PagesScanned = 10,
            PagesDiscovered = 15,
            CrawlDuration = TimeSpan.FromMinutes(2),
            SavingsSummary = new SavingsSummary
            {
                TotalImages = 0,
                ConvertibleImages = 0,
                TotalOriginalSize = 0,
                TotalEstimatedWebPSize = 0,
                TotalSavingsBytes = 0,
                TotalSavingsPercentage = 0
            },
            ImageEstimates = []
        };

        // Act
        var pdfBytes = _pdfService.GenerateReport(reportData);

        // Assert
        Assert.That(pdfBytes, Is.Not.Null);
        Assert.That(pdfBytes.Length > 0, Is.True);
    }

    [Test]
    public void GenerateReport_WithLargeImageList_Succeeds()
    {
        // Arrange
        var reportData = CreateLargeReportData(100);

        // Act
        var pdfBytes = _pdfService.GenerateReport(reportData);

        // Assert
        Assert.That(pdfBytes, Is.Not.Null);
        Assert.That(pdfBytes.Length > 0, Is.True);
    }

    [Test]
    public void GenerateReport_WithReachedPageLimit_IncludesWarning()
    {
        // Arrange
        var reportData = CreateSampleReportData();
        reportData.ReachedPageLimit = true;

        // Act
        var pdfBytes = _pdfService.GenerateReport(reportData);

        // Assert
        Assert.That(pdfBytes, Is.Not.Null);
        Assert.That(pdfBytes.Length > 0, Is.True);
    }

    [Test]
    public void GenerateReport_WithMultipleImageTypes_Succeeds()
    {
        // Arrange
        var reportData = CreateMultiTypeReportData();

        // Act
        var pdfBytes = _pdfService.GenerateReport(reportData);

        // Assert
        Assert.That(pdfBytes, Is.Not.Null);
        Assert.That(pdfBytes.Length > 0, Is.True);
    }

    [Test]
    public void GenerateReport_WithLongUrls_TruncatesAppropriately()
    {
        // Arrange
        var reportData = new PdfReportData
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://www.example.com/very/long/path/that/goes/on/and/on/and/on/forever/and/ever/page.html",
            ScanDate = DateTime.UtcNow,
            PagesScanned = 5,
            PagesDiscovered = 5,
            CrawlDuration = TimeSpan.FromMinutes(1),
            SavingsSummary = new SavingsSummary
            {
                TotalImages = 1,
                ConvertibleImages = 1,
                TotalOriginalSize = 100000,
                TotalEstimatedWebPSize = 26000,
                TotalSavingsBytes = 74000,
                TotalSavingsPercentage = 74
            },
            ImageEstimates =
            [
                new ImageSavingsEstimate
                {
                    Url = "https://www.example.com/images/very/long/path/to/image/that/should/be/truncated/image.png",
                    OriginalMimeType = "image/png",
                    OriginalSize = 100000,
                    EstimatedWebPSize = 26000,
                    SavingsBytes = 74000,
                    SavingsPercentage = 74
                }
            ]
        };

        // Act
        var pdfBytes = _pdfService.GenerateReport(reportData);

        // Assert
        Assert.That(pdfBytes, Is.Not.Null);
        Assert.That(pdfBytes.Length > 0, Is.True);
    }

    #endregion

    #region GenerateReportToStream Tests

    [Test]
    public void GenerateReportToStream_WritesToStream()
    {
        // Arrange
        var reportData = CreateSampleReportData();
        using var stream = new MemoryStream();

        // Act
        _pdfService.GenerateReportToStream(reportData, stream);

        // Assert
        Assert.That(stream.Length > 0, Is.True);

        // Verify PDF magic bytes
        stream.Position = 0;
        var header = new byte[4];
        stream.ReadExactly(header, 0, 4);
        Assert.That(header[0], Is.EqualTo((byte)'%'));
        Assert.That(header[1], Is.EqualTo((byte)'P'));
        Assert.That(header[2], Is.EqualTo((byte)'D'));
        Assert.That(header[3], Is.EqualTo((byte)'F'));
    }

    #endregion

    #region Report Size Tests

    [Test]
    public void GenerateReport_TypicalScan_StaysUnder5MB()
    {
        // Arrange
        // Typical scan: 50 pages, 200 images
        var reportData = CreateLargeReportData(200);

        // Act
        var pdfBytes = _pdfService.GenerateReport(reportData);

        // Assert
        var sizeMb = pdfBytes.Length / (1024.0 * 1024.0);
        Assert.That(sizeMb < 5, Is.True, $"PDF size {sizeMb:F2} MB exceeds 5MB limit");
    }

    [Test]
    public void GenerateReport_LargeScan_StaysUnder5MB()
    {
        // Arrange
        // Large scan: 500 images
        var reportData = CreateLargeReportData(500);

        // Act
        var pdfBytes = _pdfService.GenerateReport(reportData);

        // Assert
        var sizeMb = pdfBytes.Length / (1024.0 * 1024.0);
        Assert.That(sizeMb < 5, Is.True, $"PDF size {sizeMb:F2} MB exceeds 5MB limit");
    }

    #endregion

    #region Edge Cases

    [Test]
    public void GenerateReport_WithZeroSavings_Succeeds()
    {
        // Arrange
        var reportData = new PdfReportData
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://example.com",
            ScanDate = DateTime.UtcNow,
            PagesScanned = 10,
            PagesDiscovered = 10,
            CrawlDuration = TimeSpan.FromSeconds(30),
            SavingsSummary = new SavingsSummary
            {
                TotalImages = 5,
                ConvertibleImages = 0,
                TotalOriginalSize = 0,
                TotalEstimatedWebPSize = 0,
                TotalSavingsBytes = 0,
                TotalSavingsPercentage = 0
            },
            ImageEstimates = []
        };

        // Act
        var pdfBytes = _pdfService.GenerateReport(reportData);

        // Assert
        Assert.That(pdfBytes, Is.Not.Null);
        Assert.That(pdfBytes.Length > 0, Is.True);
    }

    [Test]
    public void GenerateReport_WithSpecialCharactersInUrl_Succeeds()
    {
        // Arrange
        var reportData = new PdfReportData
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://example.com/path?query=value&special=%20chars",
            ScanDate = DateTime.UtcNow,
            PagesScanned = 1,
            PagesDiscovered = 1,
            CrawlDuration = TimeSpan.FromSeconds(5),
            SavingsSummary = new SavingsSummary
            {
                TotalImages = 1,
                ConvertibleImages = 1,
                TotalOriginalSize = 50000,
                TotalEstimatedWebPSize = 13000,
                TotalSavingsBytes = 37000,
                TotalSavingsPercentage = 74
            },
            ImageEstimates =
            [
                new ImageSavingsEstimate
                {
                    Url = "https://example.com/image.png?version=2&size=large",
                    OriginalMimeType = "image/png",
                    OriginalSize = 50000,
                    EstimatedWebPSize = 13000,
                    SavingsBytes = 37000,
                    SavingsPercentage = 74
                }
            ]
        };

        // Act
        var pdfBytes = _pdfService.GenerateReport(reportData);

        // Assert
        Assert.That(pdfBytes, Is.Not.Null);
        Assert.That(pdfBytes.Length > 0, Is.True);
    }

    [Test]
    public void GenerateReport_WithVeryLargeSavings_DisplaysCorrectly()
    {
        // Arrange - 1GB worth of images
        var reportData = new PdfReportData
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://example.com",
            ScanDate = DateTime.UtcNow,
            PagesScanned = 100,
            PagesDiscovered = 100,
            CrawlDuration = TimeSpan.FromMinutes(30),
            SavingsSummary = new SavingsSummary
            {
                TotalImages = 1000,
                ConvertibleImages = 1000,
                TotalOriginalSize = 1073741824, // 1 GB
                TotalEstimatedWebPSize = 279172875, // ~26%
                TotalSavingsBytes = 794568949, // ~74%
                TotalSavingsPercentage = 74
            },
            ImageEstimates = []
        };

        // Act
        var pdfBytes = _pdfService.GenerateReport(reportData);

        // Assert
        Assert.That(pdfBytes, Is.Not.Null);
        Assert.That(pdfBytes.Length > 0, Is.True);
    }

    [Test]
    public void GenerateReport_WithAllImageTypes_IncludesTypeBreakdown()
    {
        // Arrange
        var reportData = CreateMultiTypeReportData();

        // Act
        var pdfBytes = _pdfService.GenerateReport(reportData);

        // Assert
        Assert.That(pdfBytes, Is.Not.Null);
        Assert.That(pdfBytes.Length > 0, Is.True);
        Assert.That(reportData.SavingsSummary.ByType.Count == 5, Is.True); // JPEG, PNG, GIF, BMP, TIFF
    }

    [Test]
    public void GenerateReport_WithSmallByteValues_DisplaysCorrectUnits()
    {
        // Arrange - very small images (bytes range)
        var reportData = new PdfReportData
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://example.com",
            ScanDate = DateTime.UtcNow,
            PagesScanned = 1,
            PagesDiscovered = 1,
            CrawlDuration = TimeSpan.FromSeconds(1),
            SavingsSummary = new SavingsSummary
            {
                TotalImages = 1,
                ConvertibleImages = 1,
                TotalOriginalSize = 500,
                TotalEstimatedWebPSize = 130,
                TotalSavingsBytes = 370,
                TotalSavingsPercentage = 74
            },
            ImageEstimates =
            [
                new ImageSavingsEstimate
                {
                    Url = "https://example.com/tiny.png",
                    OriginalMimeType = "image/png",
                    OriginalSize = 500,
                    EstimatedWebPSize = 130,
                    SavingsBytes = 370,
                    SavingsPercentage = 74
                }
            ]
        };

        // Act
        var pdfBytes = _pdfService.GenerateReport(reportData);

        // Assert
        Assert.That(pdfBytes, Is.Not.Null);
        Assert.That(pdfBytes.Length > 0, Is.True);
    }

    [Test]
    public void GenerateReport_WithKilobyteValues_DisplaysCorrectUnits()
    {
        // Arrange - KB range images
        var reportData = new PdfReportData
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://example.com",
            ScanDate = DateTime.UtcNow,
            PagesScanned = 1,
            PagesDiscovered = 1,
            CrawlDuration = TimeSpan.FromSeconds(5),
            SavingsSummary = new SavingsSummary
            {
                TotalImages = 1,
                ConvertibleImages = 1,
                TotalOriginalSize = 50 * 1024, // 50 KB
                TotalEstimatedWebPSize = 13 * 1024, // 13 KB
                TotalSavingsBytes = 37 * 1024, // 37 KB
                TotalSavingsPercentage = 74
            },
            ImageEstimates =
            [
                new ImageSavingsEstimate
                {
                    Url = "https://example.com/medium.png",
                    OriginalMimeType = "image/png",
                    OriginalSize = 50 * 1024,
                    EstimatedWebPSize = 13 * 1024,
                    SavingsBytes = 37 * 1024,
                    SavingsPercentage = 74
                }
            ]
        };

        // Act
        var pdfBytes = _pdfService.GenerateReport(reportData);

        // Assert
        Assert.That(pdfBytes, Is.Not.Null);
        Assert.That(pdfBytes.Length > 0, Is.True);
    }

    [Test]
    public void GenerateReport_WithMegabyteValues_DisplaysCorrectUnits()
    {
        // Arrange - MB range images
        var reportData = new PdfReportData
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://example.com",
            ScanDate = DateTime.UtcNow,
            PagesScanned = 1,
            PagesDiscovered = 1,
            CrawlDuration = TimeSpan.FromMinutes(1),
            SavingsSummary = new SavingsSummary
            {
                TotalImages = 1,
                ConvertibleImages = 1,
                TotalOriginalSize = 5 * 1024 * 1024, // 5 MB
                TotalEstimatedWebPSize = (long)(1.3 * 1024 * 1024), // ~1.3 MB
                TotalSavingsBytes = (long)(3.7 * 1024 * 1024), // ~3.7 MB
                TotalSavingsPercentage = 74
            },
            ImageEstimates =
            [
                new ImageSavingsEstimate
                {
                    Url = "https://example.com/large.png",
                    OriginalMimeType = "image/png",
                    OriginalSize = 5 * 1024 * 1024,
                    EstimatedWebPSize = (long)(1.3 * 1024 * 1024),
                    SavingsBytes = (long)(3.7 * 1024 * 1024),
                    SavingsPercentage = 74
                }
            ]
        };

        // Act
        var pdfBytes = _pdfService.GenerateReport(reportData);

        // Assert
        Assert.That(pdfBytes, Is.Not.Null);
        Assert.That(pdfBytes.Length > 0, Is.True);
    }

    [Test]
    public void GenerateReport_WithEmptyByType_Succeeds()
    {
        // Arrange
        var reportData = new PdfReportData
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://example.com",
            ScanDate = DateTime.UtcNow,
            PagesScanned = 5,
            PagesDiscovered = 5,
            CrawlDuration = TimeSpan.FromSeconds(30),
            SavingsSummary = new SavingsSummary
            {
                TotalImages = 0,
                ConvertibleImages = 0,
                TotalOriginalSize = 0,
                TotalEstimatedWebPSize = 0,
                TotalSavingsBytes = 0,
                TotalSavingsPercentage = 0,
                ByType = new Dictionary<string, TypeSavingsSummary>() // Empty
            },
            ImageEstimates = []
        };

        // Act
        var pdfBytes = _pdfService.GenerateReport(reportData);

        // Assert
        Assert.That(pdfBytes, Is.Not.Null);
        Assert.That(pdfBytes.Length > 0, Is.True);
    }

    #endregion

    #region Helper Methods

    private static PdfReportData CreateSampleReportData()
    {
        return new PdfReportData
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://www.example.com",
            ScanDate = DateTime.UtcNow,
            PagesScanned = 25,
            PagesDiscovered = 30,
            CrawlDuration = TimeSpan.FromMinutes(5),
            ReachedPageLimit = false,
            SavingsSummary = new SavingsSummary
            {
                TotalImages = 50,
                ConvertibleImages = 35,
                TotalOriginalSize = 5242880, // 5 MB
                TotalEstimatedWebPSize = 1572864, // ~1.5 MB
                TotalSavingsBytes = 3670016, // ~3.5 MB
                TotalSavingsPercentage = 70,
                ByType = new Dictionary<string, TypeSavingsSummary>
                {
                    ["image/jpeg"] = new()
                    {
                        MimeType = "image/jpeg",
                        Count = 20,
                        TotalOriginalSize = 2097152,
                        TotalEstimatedWebPSize = 1572864,
                        TotalSavingsBytes = 524288,
                        SavingsPercentage = 25,
                        ConversionRatio = 0.75
                    },
                    ["image/png"] = new()
                    {
                        MimeType = "image/png",
                        Count = 15,
                        TotalOriginalSize = 3145728,
                        TotalEstimatedWebPSize = 817889,
                        TotalSavingsBytes = 2327839,
                        SavingsPercentage = 74,
                        ConversionRatio = 0.26
                    }
                }
            },
            ImageEstimates =
            [
                new ImageSavingsEstimate
                {
                    Url = "https://www.example.com/images/hero.png",
                    OriginalMimeType = "image/png",
                    OriginalSize = 524288,
                    EstimatedWebPSize = 136315,
                    SavingsBytes = 387973,
                    SavingsPercentage = 74,
                    ConversionRatio = 0.26
                },

                new ImageSavingsEstimate
                {
                    Url = "https://www.example.com/images/product.jpg",
                    OriginalMimeType = "image/jpeg",
                    OriginalSize = 262144,
                    EstimatedWebPSize = 196608,
                    SavingsBytes = 65536,
                    SavingsPercentage = 25,
                    ConversionRatio = 0.75
                }
            ]
        };
    }

    private static PdfReportData CreateLargeReportData(int imageCount)
    {
        var images = new List<ImageSavingsEstimate>();
        var random = new Random(42); // Fixed seed for reproducibility

        for (var i = 0; i < imageCount; i++)
        {
            var size = random.Next(10000, 500000);
            var isPng = i % 3 == 0;
            var ratio = isPng ? 0.26 : 0.75;
            var estimatedSize = (long)(size * ratio);

            images.Add(new ImageSavingsEstimate
            {
                Url = $"https://example.com/images/image_{i:D4}.{(isPng ? "png" : "jpg")}",
                OriginalMimeType = isPng ? "image/png" : "image/jpeg",
                OriginalSize = size,
                EstimatedWebPSize = estimatedSize,
                SavingsBytes = size - estimatedSize,
                SavingsPercentage = (1 - ratio) * 100,
                ConversionRatio = ratio
            });
        }

        var totalOriginal = images.Sum(i => i.OriginalSize);
        var totalEstimated = images.Sum(i => i.EstimatedWebPSize);

        return new PdfReportData
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://www.largesite.com",
            ScanDate = DateTime.UtcNow,
            PagesScanned = imageCount / 4,
            PagesDiscovered = imageCount / 3,
            CrawlDuration = TimeSpan.FromMinutes(imageCount / 10),
            ReachedPageLimit = imageCount >= 100,
            SavingsSummary = new SavingsSummary
            {
                TotalImages = imageCount,
                ConvertibleImages = imageCount,
                TotalOriginalSize = totalOriginal,
                TotalEstimatedWebPSize = totalEstimated,
                TotalSavingsBytes = totalOriginal - totalEstimated,
                TotalSavingsPercentage = (double)(totalOriginal - totalEstimated) / totalOriginal * 100,
                ByType = new Dictionary<string, TypeSavingsSummary>
                {
                    ["image/png"] = new()
                    {
                        MimeType = "image/png",
                        Count = images.Count(i => i.OriginalMimeType == "image/png"),
                        TotalOriginalSize = images.Where(i => i.OriginalMimeType == "image/png").Sum(i => i.OriginalSize),
                        TotalEstimatedWebPSize = images.Where(i => i.OriginalMimeType == "image/png").Sum(i => i.EstimatedWebPSize),
                        TotalSavingsBytes = images.Where(i => i.OriginalMimeType == "image/png").Sum(i => i.SavingsBytes),
                        SavingsPercentage = 74,
                        ConversionRatio = 0.26
                    },
                    ["image/jpeg"] = new()
                    {
                        MimeType = "image/jpeg",
                        Count = images.Count(i => i.OriginalMimeType == "image/jpeg"),
                        TotalOriginalSize = images.Where(i => i.OriginalMimeType == "image/jpeg").Sum(i => i.OriginalSize),
                        TotalEstimatedWebPSize = images.Where(i => i.OriginalMimeType == "image/jpeg").Sum(i => i.EstimatedWebPSize),
                        TotalSavingsBytes = images.Where(i => i.OriginalMimeType == "image/jpeg").Sum(i => i.SavingsBytes),
                        SavingsPercentage = 25,
                        ConversionRatio = 0.75
                    }
                }
            },
            ImageEstimates = images
        };
    }

    private static PdfReportData CreateMultiTypeReportData()
    {
        var images = new List<ImageSavingsEstimate>
        {
            new()
            {
                Url = "https://example.com/image.jpg",
                OriginalMimeType = "image/jpeg",
                OriginalSize = 100000,
                EstimatedWebPSize = 75000,
                SavingsBytes = 25000,
                SavingsPercentage = 25,
                ConversionRatio = 0.75
            },
            new()
            {
                Url = "https://example.com/image.png",
                OriginalMimeType = "image/png",
                OriginalSize = 100000,
                EstimatedWebPSize = 26000,
                SavingsBytes = 74000,
                SavingsPercentage = 74,
                ConversionRatio = 0.26
            },
            new()
            {
                Url = "https://example.com/image.gif",
                OriginalMimeType = "image/gif",
                OriginalSize = 100000,
                EstimatedWebPSize = 50000,
                SavingsBytes = 50000,
                SavingsPercentage = 50,
                ConversionRatio = 0.50
            },
            new()
            {
                Url = "https://example.com/image.bmp",
                OriginalMimeType = "image/bmp",
                OriginalSize = 100000,
                EstimatedWebPSize = 10000,
                SavingsBytes = 90000,
                SavingsPercentage = 90,
                ConversionRatio = 0.10
            },
            new()
            {
                Url = "https://example.com/image.tiff",
                OriginalMimeType = "image/tiff",
                OriginalSize = 100000,
                EstimatedWebPSize = 15000,
                SavingsBytes = 85000,
                SavingsPercentage = 85,
                ConversionRatio = 0.15
            }
        };

        return new PdfReportData
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://example.com",
            ScanDate = DateTime.UtcNow,
            PagesScanned = 10,
            PagesDiscovered = 10,
            CrawlDuration = TimeSpan.FromMinutes(2),
            SavingsSummary = new SavingsSummary
            {
                TotalImages = 5,
                ConvertibleImages = 5,
                TotalOriginalSize = 500000,
                TotalEstimatedWebPSize = 176000,
                TotalSavingsBytes = 324000,
                TotalSavingsPercentage = 64.8,
                ByType = new Dictionary<string, TypeSavingsSummary>
                {
                    ["image/jpeg"] = new()
                    {
                        MimeType = "image/jpeg",
                        Count = 1,
                        TotalOriginalSize = 100000,
                        TotalEstimatedWebPSize = 75000,
                        TotalSavingsBytes = 25000,
                        SavingsPercentage = 25,
                        ConversionRatio = 0.75
                    },
                    ["image/png"] = new()
                    {
                        MimeType = "image/png",
                        Count = 1,
                        TotalOriginalSize = 100000,
                        TotalEstimatedWebPSize = 26000,
                        TotalSavingsBytes = 74000,
                        SavingsPercentage = 74,
                        ConversionRatio = 0.26
                    },
                    ["image/gif"] = new()
                    {
                        MimeType = "image/gif",
                        Count = 1,
                        TotalOriginalSize = 100000,
                        TotalEstimatedWebPSize = 50000,
                        TotalSavingsBytes = 50000,
                        SavingsPercentage = 50,
                        ConversionRatio = 0.50
                    },
                    ["image/bmp"] = new()
                    {
                        MimeType = "image/bmp",
                        Count = 1,
                        TotalOriginalSize = 100000,
                        TotalEstimatedWebPSize = 10000,
                        TotalSavingsBytes = 90000,
                        SavingsPercentage = 90,
                        ConversionRatio = 0.10
                    },
                    ["image/tiff"] = new()
                    {
                        MimeType = "image/tiff",
                        Count = 1,
                        TotalOriginalSize = 100000,
                        TotalEstimatedWebPSize = 15000,
                        TotalSavingsBytes = 85000,
                        SavingsPercentage = 85,
                        ConversionRatio = 0.15
                    }
                }
            },
            ImageEstimates = images
        };
    }

    #endregion
}
