using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WebPScanner.Api.Hubs;
using WebPScanner.Api.Services;
using WebPScanner.Core.Configuration;
using WebPScanner.Core.DTOs;
using WebPScanner.Core.Entities;
using WebPScanner.Core.Enums;
using WebPScanner.Core.Interfaces;

namespace WebPScanner.Api.Tests;

[TestFixture]
public class ScanProgressServiceTests
{
    private Mock<IHubContext<ScanProgressHub, IScanProgressClient>> _mockHubContext = null!;
    private Mock<IScanJobRepository> _mockRepository = null!;
    private Mock<IAggregateStatsService> _mockAggregateStatsService = null!;
    private Mock<ILiveScanStatsTracker> _mockLiveScanStatsTracker = null!;
    private Mock<IOptions<QueueOptions>> _mockQueueOptions = null!;
    private Mock<ILogger<ScanProgressService>> _mockLogger = null!;
    private Mock<IScanProgressClient> _mockClient = null!;
    private Mock<IHubClients<IScanProgressClient>> _mockClients = null!;
    private ScanProgressService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockHubContext = new Mock<IHubContext<ScanProgressHub, IScanProgressClient>>();
        _mockRepository = new Mock<IScanJobRepository>();
        _mockAggregateStatsService = new Mock<IAggregateStatsService>();
        _mockLiveScanStatsTracker = new Mock<ILiveScanStatsTracker>();
        _mockQueueOptions = new Mock<IOptions<QueueOptions>>();
        _mockLogger = new Mock<ILogger<ScanProgressService>>();
        _mockClient = new Mock<IScanProgressClient>();
        _mockClients = new Mock<IHubClients<IScanProgressClient>>();

        _mockHubContext.Setup(x => x.Clients).Returns(_mockClients.Object);
        _mockClients.Setup(x => x.Group(It.IsAny<string>())).Returns(_mockClient.Object);
        _mockQueueOptions.Setup(x => x.Value).Returns(new QueueOptions());
        _mockLiveScanStatsTracker.Setup(x => x.GetTotalRemainingPagesForActiveScans()).Returns(0);
        _mockLiveScanStatsTracker.Setup(x => x.GetActiveScansRemainingPagesSorted()).Returns([]);
        _mockAggregateStatsService.Setup(x => x.GetAverageTimePerPageTicksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0L);

