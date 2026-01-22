using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPScanner.Core.Configuration;
using WebPScanner.Core.Interfaces;

namespace WebPScanner.Core.Services;

/// <summary>
/// Background service that periodically cleans up old scan data.
/// </summary>
public class DataRetentionService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DataRetentionOptions _options;
    private readonly ILogger<DataRetentionService> _logger;

    public DataRetentionService(
        IServiceProvider serviceProvider,
        IOptions<DataRetentionOptions> options,
        ILogger<DataRetentionService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.RetentionHours <= 0)
        {
            _logger.LogInformation(
                "Data retention is disabled (RetentionHours = {RetentionHours}, must be > 0 to enable)",
                _options.RetentionHours);
            return;
        }

        const int minimumIntervalMinutes = 1;
        var cleanupIntervalMinutes = _options.CleanupIntervalMinutes;
        if (cleanupIntervalMinutes < minimumIntervalMinutes)
        {
            _logger.LogWarning(
                "CleanupIntervalMinutes value {ConfiguredValue} is invalid (must be >= {Min}). Using minimum value of {Minimum} minutes",
                cleanupIntervalMinutes, minimumIntervalMinutes, minimumIntervalMinutes);
            cleanupIntervalMinutes = minimumIntervalMinutes;
        }

        _logger.LogInformation(
            "Data retention service started. Retention period: {Hours} hours, cleanup interval: {Interval} minutes",
            _options.RetentionHours, cleanupIntervalMinutes);

        try
        {
            // Initial delay to allow the app to start up
            await Task.Delay(TimeSpan.FromMinutes(cleanupIntervalMinutes), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupOldScansAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Cancellation requested during cleanup, exit gracefully
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during data retention cleanup");
                }

                await Task.Delay(TimeSpan.FromMinutes(cleanupIntervalMinutes), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation requested during delay, exit gracefully
        }

        _logger.LogInformation("Data retention service stopped");
    }

    private async Task CleanupOldScansAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var scanJobRepository = scope.ServiceProvider.GetRequiredService<IScanJobRepository>();

        var cutoffTime = DateTime.UtcNow.AddHours(-_options.RetentionHours);

        // Validate and clamp MaxDeletesPerRun
        const int minimumMaxDeletes = 1;
        var maxDeletesPerRun = _options.MaxDeletesPerRun;
        if (maxDeletesPerRun < minimumMaxDeletes)
        {
            _logger.LogWarning(
                "MaxDeletesPerRun value {ConfiguredValue} is invalid (must be >= {Min}). Using minimum value of {Minimum}",
                maxDeletesPerRun, minimumMaxDeletes, minimumMaxDeletes);
            maxDeletesPerRun = minimumMaxDeletes;
        }

        _logger.LogDebug("Running data retention cleanup. Deleting scans completed before {CutoffTime}", cutoffTime);

        // Get completed scans older than the retention period
        var oldScans = await scanJobRepository.GetCompletedScansBeforeAsync(cutoffTime, maxDeletesPerRun, cancellationToken);
        var scanList = oldScans.ToList();

        if (scanList.Count == 0)
        {
            _logger.LogDebug("No old scans to delete");
            return;
        }

        var deletedCount = 0;
        foreach (var scan in scanList)
        {
            try
            {
                await scanJobRepository.DeleteAsync(scan.ScanId, cancellationToken);
                deletedCount++;

                _logger.LogDebug(
                    "Deleted old scan {ScanId} (completed {CompletedAt})",
                    scan.ScanId, scan.CompletedAt);
            }
            catch (OperationCanceledException)
            {
                // Cancellation requested, let it bubble up to stop the cleanup
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete scan {ScanId}", scan.ScanId);
            }
        }

        _logger.LogInformation(
            "Data retention cleanup complete. Deleted {Count} old scans",
            deletedCount);
    }
}
