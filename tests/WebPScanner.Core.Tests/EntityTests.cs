using WebPScanner.Core.Entities;
using WebPScanner.Core.Enums;

namespace WebPScanner.Core.Tests;

/// <summary>
/// Tests for entity classes and enums.
/// </summary>
public class EntityTests
{
    #region ScanJob Tests

    [Test]
    public void ScanJob_CanSetAllProperties()
    {
        var scanId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var startedAt = createdAt.AddSeconds(10);
        var completedAt = createdAt.AddMinutes(5);

        var scanJob = new ScanJob
        {
            ScanId = scanId,
            TargetUrl = "https://example.com",
            Email = "test@example.com",
            Status = ScanStatus.Completed,
            QueuePosition = 0,
            PriorityScore = 12345678,
            SubmitterIp = "192.0.2.1",
            SubmissionCount = 3,
            CreatedAt = createdAt,
            StartedAt = startedAt,
            CompletedAt = completedAt,
            ErrorMessage = null,
            PagesScanned = 50,
            PagesDiscovered = 60,
            NonWebPImagesFound = 25
        };

        Assert.That(scanJob.ScanId, Is.EqualTo(scanId));
        Assert.That(scanJob.TargetUrl, Is.EqualTo("https://example.com"));
        Assert.That(scanJob.Email, Is.EqualTo("test@example.com"));
        Assert.That(scanJob.Status, Is.EqualTo(ScanStatus.Completed));
        Assert.That(scanJob.QueuePosition, Is.EqualTo(0));
        Assert.That(scanJob.PriorityScore, Is.EqualTo(12345678));
        Assert.That(scanJob.SubmitterIp, Is.EqualTo("192.0.2.1"));
        Assert.That(scanJob.SubmissionCount, Is.EqualTo(3));
        Assert.That(scanJob.CreatedAt, Is.EqualTo(createdAt));
        Assert.That(scanJob.StartedAt, Is.EqualTo(startedAt));
        Assert.That(scanJob.CompletedAt, Is.EqualTo(completedAt));
        Assert.That(scanJob.ErrorMessage, Is.Null);
        Assert.That(scanJob.PagesScanned, Is.EqualTo(50));
        Assert.That(scanJob.PagesDiscovered, Is.EqualTo(60));
        Assert.That(scanJob.NonWebPImagesFound, Is.EqualTo(25));
    }

    [Test]
    public void ScanJob_DefaultValues()
    {
        var scanJob = new ScanJob();

        Assert.That(scanJob.ScanId, Is.EqualTo(Guid.Empty));
        Assert.That(scanJob.TargetUrl, Is.EqualTo(string.Empty));
        Assert.That(scanJob.Email, Is.Null);
        Assert.That(scanJob.Status, Is.EqualTo(ScanStatus.Queued));
        Assert.That(scanJob.QueuePosition, Is.EqualTo(0));
        Assert.That(scanJob.PriorityScore, Is.EqualTo(0));
        Assert.That(scanJob.SubmitterIp, Is.Null);
        Assert.That(scanJob.SubmissionCount, Is.EqualTo(0));
        Assert.That(scanJob.StartedAt, Is.Null);
        Assert.That(scanJob.CompletedAt, Is.Null);
        Assert.That(scanJob.ErrorMessage, Is.Null);
        Assert.That(scanJob.PagesScanned, Is.EqualTo(0));
        Assert.That(scanJob.PagesDiscovered, Is.EqualTo(0));
        Assert.That(scanJob.NonWebPImagesFound, Is.EqualTo(0));
    }

    [Test]
    public void ScanJob_WithErrorMessage()
    {
        var scanJob = new ScanJob
        {
            Status = ScanStatus.Failed,
            ErrorMessage = "Connection timeout"
        };

        Assert.That(scanJob.Status, Is.EqualTo(ScanStatus.Failed));
        Assert.That(scanJob.ErrorMessage, Is.EqualTo("Connection timeout"));
    }

    [Test]
    public void ScanJob_DiscoveredImagesCollection()
    {
        var scanJob = new ScanJob
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://example.com",
            Email = "test@example.com"
        };

        Assert.That(scanJob.DiscoveredImages, Is.Not.Null);
        Assert.That(scanJob.DiscoveredImages, Is.Empty);

