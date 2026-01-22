using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPScanner.Core.Configuration;
using WebPScanner.Core.Interfaces;

namespace WebPScanner.Core.Services;

/// <summary>
/// Background service that periodically cleans up expired converted image zip files.
/// </summary>
public class ImageZipCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly WebPConversionOptions _options;
    private readonly ILogger<ImageZipCleanupService> _logger;

    public ImageZipCleanupService(
        IServiceProvider serviceProvider,
        IOptions<WebPConversionOptions> options,
        ILogger<ImageZipCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Validate and clamp cleanup interval to prevent tight loops or exceptions
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
            "Image zip cleanup service started. Retention: {Hours} hours, cleanup interval: {Interval} minutes",
            _options.RetentionHours, cleanupIntervalMinutes);

        // Initial delay to allow the app to start up
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredZipsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during image zip cleanup");
            }

            await Task.Delay(TimeSpan.FromMinutes(cleanupIntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("Image zip cleanup service stopped");
    }

    private async Task CleanupExpiredZipsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var webPConversionService = scope.ServiceProvider.GetRequiredService<IWebPConversionService>();

        _logger.LogDebug("Running image zip cleanup...");

        var deletedCount = await webPConversionService.CleanupExpiredZipsAsync(cancellationToken);

        if (deletedCount > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired image zip files", deletedCount);
        }
    }
}
