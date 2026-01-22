using WebPScanner.Core.Entities;
using WebPScanner.Core.Services;

namespace WebPScanner.Core.Tests;

public class SavingsEstimatorServiceTests
{
    private SavingsEstimatorService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new SavingsEstimatorService();
    }

    #region CalculateSavingsSummary (DiscoveredImage) Tests

    [Test]
    public void CalculateSavingsSummary_EmptyList_ReturnsEmptySummary()
    {
        var images = Array.Empty<DiscoveredImage>();
        var summary = _service.CalculateSavingsSummary(images);

        Assert.That(summary.TotalImages, Is.EqualTo(0));
        Assert.That(summary.ConvertibleImages, Is.EqualTo(0));
        Assert.That(summary.TotalOriginalSize, Is.EqualTo(0));
        Assert.That(summary.TotalEstimatedWebPSize, Is.EqualTo(0));
        Assert.That(summary.TotalSavingsBytes, Is.EqualTo(0));
        Assert.That(summary.TotalSavingsPercentage, Is.EqualTo(0));
        Assert.That(summary.ByType, Is.Empty);
    }

    [Test]
    public void CalculateSavingsSummary_SingleImage_CalculatesCorrectly()
    {
        var images = new List<DiscoveredImage>
        {
            CreateDiscoveredImage("test.png", "image/png", 100000, 26000)
        };

        var summary = _service.CalculateSavingsSummary(images);

        Assert.That(summary.TotalImages, Is.EqualTo(1));
        Assert.That(summary.ConvertibleImages, Is.EqualTo(1));
        Assert.That(summary.TotalOriginalSize, Is.EqualTo(100000));
        Assert.That(summary.TotalEstimatedWebPSize, Is.EqualTo(26000));
        Assert.That(summary.TotalSavingsBytes, Is.EqualTo(74000));
        Assert.That(summary.TotalSavingsPercentage, Is.EqualTo(74));
    }

    [Test]
    public void CalculateSavingsSummary_MixedImages_CalculatesCorrectly()
    {
        var images = new List<DiscoveredImage>
        {
            CreateDiscoveredImage("test.png", "image/png", 100000, 26000),
            CreateDiscoveredImage("photo.jpg", "image/jpeg", 200000, 150000),
            CreateDiscoveredImage("animation.gif", "image/gif", 50000, 25000)
        };

        var summary = _service.CalculateSavingsSummary(images);

        Assert.That(summary.TotalImages, Is.EqualTo(3));
        Assert.That(summary.ConvertibleImages, Is.EqualTo(3));
        Assert.That(summary.TotalOriginalSize, Is.EqualTo(350000));
        Assert.That(summary.TotalEstimatedWebPSize, Is.EqualTo(201000));
        Assert.That(summary.TotalSavingsBytes, Is.EqualTo(149000));
        Assert.That(summary.TotalSavingsPercentage, Is.EqualTo(42.57));
    }

    [Test]
    public void CalculateSavingsSummary_GroupsByType()
    {
        var images = new List<DiscoveredImage>
        {
            CreateDiscoveredImage("test1.png", "image/png", 100000, 26000),
            CreateDiscoveredImage("test2.png", "image/png", 50000, 13000),
            CreateDiscoveredImage("photo.jpg", "image/jpeg", 200000, 150000)
        };

        var summary = _service.CalculateSavingsSummary(images);

        Assert.That(summary.ByType.Count, Is.EqualTo(2));
        Assert.That(summary.ByType.ContainsKey("image/png"), Is.True);
        Assert.That(summary.ByType.ContainsKey("image/jpeg"), Is.True);

        var pngSummary = summary.ByType["image/png"];
        Assert.That(pngSummary.Count, Is.EqualTo(2));
        Assert.That(pngSummary.TotalOriginalSize, Is.EqualTo(150000));
        Assert.That(pngSummary.TotalEstimatedWebPSize, Is.EqualTo(39000));
        Assert.That(pngSummary.TotalSavingsBytes, Is.EqualTo(111000));

        var jpegSummary = summary.ByType["image/jpeg"];
        Assert.That(jpegSummary.Count, Is.EqualTo(1));
        Assert.That(jpegSummary.TotalOriginalSize, Is.EqualTo(200000));
        Assert.That(jpegSummary.TotalEstimatedWebPSize, Is.EqualTo(150000));
    }

    [Test]
    public void CalculateSavingsSummary_SkipsZeroSizeImages()
    {
        var images = new List<DiscoveredImage>
        {
            CreateDiscoveredImage("test.png", "image/png", 100000, 26000),
            CreateDiscoveredImage("empty.png", "image/png", 0, 0),
            CreateDiscoveredImage("negative.png", "image/png", -100, 0)
        };

        var summary = _service.CalculateSavingsSummary(images);

        Assert.That(summary.TotalImages, Is.EqualTo(3));
        Assert.That(summary.ConvertibleImages, Is.EqualTo(1));
        Assert.That(summary.TotalOriginalSize, Is.EqualTo(100000));
    }

    [Test]
    public void CalculateSavingsSummary_IncludesDisclaimer()
    {
        var images = new List<DiscoveredImage>
        {
            CreateDiscoveredImage("test.png", "image/png", 100000, 26000)
        };

        var summary = _service.CalculateSavingsSummary(images);

        Assert.That(summary.Disclaimer, Is.Not.Empty);
        Assert.That(summary.Disclaimer.ToLower(), Does.Contain("approximate"));
    }

    #endregion

    #region CalculateImageSavings (DiscoveredImage) Tests

    [Test]
    public void CalculateImageSavings_ReturnsCompleteEstimates()
    {
        var images = new List<DiscoveredImage>
        {
            CreateDiscoveredImage("https://example.com/image.png", "image/png", 100000, 26000)
        };

        var estimates = _service.CalculateImageSavings(images);

        Assert.That(estimates, Has.Count.EqualTo(1));
        var estimate = estimates[0];
        Assert.That(estimate.Url, Is.EqualTo("https://example.com/image.png"));
        Assert.That(estimate.OriginalMimeType, Is.EqualTo("image/png"));
        Assert.That(estimate.OriginalSize, Is.EqualTo(100000));
        Assert.That(estimate.EstimatedWebPSize, Is.EqualTo(26000));
        Assert.That(estimate.SavingsBytes, Is.EqualTo(74000));
        Assert.That(estimate.SavingsPercentage, Is.EqualTo(74));
    }

    [Test]
    public void CalculateImageSavings_MultipleImages_ReturnsAllEstimates()
    {
        var images = new List<DiscoveredImage>
        {
            CreateDiscoveredImage("test1.png", "image/png", 100000, 26000),
            CreateDiscoveredImage("test2.jpg", "image/jpeg", 200000, 150000),
            CreateDiscoveredImage("test3.gif", "image/gif", 50000, 25000)
        };

        var estimates = _service.CalculateImageSavings(images);

        Assert.That(estimates, Has.Count.EqualTo(3));
        Assert.That(estimates[0].SavingsPercentage, Is.EqualTo(74));
        Assert.That(estimates[1].SavingsPercentage, Is.EqualTo(25));
        Assert.That(estimates[2].SavingsPercentage, Is.EqualTo(50));
    }

    [Test]
    public void CalculateImageSavings_ZeroSizeImage_ReturnsZeroSavings()
    {
        var images = new List<DiscoveredImage>
        {
            CreateDiscoveredImage("empty.png", "image/png", 0, 0)
        };

        var estimates = _service.CalculateImageSavings(images);

        Assert.That(estimates, Has.Count.EqualTo(1));
        Assert.That(estimates[0].SavingsBytes, Is.EqualTo(0));
        Assert.That(estimates[0].SavingsPercentage, Is.EqualTo(0));
    }

    #endregion

    #region Helper Methods

    private static DiscoveredImage CreateDiscoveredImage(string url, string mimeType, long fileSize, long estimatedWebPSize)
    {
        return new DiscoveredImage
        {
            Id = Guid.NewGuid(),
            ScanJobId = Guid.NewGuid(),
            ImageUrl = url,
            MimeType = mimeType,
            FileSize = fileSize,
            EstimatedWebPSize = estimatedWebPSize,
            DiscoveredAt = DateTime.UtcNow
        };
    }

    #endregion
}