        // Add images
        scanJob.DiscoveredImages.Add(new DiscoveredImage
        {
            ImageUrl = "https://example.com/img1.jpg",
            MimeType = "image/jpeg"
        });

        Assert.That(scanJob.DiscoveredImages, Has.Count.EqualTo(1));
    }

    #endregion

    #region DiscoveredImage Tests

    [Test]
    public void DiscoveredImage_CanSetAllProperties()
    {
        var imageId = Guid.NewGuid();
        var scanJobId = Guid.NewGuid();

        var image = new DiscoveredImage
        {
            Id = imageId,
            ScanJobId = scanJobId,
            ImageUrl = "https://example.com/image.jpg",
            MimeType = "image/jpeg",
            FileSize = 50000,
            EstimatedWebPSize = 37500,
            PotentialSavingsPercent = 25.0,
            PageUrl = "https://example.com/page",
            Width = 1920,
            Height = 1080
        };

        Assert.That(image.Id, Is.EqualTo(imageId));
        Assert.That(image.ScanJobId, Is.EqualTo(scanJobId));
        Assert.That(image.ImageUrl, Is.EqualTo("https://example.com/image.jpg"));
        Assert.That(image.MimeType, Is.EqualTo("image/jpeg"));
        Assert.That(image.FileSize, Is.EqualTo(50000));
        Assert.That(image.EstimatedWebPSize, Is.EqualTo(37500));
        Assert.That(image.PotentialSavingsPercent, Is.EqualTo(25.0));
        Assert.That(image.PageUrl, Is.EqualTo("https://example.com/page"));
        Assert.That(image.Width, Is.EqualTo(1920));
        Assert.That(image.Height, Is.EqualTo(1080));
    }

    [Test]
    public void DiscoveredImage_DefaultValues()
    {
        var image = new DiscoveredImage();

        Assert.That(image.Id, Is.EqualTo(Guid.Empty));
        Assert.That(image.ScanJobId, Is.EqualTo(Guid.Empty));
        Assert.That(image.ImageUrl, Is.EqualTo(string.Empty));
        Assert.That(image.MimeType, Is.EqualTo(string.Empty));
        Assert.That(image.FileSize, Is.EqualTo(0));
        Assert.That(image.EstimatedWebPSize, Is.EqualTo(0));
        Assert.That(image.PotentialSavingsPercent, Is.EqualTo(0));
        Assert.That(image.PageUrl, Is.EqualTo(string.Empty));
        Assert.That(image.Width, Is.Null);
        Assert.That(image.Height, Is.Null);
    }

    [Test]
    public void DiscoveredImage_CanReferenceScanJob()
    {
        var scanJob = new ScanJob
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://example.com",
            Email = "test@example.com"
        };

        var image = new DiscoveredImage
        {
            Id = Guid.NewGuid(),
            ScanJobId = scanJob.ScanId,
            ImageUrl = "https://example.com/image.jpg",
            MimeType = "image/jpeg",
            ScanJob = scanJob
        };

        Assert.That(image.ScanJobId, Is.EqualTo(scanJob.ScanId));
        Assert.That(image.ScanJob, Is.SameAs(scanJob));
    }

    #endregion

    #region ScanStatus Enum Tests

    [Test]
    public void ScanStatus_HasAllExpectedValues()
    {
        Assert.That((int)ScanStatus.Queued, Is.EqualTo(0));
        Assert.That((int)ScanStatus.Processing, Is.EqualTo(1));
        Assert.That((int)ScanStatus.Completed, Is.EqualTo(2));
        Assert.That((int)ScanStatus.Failed, Is.EqualTo(3));
    }

    [TestCase(ScanStatus.Queued, "Queued")]
    [TestCase(ScanStatus.Processing, "Processing")]
    [TestCase(ScanStatus.Completed, "Completed")]
    [TestCase(ScanStatus.Failed, "Failed")]
    public void ScanStatus_ToStringReturnsCorrectValue(ScanStatus status, string expected)
    {
        Assert.That(status.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public void ScanStatus_CanBeParsedFromString()
    {
        var status = Enum.Parse<ScanStatus>("Completed");
        Assert.That(status, Is.EqualTo(ScanStatus.Completed));
    }

    [Test]
    public void ScanStatus_CanBeCastFromInt()
    {
        const ScanStatus status = (ScanStatus)2;
        Assert.That(status, Is.EqualTo(ScanStatus.Completed));
    }

    #endregion
}
