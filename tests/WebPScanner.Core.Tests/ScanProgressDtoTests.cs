using WebPScanner.Core.DTOs;

namespace WebPScanner.Core.Tests;

/// <summary>
/// Tests for ScanProgress DTOs used in SignalR communication.
/// </summary>
public class ScanProgressDtoTests
{
    #region QueuePositionUpdateDto Tests

    [Test]
    public void QueuePositionUpdateDto_CanSetProperties()
    {
        var scanId = Guid.NewGuid();
        var dto = new QueuePositionUpdateDto
        {
            ScanId = scanId,
            Position = 5,
            TotalInQueue = 10
        };

        Assert.That(dto.ScanId, Is.EqualTo(scanId));
        Assert.That(dto.Position, Is.EqualTo(5));
        Assert.That(dto.TotalInQueue, Is.EqualTo(10));
    }

    [Test]
    public void QueuePositionUpdateDto_DefaultValues()
    {
        var dto = new QueuePositionUpdateDto();

        Assert.That(dto.ScanId, Is.EqualTo(Guid.Empty));
        Assert.That(dto.Position, Is.EqualTo(0));
        Assert.That(dto.TotalInQueue, Is.EqualTo(0));
    }

    #endregion

    #region ScanStartedDto Tests

    [Test]
    public void ScanStartedDto_CanSetProperties()
    {
        var scanId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow;

        var dto = new ScanStartedDto
        {
            ScanId = scanId,
            TargetUrl = "https://example.com",
            StartedAt = startedAt
        };

        Assert.That(dto.ScanId, Is.EqualTo(scanId));
        Assert.That(dto.TargetUrl, Is.EqualTo("https://example.com"));
        Assert.That(dto.StartedAt, Is.EqualTo(startedAt));
    }

    [Test]
    public void ScanStartedDto_DefaultValues()
    {
        var dto = new ScanStartedDto();

        Assert.That(dto.ScanId, Is.EqualTo(Guid.Empty));
        Assert.That(dto.TargetUrl, Is.EqualTo(string.Empty));
        Assert.That(dto.StartedAt, Is.EqualTo(default(DateTime)));
    }

    #endregion

    #region PageProgressDto Tests

    [Test]
    public void PageProgressDto_CanSetProperties()
    {
        var scanId = Guid.NewGuid();

        var dto = new PageProgressDto
        {
            ScanId = scanId,
            CurrentUrl = "https://example.com/page",
            PagesScanned = 10,
            PagesDiscovered = 25,
            ProgressPercent = 40
        };

        Assert.That(dto.ScanId, Is.EqualTo(scanId));
        Assert.That(dto.CurrentUrl, Is.EqualTo("https://example.com/page"));
        Assert.That(dto.PagesScanned, Is.EqualTo(10));
        Assert.That(dto.PagesDiscovered, Is.EqualTo(25));
        Assert.That(dto.ProgressPercent, Is.EqualTo(40));
    }

    [Test]
    public void PageProgressDto_DefaultValues()
    {
        var dto = new PageProgressDto();

        Assert.That(dto.ScanId, Is.EqualTo(Guid.Empty));
        Assert.That(dto.CurrentUrl, Is.EqualTo(string.Empty));
        Assert.That(dto.PagesScanned, Is.EqualTo(0));
        Assert.That(dto.PagesDiscovered, Is.EqualTo(0));
        Assert.That(dto.ProgressPercent, Is.EqualTo(0));
    }

    #endregion

    #region ImageFoundDto Tests