        _service = new ScanProgressService(
            _mockHubContext.Object,
            _mockRepository.Object,
            _mockAggregateStatsService.Object,
            _mockLiveScanStatsTracker.Object,
            _mockQueueOptions.Object,
            _mockLogger.Object);
    }

    [Test]
    public async Task BroadcastQueuePositionsAsync_SendsToAllQueuedJobs()
    {
        // Arrange
        var jobs = new List<ScanJob>
        {
            new() { ScanId = Guid.NewGuid(), Status = ScanStatus.Queued },
            new() { ScanId = Guid.NewGuid(), Status = ScanStatus.Queued },
            new() { ScanId = Guid.NewGuid(), Status = ScanStatus.Queued }
        };

        _mockRepository
            .Setup(x => x.GetQueuedCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        _mockRepository
            .Setup(x => x.GetQueuedJobsOrderedByPriorityAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobs);

        _mockClient
            .Setup(x => x.QueuePositionUpdate(It.IsAny<QueuePositionUpdateDto>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.BroadcastQueuePositionsAsync();

        // Assert
        _mockClient.Verify(
            x => x.QueuePositionUpdate(It.Is<QueuePositionUpdateDto>(u => u.Position == 1 && u.TotalInQueue == 3)),
            Times.Once);
        _mockClient.Verify(
            x => x.QueuePositionUpdate(It.Is<QueuePositionUpdateDto>(u => u.Position == 2 && u.TotalInQueue == 3)),
            Times.Once);
        _mockClient.Verify(
            x => x.QueuePositionUpdate(It.Is<QueuePositionUpdateDto>(u => u.Position == 3 && u.TotalInQueue == 3)),
            Times.Once);
    }

    [Test]
    public async Task BroadcastQueuePositionsAsync_HandlesEmptyQueue()
    {
        // Arrange
        _mockRepository
            .Setup(x => x.GetQueuedCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockRepository
            .Setup(x => x.GetQueuedJobsOrderedByPriorityAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScanJob>());

        // Act
        await _service.BroadcastQueuePositionsAsync();

        // Assert
        _mockClient.Verify(
            x => x.QueuePositionUpdate(It.IsAny<QueuePositionUpdateDto>()),
            Times.Never);
    }

    [Test]
    public async Task SendScanStartedAsync_SendsToCorrectGroup()
    {
        // Arrange
        var scanId = Guid.NewGuid();
        var notification = new ScanStartedDto
        {
            ScanId = scanId,
            TargetUrl = "https://example.com",
            StartedAt = DateTime.UtcNow
        };

        _mockClient
            .Setup(x => x.ScanStarted(It.IsAny<ScanStartedDto>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SendScanStartedAsync(notification);

        // Assert
        _mockClients.Verify(x => x.Group($"scan-{scanId}"), Times.Once);
        _mockClient.Verify(x => x.ScanStarted(notification), Times.Once);
    }

    [Test]
    public async Task SendPageProgressAsync_SendsToCorrectGroup()
    {
        // Arrange
        var scanId = Guid.NewGuid();
        var progress = new PageProgressDto
        {
            ScanId = scanId,
            CurrentUrl = "https://example.com/page1",
            PagesScanned = 5,
            PagesDiscovered = 20,
            ProgressPercent = 25
        };

        _mockClient
            .Setup(x => x.PageProgress(It.IsAny<PageProgressDto>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SendPageProgressAsync(progress);

        // Assert
        _mockClients.Verify(x => x.Group($"scan-{scanId}"), Times.Once);
        _mockClient.Verify(x => x.PageProgress(progress), Times.Once);
    }

    [Test]
    public async Task SendImageFoundAsync_SendsToCorrectGroup()
    {
        // Arrange
        var scanId = Guid.NewGuid();
        var imageFound = new ImageFoundDto
        {
            ScanId = scanId,
            ImageUrl = "https://example.com/image.png",
            MimeType = "image/png",
            Size = 102400,
            IsNonWebP = true,
            TotalNonWebPCount = 5,
            PageUrl = "https://example.com/page1"
        };

        _mockClient
            .Setup(x => x.ImageFound(It.IsAny<ImageFoundDto>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SendImageFoundAsync(imageFound);

        // Assert
        _mockClients.Verify(x => x.Group($"scan-{scanId}"), Times.Once);
        _mockClient.Verify(x => x.ImageFound(imageFound), Times.Once);
    }

    [Test]
    public async Task SendScanCompleteAsync_SendsToCorrectGroup()
    {
        // Arrange
        var scanId = Guid.NewGuid();
        var notification = new ScanCompleteDto
        {
            ScanId = scanId,
            TotalPagesScanned = 50,
            TotalImagesFound = 200,
            NonWebPImagesCount = 45,
            Duration = TimeSpan.FromMinutes(5),
            CompletedAt = DateTime.UtcNow,
            ReachedPageLimit = false
        };

        _mockClient
            .Setup(x => x.ScanComplete(It.IsAny<ScanCompleteDto>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SendScanCompleteAsync(notification);

        // Assert
        _mockClients.Verify(x => x.Group($"scan-{scanId}"), Times.Once);
        _mockClient.Verify(x => x.ScanComplete(notification), Times.Once);
    }

    [Test]
    public async Task SendScanFailedAsync_SendsToCorrectGroup()
    {
        // Arrange
        var scanId = Guid.NewGuid();
        var notification = new ScanFailedDto
        {
            ScanId = scanId,
            ErrorMessage = "Connection timed out",
            FailedAt = DateTime.UtcNow
        };

        _mockClient
            .Setup(x => x.ScanFailed(It.IsAny<ScanFailedDto>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SendScanFailedAsync(notification);

        // Assert
        _mockClients.Verify(x => x.Group($"scan-{scanId}"), Times.Once);
        _mockClient.Verify(x => x.ScanFailed(notification), Times.Once);
    }

}
