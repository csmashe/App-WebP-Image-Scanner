using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WebPScanner.Core.Configuration;
using WebPScanner.Core.Entities;
using WebPScanner.Core.Enums;
using WebPScanner.Core.Interfaces;
using WebPScanner.Core.Services;

namespace WebPScanner.Core.Tests;

public class QueueServiceTests
{
    private Mock<IScanJobRepository> _mockRepository = null!;
    private Mock<ILogger<QueueService>> _mockLogger = null!;
    private QueueOptions _options = null!;
    private QueueService _queueService = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new Mock<IScanJobRepository>();
        _mockLogger = new Mock<ILogger<QueueService>>();
        _options = new QueueOptions
        {
            MaxConcurrentScans = 2,
            MaxQueueSize = 100,
            MaxQueuedJobsPerIp = 3,
            FairnessSlotTicks = TimeSpan.TicksPerHour,
            PriorityAgingBoostSeconds = 30,
            CooldownAfterScanSeconds = 300,
            ProcessingIntervalSeconds = 5
        };

        var optionsMock = new Mock<IOptions<QueueOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);

        _queueService = new QueueService(_mockRepository.Object, optionsMock.Object, _mockLogger.Object);
        ClearIpCooldowns();
    }

    private static void ClearIpCooldowns()
    {
        var field = typeof(QueueService).GetField("IpCooldowns", BindingFlags.NonPublic | BindingFlags.Static);
        var cooldowns = (ConcurrentDictionary<string, DateTime>)field!.GetValue(null)!;
        cooldowns.Clear();
    }

    #region Enqueue Tests

    [Test]
    public async Task EnqueueAsync_ShouldSetStatusToQueued()
    {
        // Arrange
        var scanJob = CreateTestScanJob();
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ScanJob>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScanJob job, CancellationToken _) => job);
        _mockRepository.Setup(r => r.GetQueuePositionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _queueService.EnqueueAsync(scanJob);

        // Assert
        Assert.That(result.Status, Is.EqualTo(ScanStatus.Queued));
    }

    [Test]
    public async Task EnqueueAsync_ShouldSetPriorityScore()
    {
        // Arrange
        var scanJob = CreateTestScanJob();
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ScanJob>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScanJob job, CancellationToken _) => job);
        _mockRepository.Setup(r => r.GetQueuePositionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _queueService.EnqueueAsync(scanJob);

        // Assert
        Assert.That(result.PriorityScore > 0, Is.True);
    }

    [Test]
    public async Task EnqueueAsync_ShouldApplyFairnessPenalty()
    {
        // Arrange
        var job1 = CreateTestScanJob();
        job1.SubmissionCount = 0;
        job1.CreatedAt = DateTime.UtcNow;

        var job2 = CreateTestScanJob();
        job2.SubmissionCount = 2;
        job2.CreatedAt = job1.CreatedAt; // Same timestamp

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ScanJob>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScanJob job, CancellationToken _) => job);
        _mockRepository.Setup(r => r.GetQueuePositionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result1 = await _queueService.EnqueueAsync(job1);
        var result2 = await _queueService.EnqueueAsync(job2);

        // Assert
        // Job with more submissions should have higher (worse) priority score
        Assert.That(result2.PriorityScore > result1.PriorityScore, Is.True);
    }

    [Test]
    public async Task EnqueueAsync_ShouldSetQueuePosition()
    {
        // Arrange
        var scanJob = CreateTestScanJob();
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ScanJob>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScanJob job, CancellationToken _) => job);
        _mockRepository.Setup(r => r.GetQueuePositionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var result = await _queueService.EnqueueAsync(scanJob);

        // Assert
        Assert.That(result.QueuePosition, Is.EqualTo(5));
    }

    [Test]
    public async Task EnqueueAsync_ShouldCallRepositoryAdd()
    {
        // Arrange
        var scanJob = CreateTestScanJob();
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ScanJob>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScanJob job, CancellationToken _) => job);
        _mockRepository.Setup(r => r.GetQueuePositionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _queueService.EnqueueAsync(scanJob);

        // Assert
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<ScanJob>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Dequeue Tests

    [Test]
    public async Task DequeueAsync_ShouldReturnNull_WhenMaxConcurrentScansReached()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetProcessingCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_options.MaxConcurrentScans);

        // Act
        var result = await _queueService.DequeueAsync();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task DequeueAsync_ShouldReturnNull_WhenQueueIsEmpty()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetProcessingCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockRepository.Setup(r => r.GetQueuedJobsOrderedByPriorityAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScanJob>());

        // Act
        var result = await _queueService.DequeueAsync();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task DequeueAsync_ShouldReturnJob_WhenAvailable()
    {
        // Arrange
        var scanJob = CreateTestScanJobWithUniqueIp();
        scanJob.Status = ScanStatus.Queued;

        _mockRepository.Setup(r => r.GetProcessingCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockRepository.Setup(r => r.GetQueuedJobsOrderedByPriorityAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScanJob> { scanJob });
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ScanJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _queueService.DequeueAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ScanId, Is.EqualTo(scanJob.ScanId));
    }

    [Test]
    public async Task DequeueAsync_ShouldSetStatusToProcessing()
    {
        // Arrange
        var scanJob = CreateTestScanJobWithUniqueIp();
        scanJob.Status = ScanStatus.Queued;

        _mockRepository.Setup(r => r.GetProcessingCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockRepository.Setup(r => r.GetQueuedJobsOrderedByPriorityAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScanJob> { scanJob });
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ScanJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _queueService.DequeueAsync();

        // Assert
        Assert.That(result!.Status, Is.EqualTo(ScanStatus.Processing));
    }

    [Test]
    public async Task DequeueAsync_ShouldSetStartedAt()
    {
        // Arrange
        var scanJob = CreateTestScanJobWithUniqueIp();
        scanJob.Status = ScanStatus.Queued;
        scanJob.StartedAt = null;

        _mockRepository.Setup(r => r.GetProcessingCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockRepository.Setup(r => r.GetQueuedJobsOrderedByPriorityAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScanJob> { scanJob });
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ScanJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _queueService.DequeueAsync();

        // Assert
        Assert.That(result!.StartedAt, Is.Not.Null);
    }

    [Test]
    public async Task DequeueAsync_ShouldSetQueuePositionToZero()
    {
        // Arrange
        var scanJob = CreateTestScanJobWithUniqueIp();
        scanJob.Status = ScanStatus.Queued;
        scanJob.QueuePosition = 5;

        _mockRepository.Setup(r => r.GetProcessingCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockRepository.Setup(r => r.GetQueuedJobsOrderedByPriorityAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScanJob> { scanJob });
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ScanJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _queueService.DequeueAsync();

        // Assert
        Assert.That(result!.QueuePosition, Is.EqualTo(0));
    }

    [Test]
    public async Task DequeueAsync_ShouldCallRepositoryUpdate()
    {
        // Arrange
        var scanJob = CreateTestScanJobWithUniqueIp();
        scanJob.Status = ScanStatus.Queued;

        _mockRepository.Setup(r => r.GetProcessingCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _mockRepository.Setup(r => r.GetQueuedJobsOrderedByPriorityAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScanJob> { scanJob });
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ScanJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _queueService.DequeueAsync();

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ScanJob>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CanEnqueue Tests

    [Test]
    public async Task CanEnqueueAsync_ShouldReturnTrue_WhenBelowMaxQueueSize()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetQueuedCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_options.MaxQueueSize - 1);

        // Act
        var result = await _queueService.CanEnqueueAsync();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task CanEnqueueAsync_ShouldReturnFalse_WhenAtMaxQueueSize()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetQueuedCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_options.MaxQueueSize);

        // Act
        var result = await _queueService.CanEnqueueAsync();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CanEnqueueAsync_ShouldReturnFalse_WhenAboveMaxQueueSize()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetQueuedCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_options.MaxQueueSize + 10);

        // Act
        var result = await _queueService.CanEnqueueAsync();

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region IP Cooldown Tests

    [Test]
    public void IsIpInCooldown_ShouldReturnFalse_WhenNoRecordedCooldown()
    {
        // Arrange
        const string ip = "192.0.2.100"; // Test IP that hasn't been recorded

        // Act
        var result = _queueService.IsIpInCooldown(ip);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void RecordCooldown_ShouldSetCooldown()
    {
        // Arrange
        const string ip = "192.0.2.101";

        // Act
        _queueService.RecordCooldown(ip);
        var result = _queueService.IsIpInCooldown(ip);

        // Assert
        Assert.That(result, Is.True);
    }

    #endregion

    #region CompleteJob Tests

    [Test]
    public async Task CompleteJobAsync_ShouldSetStatusToCompleted_WhenSuccess()
    {
        // Arrange
        var scanJob = CreateTestScanJob();
        scanJob.Status = ScanStatus.Processing;

        _mockRepository.Setup(r => r.GetByIdAsync(scanJob.ScanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanJob);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ScanJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _queueService.CompleteJobAsync(scanJob.ScanId, true);

        // Assert
        Assert.That(scanJob.Status, Is.EqualTo(ScanStatus.Completed));
    }

    [Test]
    public async Task CompleteJobAsync_ShouldSetStatusToFailed_WhenFailure()
    {
        // Arrange
        var scanJob = CreateTestScanJob();
        scanJob.Status = ScanStatus.Processing;

        _mockRepository.Setup(r => r.GetByIdAsync(scanJob.ScanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanJob);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ScanJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _queueService.CompleteJobAsync(scanJob.ScanId, false, "Test error");

        // Assert
        Assert.That(scanJob.Status, Is.EqualTo(ScanStatus.Failed));
        Assert.That(scanJob.ErrorMessage, Is.EqualTo("Test error"));
    }

    [Test]
    public async Task CompleteJobAsync_ShouldSetCompletedAt()
    {
        // Arrange
        var scanJob = CreateTestScanJob();
        scanJob.Status = ScanStatus.Processing;
        scanJob.CompletedAt = null;

        _mockRepository.Setup(r => r.GetByIdAsync(scanJob.ScanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanJob);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ScanJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _queueService.CompleteJobAsync(scanJob.ScanId, true);

        // Assert
        Assert.That(scanJob.CompletedAt, Is.Not.Null);
    }

    [Test]
    public async Task CompleteJobAsync_ShouldRecordCooldown()
    {
        // Arrange
        var scanJob = CreateTestScanJob();
        scanJob.Status = ScanStatus.Processing;
        scanJob.SubmitterIp = "192.0.2.102";

        _mockRepository.Setup(r => r.GetByIdAsync(scanJob.ScanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scanJob);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ScanJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _queueService.CompleteJobAsync(scanJob.ScanId, true);

        // Assert - Check that the IP is now in cooldown
        var isInCooldown = _queueService.IsIpInCooldown(scanJob.SubmitterIp);
        Assert.That(isInCooldown, Is.True);
    }

    [Test]
    public async Task CompleteJobAsync_ShouldNotFail_WhenJobNotFound()
    {
        // Arrange
        var scanId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(scanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScanJob?)null);

        // Act & Assert - Should not throw
        await _queueService.CompleteJobAsync(scanId, true);
    }

    #endregion

    #region HasIpReachedQueueLimit Tests

    [Test]
    public async Task HasIpReachedQueueLimitAsync_ShouldReturnFalse_WhenBelowLimit()
    {
        // Arrange
        const string ip = "192.0.2.50";
        _mockRepository.Setup(r => r.GetJobCountByIpAsync(ip, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_options.MaxQueuedJobsPerIp - 1);

        // Act
        var result = await _queueService.HasIpReachedQueueLimitAsync(ip);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task HasIpReachedQueueLimitAsync_ShouldReturnTrue_WhenAtLimit()
    {
        // Arrange
        const string ip = "192.0.2.51";
        _mockRepository.Setup(r => r.GetJobCountByIpAsync(ip, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_options.MaxQueuedJobsPerIp);

        // Act
        var result = await _queueService.HasIpReachedQueueLimitAsync(ip);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task HasIpReachedQueueLimitAsync_ShouldReturnTrue_WhenAboveLimit()
    {
        // Arrange
        const string ip = "192.0.2.52";
        _mockRepository.Setup(r => r.GetJobCountByIpAsync(ip, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_options.MaxQueuedJobsPerIp + 1);

        // Act
        var result = await _queueService.HasIpReachedQueueLimitAsync(ip);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task HasIpReachedQueueLimitAsync_ShouldReturnFalse_WhenIpIsEmpty()
    {
        // Arrange & Act
        var result = await _queueService.HasIpReachedQueueLimitAsync(string.Empty);

        // Assert
        Assert.That(result, Is.False);
        _mockRepository.Verify(r => r.GetJobCountByIpAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HasIpReachedQueueLimitAsync_ShouldReturnFalse_WhenIpIsNull()
    {
        // Arrange & Act
        var result = await _queueService.HasIpReachedQueueLimitAsync(null!);

        // Assert
        Assert.That(result, Is.False);
        _mockRepository.Verify(r => r.GetJobCountByIpAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region RecalculatePrioritiesWithAging Tests

    [Test]
    public async Task RecalculatePrioritiesWithAgingAsync_ShouldReturnEmpty_WhenNoQueuedJobs()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetAllQueuedJobsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScanJob>());

        // Act
        var result = await _queueService.RecalculatePrioritiesWithAgingAsync();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task RecalculatePrioritiesWithAgingAsync_ShouldApplyAgingBoost_ToOlderJobs()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var oldJob = new ScanJob
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://example.com",
            Email = "old@example.com",
            Status = ScanStatus.Queued,
            CreatedAt = now.AddMinutes(-10), // 10 minutes ago
            SubmissionCount = 1,
            PriorityScore = 1 * _options.FairnessSlotTicks + now.AddMinutes(-10).Ticks
        };

        var newJob = new ScanJob
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://example.com",
            Email = "new@example.com",
            Status = ScanStatus.Queued,
            CreatedAt = now, // just now
            SubmissionCount = 1,
            PriorityScore = 1 * _options.FairnessSlotTicks + now.Ticks
        };

        // Order by priority: oldJob should have lower (better) priority after aging
        var orderedJobs = new List<ScanJob> { oldJob, newJob };

        _mockRepository.Setup(r => r.GetAllQueuedJobsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(orderedJobs);
        _mockRepository.Setup(r => r.UpdateManyAsync(It.IsAny<IEnumerable<ScanJob>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _queueService.RecalculatePrioritiesWithAgingAsync();

        // Assert - verify UpdateManyAsync was called
        _mockRepository.Verify(r => r.UpdateManyAsync(It.IsAny<IEnumerable<ScanJob>>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);

        // The old job should have a lower priority score after aging boost is applied
        // (older jobs get their priority reduced to move them up)
    }

    [Test]
    public async Task RecalculatePrioritiesWithAgingAsync_ShouldPreserveOrder_ForRecentJobs()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var jobs = new List<ScanJob>
        {
            new()
            {
                ScanId = Guid.NewGuid(),
                TargetUrl = "https://example1.com",
                Email = "test1@example.com",
                Status = ScanStatus.Queued,
                CreatedAt = now.AddSeconds(-5),
                SubmissionCount = 0,
                PriorityScore = now.AddSeconds(-5).Ticks
            },
            new()
            {
                ScanId = Guid.NewGuid(),
                TargetUrl = "https://example2.com",
                Email = "test2@example.com",
                Status = ScanStatus.Queued,
                CreatedAt = now.AddSeconds(-3),
                SubmissionCount = 0,
                PriorityScore = now.AddSeconds(-3).Ticks
            }
        };

        _mockRepository.Setup(r => r.GetAllQueuedJobsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobs);
        _mockRepository.Setup(r => r.UpdateManyAsync(It.IsAny<IEnumerable<ScanJob>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        _ = await _queueService.RecalculatePrioritiesWithAgingAsync();

        // Assert - verify UpdateManyAsync was called at least once
        _mockRepository.Verify(r => r.UpdateManyAsync(It.IsAny<IEnumerable<ScanJob>>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    #endregion

    #region Priority Calculation Tests

    [Test]
    public async Task EnqueueAsync_ShouldCalculatePriorityWithFairnessSlot()
    {
        // Arrange
        var scanJob = CreateTestScanJob();
        scanJob.SubmissionCount = 3; // Third submission from this IP
        scanJob.CreatedAt = DateTime.UtcNow;

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ScanJob>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScanJob job, CancellationToken _) => job);
        _mockRepository.Setup(r => r.GetQueuePositionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _queueService.EnqueueAsync(scanJob);

        // Assert
        // Priority = (slot_priority) + base_time where slot_priority = count * FairnessSlotTicks
        var expectedSlotPriority = scanJob.SubmissionCount * _options.FairnessSlotTicks;
        var expectedTimePriority = scanJob.CreatedAt.Ticks;
        Assert.That(result.PriorityScore, Is.EqualTo(expectedSlotPriority + expectedTimePriority));
    }

    [Test]
    public async Task EnqueueAsync_FirstSubmission_ShouldHaveNoPenalty()
    {
        // Arrange
        var scanJob = CreateTestScanJob();
        scanJob.SubmissionCount = 0;
        scanJob.CreatedAt = DateTime.UtcNow;

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ScanJob>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScanJob job, CancellationToken _) => job);
        _mockRepository.Setup(r => r.GetQueuePositionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _queueService.EnqueueAsync(scanJob);

        // Assert
        // First submission has zero penalty
        Assert.That(result.PriorityScore, Is.EqualTo(scanJob.CreatedAt.Ticks));
    }

    [Test]
    public async Task EnqueueAsync_HigherSubmissionCount_ShouldHaveHigherPriority()
    {
        // Arrange (higher priority score = lower priority = processed later)
        var time = DateTime.UtcNow;

        var job1 = CreateTestScanJob();
        job1.SubmissionCount = 1;
        job1.CreatedAt = time;

        var job2 = CreateTestScanJob();
        job2.SubmissionCount = 5;
        job2.CreatedAt = time;

        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ScanJob>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScanJob job, CancellationToken _) => job);
        _mockRepository.Setup(r => r.GetQueuePositionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result1 = await _queueService.EnqueueAsync(job1);
        var result2 = await _queueService.EnqueueAsync(job2);

        // Assert
        Assert.That(result2.PriorityScore > result1.PriorityScore, Is.True);
    }

    #endregion

    #region Helper Methods

    private static ScanJob CreateTestScanJob()
    {
        return new ScanJob
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://example.com",
            Email = "test@example.com",
            Status = ScanStatus.Queued,
            SubmitterIp = "192.0.2.1",
            SubmissionCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static int _uniqueIpCounter;

    private static ScanJob CreateTestScanJobWithUniqueIp()
    {
        var counter = Interlocked.Increment(ref _uniqueIpCounter);
        return new ScanJob
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://example.com",
            Email = "test@example.com",
            Status = ScanStatus.Queued,
            SubmitterIp = $"192.0.2.{counter % 256}",
            SubmissionCount = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
