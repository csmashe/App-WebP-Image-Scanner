using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WebPScanner.Core.Configuration;
using WebPScanner.Core.Entities;
using WebPScanner.Core.Interfaces;
using WebPScanner.Core.Services;

namespace WebPScanner.Core.Tests;

/// <summary>
/// Tests for QueueProcessorService background service.
/// </summary>
public class QueueProcessorServiceTests
{
    private Mock<IQueueService> _mockQueueService = null!;
    private Mock<IScanProgressService> _mockProgressService = null!;
    private Mock<ILogger<QueueProcessorService>> _mockLogger = null!;
    private QueueOptions _options = null!;
    private SecurityOptions _securityOptions = null!;
    private IServiceProvider _serviceProvider = null!;

    [SetUp]
    public void SetUp()
    {
        _mockQueueService = new Mock<IQueueService>();
        _mockProgressService = new Mock<IScanProgressService>();
        _mockLogger = new Mock<ILogger<QueueProcessorService>>();
        _options = new QueueOptions
        {
            MaxConcurrentScans = 2,
            MaxQueueSize = 100,
            MaxQueuedJobsPerIp = 3,
            FairnessSlotTicks = TimeSpan.TicksPerHour,
            PriorityAgingBoostSeconds = 30,
            CooldownAfterScanSeconds = 300,
            ProcessingIntervalSeconds = 1 // Short interval for tests
        };
        _securityOptions = new SecurityOptions
        {
            MaxScanDurationMinutes = 10,
            MaxMemoryPerScanMb = 512
        };

        var services = new ServiceCollection();
        services.AddSingleton(_mockQueueService.Object);
        services.AddSingleton(_mockProgressService.Object);
        _serviceProvider = services.BuildServiceProvider();
    }

    [Test]
    public void QueueProcessorService_CanBeInstantiated()
    {
        var optionsMock = new Mock<IOptions<QueueOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);
        var securityOptionsMock = new Mock<IOptions<SecurityOptions>>();
        securityOptionsMock.Setup(o => o.Value).Returns(_securityOptions);

        var service = new QueueProcessorService(_serviceProvider, optionsMock.Object, securityOptionsMock.Object, _mockLogger.Object);

        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public async Task QueueProcessorService_ProcessesQueueOnStart()
    {
        var optionsMock = new Mock<IOptions<QueueOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);
        var securityOptionsMock = new Mock<IOptions<SecurityOptions>>();
        securityOptionsMock.Setup(o => o.Value).Returns(_securityOptions);

        _mockQueueService.Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScanJob?)null);
        _mockQueueService.Setup(q => q.RecalculatePrioritiesWithAgingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        var service = new QueueProcessorService(_serviceProvider, optionsMock.Object, securityOptionsMock.Object, _mockLogger.Object);

        var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        // StartAsync should not throw even with short cancellation
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(50, cts.Token);
            await service.StopAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }
    }

    [Test]
    public void QueueOptions_HasCorrectDefaults()
    {
        var options = new QueueOptions();

        Assert.That(options.MaxConcurrentScans, Is.EqualTo(2));
        Assert.That(options.MaxQueueSize, Is.EqualTo(100));
        Assert.That(options.MaxQueuedJobsPerIp, Is.EqualTo(20));
        Assert.That(options.FairnessSlotTicks, Is.EqualTo(TimeSpan.TicksPerHour));
        Assert.That(options.PriorityAgingBoostSeconds, Is.EqualTo(30));
        Assert.That(options.CooldownAfterScanSeconds, Is.EqualTo(0));
        Assert.That(options.ProcessingIntervalSeconds, Is.EqualTo(5));
    }

    [Test]
    public void QueueOptions_SectionNameIsCorrect()
    {
        Assert.That(QueueOptions.SectionName, Is.EqualTo("Queue"));
    }

    [Test]
    public void QueueOptions_CanSetAllProperties()
    {
        var options = new QueueOptions
        {
            MaxConcurrentScans = 5,
            MaxQueueSize = 200,
            MaxQueuedJobsPerIp = 5,
            FairnessSlotTicks = TimeSpan.TicksPerMinute * 30,
            PriorityAgingBoostSeconds = 60,
            CooldownAfterScanSeconds = 600,
            ProcessingIntervalSeconds = 10
        };

        Assert.That(options.MaxConcurrentScans, Is.EqualTo(5));
        Assert.That(options.MaxQueueSize, Is.EqualTo(200));
        Assert.That(options.MaxQueuedJobsPerIp, Is.EqualTo(5));
        Assert.That(options.FairnessSlotTicks, Is.EqualTo(TimeSpan.TicksPerMinute * 30));
        Assert.That(options.PriorityAgingBoostSeconds, Is.EqualTo(60));
        Assert.That(options.CooldownAfterScanSeconds, Is.EqualTo(600));
        Assert.That(options.ProcessingIntervalSeconds, Is.EqualTo(10));
    }
}
