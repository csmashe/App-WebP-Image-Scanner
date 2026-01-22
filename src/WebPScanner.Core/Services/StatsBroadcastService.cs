using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WebPScanner.Core.Interfaces;

namespace WebPScanner.Core.Services;

/// <summary>
/// Background service that periodically broadcasts stats updates while scans are running.
/// Only broadcasts when there are active scans to avoid unnecessary traffic.
/// </summary>
public class StatsBroadcastService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StatsBroadcastService> _logger;

    /// <summary>
    /// How often to check and broadcast stats (in seconds).
    /// </summary>
    private const int BroadcastIntervalSeconds = 3;

    public StatsBroadcastService(
        IServiceProvider serviceProvider,
        ILogger<StatsBroadcastService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Stats broadcast service started. Checking every {Interval} seconds",
            BroadcastIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndBroadcastAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in stats broadcast service");
            }

            await Task.Delay(TimeSpan.FromSeconds(BroadcastIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Stats broadcast service stopped");
    }

    private async Task CheckAndBroadcastAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var scanJobRepository = scope.ServiceProvider.GetRequiredService<IScanJobRepository>();

        // Check if any scans are currently processing
        var processingCount = await scanJobRepository.GetProcessingCountAsync(cancellationToken);

        if (processingCount == 0)
        {
            // No active scans, no need to broadcast
            return;
        }

        _logger.LogDebug("Broadcasting stats update ({ProcessingCount} scans in progress)", processingCount);

        // Get the progress service and broadcast stats
        var progressService = scope.ServiceProvider.GetService<IScanProgressService>();
        if (progressService != null)
        {
            await progressService.BroadcastStatsUpdateAsync(cancellationToken);
        }
    }
}