    [Test]
    public void ImageFoundDto_CanSetProperties()
    {
        var scanId = Guid.NewGuid();

        var dto = new ImageFoundDto
        {
            ScanId = scanId,
            ImageUrl = "https://example.com/image.jpg",
            MimeType = "image/jpeg",
            Size = 50000,
            PageUrl = "https://example.com/page",
            IsNonWebP = true,
            TotalNonWebPCount = 25
        };

        Assert.That(dto.ScanId, Is.EqualTo(scanId));
        Assert.That(dto.ImageUrl, Is.EqualTo("https://example.com/image.jpg"));
        Assert.That(dto.MimeType, Is.EqualTo("image/jpeg"));
        Assert.That(dto.Size, Is.EqualTo(50000));
        Assert.That(dto.PageUrl, Is.EqualTo("https://example.com/page"));
        Assert.That(dto.IsNonWebP, Is.True);
        Assert.That(dto.TotalNonWebPCount, Is.EqualTo(25));
    }

    [Test]
    public void ImageFoundDto_DefaultValues()
    {
        var dto = new ImageFoundDto();

        Assert.That(dto.ScanId, Is.EqualTo(Guid.Empty));
        Assert.That(dto.ImageUrl, Is.EqualTo(string.Empty));
        Assert.That(dto.MimeType, Is.EqualTo(string.Empty));
        Assert.That(dto.Size, Is.EqualTo(0));
        Assert.That(dto.PageUrl, Is.EqualTo(string.Empty));
        Assert.That(dto.IsNonWebP, Is.False);
        Assert.That(dto.TotalNonWebPCount, Is.EqualTo(0));
    }

    #endregion

    #region ScanCompleteDto Tests

    [Test]
    public void ScanCompleteDto_CanSetProperties()
    {
        var scanId = Guid.NewGuid();
        var completedAt = DateTime.UtcNow;
        var duration = TimeSpan.FromMinutes(5);

        var dto = new ScanCompleteDto
        {
            ScanId = scanId,
            CompletedAt = completedAt,
            Duration = duration,
            TotalPagesScanned = 50,
            TotalImagesFound = 100,
            NonWebPImagesCount = 25,
            ReachedPageLimit = true
        };

        Assert.That(dto.ScanId, Is.EqualTo(scanId));
        Assert.That(dto.CompletedAt, Is.EqualTo(completedAt));
        Assert.That(dto.Duration, Is.EqualTo(duration));
        Assert.That(dto.TotalPagesScanned, Is.EqualTo(50));
        Assert.That(dto.TotalImagesFound, Is.EqualTo(100));
        Assert.That(dto.NonWebPImagesCount, Is.EqualTo(25));
        Assert.That(dto.ReachedPageLimit, Is.True);
    }

    [Test]
    public void ScanCompleteDto_DefaultValues()
    {
        var dto = new ScanCompleteDto();

        Assert.That(dto.ScanId, Is.EqualTo(Guid.Empty));
        Assert.That(dto.CompletedAt, Is.EqualTo(default(DateTime)));
        Assert.That(dto.Duration, Is.EqualTo(TimeSpan.Zero));
        Assert.That(dto.TotalPagesScanned, Is.EqualTo(0));
        Assert.That(dto.TotalImagesFound, Is.EqualTo(0));
        Assert.That(dto.NonWebPImagesCount, Is.EqualTo(0));
        Assert.That(dto.ReachedPageLimit, Is.False);
    }

    #endregion

    #region ScanFailedDto Tests

    [Test]
    public void ScanFailedDto_CanSetProperties()
    {
        var scanId = Guid.NewGuid();
        var failedAt = DateTime.UtcNow;

        var dto = new ScanFailedDto
        {
            ScanId = scanId,
            FailedAt = failedAt,
            ErrorMessage = "Connection timeout"
        };

        Assert.That(dto.ScanId, Is.EqualTo(scanId));
        Assert.That(dto.FailedAt, Is.EqualTo(failedAt));
        Assert.That(dto.ErrorMessage, Is.EqualTo("Connection timeout"));
    }

    [Test]
    public void ScanFailedDto_DefaultValues()
    {
        var dto = new ScanFailedDto();

        Assert.That(dto.ScanId, Is.EqualTo(Guid.Empty));
        Assert.That(dto.FailedAt, Is.EqualTo(default(DateTime)));
        Assert.That(dto.ErrorMessage, Is.EqualTo(string.Empty));
    }

    #endregion
}
