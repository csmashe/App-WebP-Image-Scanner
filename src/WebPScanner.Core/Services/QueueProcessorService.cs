using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPScanner.Core.Configuration;
using WebPScanner.Core.DTOs;
using WebPScanner.Core.Entities;
using WebPScanner.Core.Enums;
using WebPScanner.Core.Interfaces;
using WebPScanner.Core.Models;

namespace WebPScanner.Core.Services;

/// <summary>
/// Background service that processes the scan job queue.
/// </summary>
public class QueueProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly QueueOptions _options;
    private readonly SecurityOptions _securityOptions;
    private readonly ILogger<QueueProcessorService> _logger;
    private DateTime _lastAgingRecalculation = DateTime.UtcNow;
    private bool _hasResumedInterruptedScans;

    public QueueProcessorService(
        IServiceProvider serviceProvider,
        IOptions<QueueOptions> options,
        IOptions<SecurityOptions> securityOptions,
        ILogger<QueueProcessorService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _securityOptions = securityOptions.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Queue processor started. Checking every {Interval} seconds, max {MaxConcurrent} concurrent scans",
            _options.ProcessingIntervalSeconds, _options.MaxConcurrentScans);

        if (!_hasResumedInterruptedScans)
        {
            await ResumeInterruptedScansAsync(stoppingToken);
            _hasResumedInterruptedScans = true;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessQueueAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing queue");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.ProcessingIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Queue processor stopped");
    }

    /// <summary>
    /// Resumes any scans that were interrupted (left in Processing status).
    /// </summary>
    private async Task ResumeInterruptedScansAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var scanJobRepository = scope.ServiceProvider.GetRequiredService<IScanJobRepository>();
            var checkpointRepository = scope.ServiceProvider.GetRequiredService<ICrawlCheckpointRepository>();

            var processingScans = await scanJobRepository.GetByStatusAsync(ScanStatus.Processing, cancellationToken);
            var scanList = processingScans.ToList();

            if (scanList.Count == 0)
            {
                _logger.LogInformation("No interrupted scans found to resume");
                return;
            }

            _logger.LogInformation("Found {Count} interrupted scan(s) to process", scanList.Count);

            foreach (var scan in scanList)
            {
                var checkpoint = await checkpointRepository.GetByScanJobIdAsync(scan.ScanId, cancellationToken);

                if (checkpoint != null)
                {
                    _logger.LogInformation(
                        "Resuming scan {ScanId} from checkpoint. Visited: {Visited}, Discovered: {Discovered}",
                        scan.ScanId, checkpoint.PagesVisited, checkpoint.PagesDiscovered);

                    // Capture for closure
                    var capturedScan = scan;
                    var capturedCheckpoint = checkpoint;

                    // Fire and forget - the scan will run concurrently with normal queue processing
                    // DequeueAsync checks active scan count to respect MaxConcurrentScans
                    _ = Task.Run(async () =>
                    {
                        using var crawlScope = _serviceProvider.CreateScope();
                        await ExecuteScanAsync(crawlScope.ServiceProvider, capturedScan, capturedCheckpoint, cancellationToken);
                    }, cancellationToken);
                }
                else
                {
                    // No checkpoint - mark as failed
                    _logger.LogWarning(
                        "Scan {ScanId} was interrupted without checkpoint. Marking as failed.",
                        scan.ScanId);

                    scan.Status = ScanStatus.Failed;
                    scan.ErrorMessage = "Scan was interrupted without checkpoint. Please resubmit.";
                    scan.CompletedAt = DateTime.UtcNow;
                    await scanJobRepository.UpdateAsync(scan, cancellationToken);

                    // Notify clients of failure
                    var progressService = scope.ServiceProvider.GetService<IScanProgressService>();
                    if (progressService != null)
                    {
                        await progressService.SendScanFailedAsync(new ScanFailedDto
                        {
                            ScanId = scan.ScanId,
                            ErrorMessage = scan.ErrorMessage,
                            FailedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            _logger.LogInformation("Resumed interrupted scan(s), continuing with normal queue processing");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming interrupted scans");
        }
    }

    private async Task ProcessQueueAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var queueService = scope.ServiceProvider.GetRequiredService<IQueueService>();

        // Periodically recalculate priorities with aging boost
        // Run aging every PriorityAgingBoostSeconds to ensure fairness
        var timeSinceLastAging = DateTime.UtcNow - _lastAgingRecalculation;
        if (timeSinceLastAging.TotalSeconds >= _options.PriorityAgingBoostSeconds)
        {
            try
            {
                var changedScanIds = await queueService.RecalculatePrioritiesWithAgingAsync(stoppingToken);
                _lastAgingRecalculation = DateTime.UtcNow;

                // If positions changed, broadcast updates via SignalR
                if (changedScanIds.Count > 0)
                {
                    var progressService = scope.ServiceProvider.GetService<IScanProgressService>();
                    if (progressService != null)
                    {
                        await progressService.BroadcastQueuePositionsAsync(stoppingToken);
                        _logger.LogDebug(
                            "Broadcast queue position updates after aging recalculation: {Count} positions changed",
                            changedScanIds.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during priority aging recalculation");
            }
        }

        var job = await queueService.DequeueAsync(stoppingToken);

        if (job != null)
        {
            _logger.LogInformation(
                "Processing scan job {ScanId} for URL {Url}",
                job.ScanId, job.TargetUrl);

            var progressService = scope.ServiceProvider.GetService<IScanProgressService>();
            if (progressService != null)
            {
                await progressService.BroadcastQueuePositionsAsync(stoppingToken);
            }

            // Fire and forget the crawl to allow concurrent processing
            _ = Task.Run(async () =>
            {
                using var crawlScope = _serviceProvider.CreateScope();
                await ExecuteScanAsync(crawlScope.ServiceProvider, job, null, stoppingToken);
            }, stoppingToken);
        }
    }

    private async Task ExecuteScanAsync(
        IServiceProvider serviceProvider,
        ScanJob job,
        CrawlCheckpoint? checkpoint,
        CancellationToken cancellationToken)
    {
        var crawlerService = serviceProvider.GetRequiredService<ICrawlerService>();
        var queueService = serviceProvider.GetRequiredService<IQueueService>();
        var scanJobRepository = serviceProvider.GetRequiredService<IScanJobRepository>();
        var checkpointRepository = serviceProvider.GetRequiredService<ICrawlCheckpointRepository>();
        var discoveredImageRepo = serviceProvider.GetRequiredService<IDiscoveredImageRepository>();
        var progressService = serviceProvider.GetService<IScanProgressService>();
        var liveStatsTracker = serviceProvider.GetRequiredService<ILiveScanStatsTracker>();

        // Create a timeout token based on MaxScanDurationMinutes (0 = no timeout)
        CancellationTokenSource? timeoutCts = null;
        CancellationTokenSource? linkedCts = null;
        CancellationToken effectiveToken;

        if (_securityOptions.MaxScanDurationMinutes > 0)
        {
            timeoutCts = new CancellationTokenSource(
                TimeSpan.FromMinutes(_securityOptions.MaxScanDurationMinutes));
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);
            effectiveToken = linkedCts.Token;
        }
        else
        {
            effectiveToken = cancellationToken;
        }

        // Track saved image URLs to avoid duplicates
        // Use Ordinal comparison since URL paths are case-sensitive on most servers
        var savedImageUrls = new HashSet<string>(StringComparer.Ordinal);

        // Get base image count from database when resuming - this is added to the crawler's count
        // since the crawler's internal counter resets on resume
        var baseImageCount = checkpoint != null
            ? await discoveredImageRepo.GetCountByScanJobIdAsync(job.ScanId, effectiveToken)
            : 0;

        liveStatsTracker.StartTracking(job.ScanId);

        try
        {
            _logger.LogInformation("Starting crawl for scan job {ScanId}, getting crawler service...", job.ScanId);

            // Notify clients that scan has started
            if (progressService != null)
            {
                await progressService.SendScanStartedAsync(new ScanStartedDto
                {
                    ScanId = job.ScanId,
                    TargetUrl = job.TargetUrl,
                    StartedAt = job.StartedAt ?? DateTime.UtcNow
                });
            }

            // Create progress callback for real-time updates
            async Task ProgressAction(CrawlProgress p)
            {
                try
                {
                    _logger.LogDebug("Progress callback called: Type={Type}, URL={Url}, Pages={Pages}/{Discovered}", p.Type, p.CurrentUrl, p.PagesScanned, p.PagesDiscovered);

                    // Update live stats tracker for pages
                    if (p.Type == CrawlProgressType.PageCompleted)
                    {
                        liveStatsTracker.UpdatePages(job.ScanId, p.PagesScanned, p.PagesDiscovered);
                    }

                    // Send SignalR updates
                    if (progressService != null)
                    {
	                    // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
	                    switch (p.Type)
                        {
                            case CrawlProgressType.PageStarted:
                            case CrawlProgressType.PageCompleted:
                                await progressService.SendPageProgressAsync(new PageProgressDto
                                {
                                    ScanId = job.ScanId,
                                    CurrentUrl = p.CurrentUrl,
                                    PagesScanned = p.PagesScanned,
                                    PagesDiscovered = p.PagesDiscovered,
                                    ProgressPercent = p.PagesDiscovered > 0
                                        ? (int)((double)p.PagesScanned / p.PagesDiscovered * 100)
                                        : 0
                                });
                                break;

                            case CrawlProgressType.ImageFound:
                                // Track image in live stats for real-time aggregate updates
                                if (p.ImageDetails != null)
                                {
                                    var estimatedWebPSize = (long)(p.ImageDetails.FileSize * 0.7);
                                    liveStatsTracker.AddImage(job.ScanId, p.ImageDetails.MimeType, p.CurrentUrl, p.ImageDetails.FileSize, estimatedWebPSize, 30.0); // 30% savings estimate

                                    // Save image to database immediately for checkpoint persistence
                                    if (savedImageUrls.Add(p.CurrentUrl))
                                    {
                                        try
                                        {
                                            var pageUrl = p.PageUrl ?? job.TargetUrl;
                                            var discoveredImage = new DiscoveredImage
                                            {
                                                Id = Guid.NewGuid(),
                                                ScanJobId = job.ScanId,
                                                ImageUrl = p.CurrentUrl,
                                                MimeType = p.ImageDetails.MimeType,
                                                FileSize = p.ImageDetails.FileSize,
                                                Width = p.ImageDetails.Width > 0 ? p.ImageDetails.Width : null,
                                                Height = p.ImageDetails.Height > 0 ? p.ImageDetails.Height : null,
                                                EstimatedWebPSize = estimatedWebPSize,
                                                PotentialSavingsPercent = 30.0,
                                                PageUrl = pageUrl,
                                                PageUrlsJson = JsonSerializer.Serialize(new[] { pageUrl }),
                                                PageCount = 1,
                                                DiscoveredAt = DateTime.UtcNow
                                            };
                                            await discoveredImageRepo.AddAsync(discoveredImage, effectiveToken);
                                        }
                                        catch (Exception imgEx)
                                        {
                                            _logger.LogWarning(imgEx, "Failed to save discovered image {Url}", p.CurrentUrl);
                                        }
                                    }
                                }

                                await progressService.SendImageFoundAsync(new ImageFoundDto
                                {
                                    ScanId = job.ScanId,
                                    ImageUrl = p.CurrentUrl,
                                    MimeType = p.ImageDetails?.MimeType ?? string.Empty,
                                    Size = p.ImageDetails?.FileSize ?? 0,
                                    PageUrl = p.PageUrl ?? string.Empty,
                                    IsNonWebP = true,
                                    // Add base count from database for resumed scans
                                    TotalNonWebPCount = p.NonWebPImagesFound + baseImageCount
                                });
                                break;

                            case CrawlProgressType.CrawlCompleted:
                            case CrawlProgressType.CrawlFailed:
                                // Handled separately after CrawlAsync returns
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error sending progress update for scan {ScanId}", job.ScanId);
                }
            }

            // Create checkpoint callback for saving progress periodically
            async Task CheckpointAction(CrawlCheckpointData data)
            {
                try
                {
                    var cp = new CrawlCheckpoint
                    {
                        ScanJobId = job.ScanId,
                        VisitedUrlsJson = JsonSerializer.Serialize(data.VisitedUrls),
                        PendingUrlsJson = JsonSerializer.Serialize(data.PendingUrls),
                        PagesVisited = data.PagesVisited,
                        PagesDiscovered = data.PagesDiscovered,
                        NonWebPImagesFound = data.NonWebPImagesFound,
                        CurrentUrl = data.CurrentUrl
                    };
                    await checkpointRepository.SaveCheckpointAsync(cp, effectiveToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save checkpoint for scan {ScanId}", job.ScanId);
                }
            }

            _logger.LogInformation("Calling CrawlAsync for scan job {ScanId}...", job.ScanId);
            var result = await crawlerService.CrawlAsync(job, checkpoint, ProgressAction, CheckpointAction, effectiveToken);
            _logger.LogInformation("CrawlAsync completed for scan job {ScanId}, Success: {Success}", job.ScanId, result.Success);

            // Clean up checkpoint after successful crawl
            if (result.Success)
            {
                try
                {
                    await checkpointRepository.DeleteByScanJobIdAsync(job.ScanId, cancellationToken);
                    _logger.LogDebug("Deleted checkpoint for completed scan {ScanId}", job.ScanId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete checkpoint for scan {ScanId}", job.ScanId);
                }
            }

            // Save any new images that weren't saved during incremental progress
            foreach (var image in result.NonWebPImages)
            {
                // Skip if already saved during incremental progress
                if (savedImageUrls.Contains(image.Url))
                {
                    continue;
                }

                var pageUrls = result.ImageToPagesMap.GetValueOrDefault(image.Url, [job.TargetUrl]);
                var discoveredImage = new DiscoveredImage
                {
                    Id = Guid.NewGuid(),
                    ScanJobId = job.ScanId,
                    ImageUrl = image.Url,
                    MimeType = image.MimeType,
                    FileSize = image.Size,
                    Width = image.Width,
                    Height = image.Height,
                    EstimatedWebPSize = (long)(image.Size * 0.7), // Rough estimate: 30% savings
                    PotentialSavingsPercent = 30.0,
                    DiscoveredAt = DateTime.UtcNow
                };
                discoveredImage.SetPageUrls(pageUrls);
                await discoveredImageRepo.AddAsync(discoveredImage, cancellationToken);
            }

            // Update all images with their full page URL lists from the crawl result
            // This catches images that appear on multiple pages (only first discovery page was saved incrementally)
            if (result.ImageToPagesMap.Count > 0)
            {
                await discoveredImageRepo.UpdatePageUrlsAsync(job.ScanId, result.ImageToPagesMap, cancellationToken);
            }

            // Get total image count from database (includes images saved before any restart)
            var totalNonWebPCount = await discoveredImageRepo.GetCountByScanJobIdAsync(job.ScanId, cancellationToken);

            // Update scan job status with retry logic for concurrency
            const int maxRetries = 3;
            for (var retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    var currentJob = await scanJobRepository.GetByIdAsync(job.ScanId, cancellationToken);
                    if (currentJob != null)
                    {
                        currentJob.PagesScanned = result.PagesScanned;
                        currentJob.PagesDiscovered = result.PagesDiscovered;
                        currentJob.NonWebPImagesFound = totalNonWebPCount;
                        currentJob.Status = result.Success ? ScanStatus.Completed : ScanStatus.Failed;
                        currentJob.CompletedAt = DateTime.UtcNow;
                        currentJob.ErrorMessage = result.ErrorMessage;
                        await scanJobRepository.UpdateAsync(currentJob, cancellationToken);

                        // Record cooldown for the submitter IP
                        if (!string.IsNullOrEmpty(currentJob.SubmitterIp))
                        {
                            queueService.RecordCooldown(currentJob.SubmitterIp);
                        }
                    }
                    break; // Success, exit retry loop
                }
                catch (DbUpdateConcurrencyException ex) when (retry < maxRetries - 1)
                {
                    _logger.LogWarning(ex, "Concurrency conflict updating scan job {ScanId}, retry {Retry}/{MaxRetries}",
                        job.ScanId, retry + 1, maxRetries);
                    await Task.Delay(100 * (retry + 1), cancellationToken); // Brief delay before retry
                }
            }

            _logger.LogInformation(
                "Scan job {ScanId} completed. Pages: {Pages}, Non-WebP Images: {Images}",
                job.ScanId, result.PagesScanned, totalNonWebPCount);

            // Stop tracking live stats BEFORE updating DB to prevent double-counting.
            // When GetCombinedStatsAsync is called, it sums DB + live stats.
            // If we update DB first while live stats still exist, the same scan's data
            // would be counted twice (once from DB, once from live tracker).
            liveStatsTracker.StopTracking(job.ScanId);

            // Update aggregate stats if scan was successful
            if (result.Success)
            {
                try
                {
                    var aggregateStatsService = serviceProvider.GetService<IAggregateStatsService>();
                    if (aggregateStatsService != null)
                    {
                        await aggregateStatsService.UpdateStatsFromCompletedScanAsync(job.ScanId, cancellationToken);
                        _logger.LogInformation("Updated aggregate stats from scan {ScanId}", job.ScanId);

                        // Broadcast updated stats to all connected clients
                        if (progressService != null)
                        {
                            await progressService.BroadcastStatsUpdateAsync(cancellationToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update aggregate stats for scan {ScanId}", job.ScanId);
                }
            }

            // Process WebP conversion and send email for successful scans
            if (result.Success)
            {
                await ProcessPostScanActionsAsync(
                    serviceProvider, job, result.NonWebPImages, cancellationToken);
            }
            else
            {
                // Send failure email
                await SendFailureEmailAsync(serviceProvider, job, result.ErrorMessage, cancellationToken);
            }

            // Notify clients of completion
            if (progressService != null)
            {
                if (result.Success)
                {
                    await progressService.SendScanCompleteAsync(new ScanCompleteDto
                    {
                        ScanId = job.ScanId,
                        TotalPagesScanned = result.PagesScanned,
                        TotalImagesFound = result.DetectedImages.Count,
                        // Use database count for total images (includes those found before any restart)
                        NonWebPImagesCount = totalNonWebPCount,
                        Duration = result.TotalDuration,
                        CompletedAt = DateTime.UtcNow,
                        ReachedPageLimit = result.ReachedPageLimit
                    });
                }
                else
                {
                    await progressService.SendScanFailedAsync(new ScanFailedDto
                    {
                        ScanId = job.ScanId,
                        ErrorMessage = result.ErrorMessage ?? "Unknown error",
                        FailedAt = DateTime.UtcNow
                    });
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Determine if this was a timeout, app shutdown, or user cancellation
            var wasTimeout = timeoutCts?.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested;
            var wasAppShutdown = cancellationToken.IsCancellationRequested && timeoutCts?.IsCancellationRequested != true;

            if (wasAppShutdown)
            {
                // App is shutting down - leave scan in Processing state so it can be resumed on restart
                _logger.LogInformation(
                    "Scan job {ScanId} interrupted by application shutdown. Will resume on restart.",
                    job.ScanId);
                liveStatsTracker.StopTracking(job.ScanId);
                // Do NOT call CompleteJobAsync - leave status as Processing for resume
                return;
            }

            var message = wasTimeout
                ? $"Scan exceeded maximum duration of {_securityOptions.MaxScanDurationMinutes} minutes"
                : "Scan was cancelled";

            _logger.LogInformation("Scan job {ScanId} was {Reason}", job.ScanId,
                wasTimeout ? "timed out" : "cancelled");
            liveStatsTracker.StopTracking(job.ScanId);
            await queueService.CompleteJobAsync(job.ScanId, false, message, CancellationToken.None);

            if (wasTimeout && progressService != null)
            {
                try
                {
                    await progressService.SendScanFailedAsync(new ScanFailedDto
                    {
                        ScanId = job.ScanId,
                        ErrorMessage = message,
                        FailedAt = DateTime.UtcNow
                    });
                }
                catch
                {
                    // Ignore SignalR errors during error handling
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing scan job {ScanId}", job.ScanId);
            liveStatsTracker.StopTracking(job.ScanId);
            await queueService.CompleteJobAsync(job.ScanId, false, ex.Message, CancellationToken.None);

            if (progressService != null)
            {
                try
                {
                    await progressService.SendScanFailedAsync(new ScanFailedDto
                    {
                        ScanId = job.ScanId,
                        ErrorMessage = ex.Message,
                        FailedAt = DateTime.UtcNow
                    });
                }
                catch
                {
                    // Ignore SignalR errors during error handling
                }
            }
        }
        finally
        {
            // Dispose CancellationTokenSources
            timeoutCts?.Dispose();
            linkedCts?.Dispose();
        }
    }

    /// <summary>
    /// Processes post-scan actions: WebP conversion (if requested) and email notification.
    /// </summary>
    private async Task ProcessPostScanActionsAsync(
        IServiceProvider serviceProvider,
        ScanJob job,
        IReadOnlyList<DetectedImage> nonWebPImages,
        CancellationToken cancellationToken)
    {
		ArgumentNullException.ThrowIfNull(serviceProvider);

		ArgumentNullException.ThrowIfNull(job);

		ArgumentNullException.ThrowIfNull(nonWebPImages);

		string? convertedImagesDownloadUrl = null;

        // Convert images to WebP if requested
        if (job.ConvertToWebP && nonWebPImages.Count > 0)
        {
            try
            {
                var webPConversionService = serviceProvider.GetService<IWebPConversionService>();
                if (webPConversionService != null)
                {
                    var discoveredImageRepo = serviceProvider.GetRequiredService<IDiscoveredImageRepository>();
                    var discoveredImages = await discoveredImageRepo.GetByScanJobIdAsync(job.ScanId, cancellationToken);

                    var conversionResult = await webPConversionService.ConvertAndZipImagesAsync(
                        job, discoveredImages, cancellationToken);

                    if (conversionResult is { Success: true, DownloadId: not null })
                    {
                        // Build the download URL (relative - will be made absolute by frontend/email)
                        convertedImagesDownloadUrl = $"/api/images/{conversionResult.DownloadId.Value}";
                        _logger.LogInformation(
                            "WebP conversion completed for scan {ScanId}: {Converted} images, download ID: {DownloadId}",
                            job.ScanId, conversionResult.ImagesConverted, conversionResult.DownloadId.Value);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "WebP conversion failed for scan {ScanId}: {Error}",
                            job.ScanId, conversionResult.ErrorMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during WebP conversion for scan {ScanId}", job.ScanId);
                // Continue with email without download link
            }
        }

        // Send success email with PDF report
        await SendSuccessEmailAsync(serviceProvider, job, convertedImagesDownloadUrl, cancellationToken);
    }

    /// <summary>
    /// Sends the success email with PDF report and optional WebP download link.
    /// </summary>
    private async Task SendSuccessEmailAsync(
        IServiceProvider serviceProvider,
        ScanJob job,
        string? convertedImagesDownloadUrl,
        CancellationToken cancellationToken)
    {
        // Skip email if not provided
        if (string.IsNullOrWhiteSpace(job.Email))
        {
            _logger.LogInformation("Skipping success email for scan {ScanId} - no email provided", job.ScanId);
            return;
        }

        try
        {
            var emailService = serviceProvider.GetRequiredService<IEmailService>();
            var pdfReportService = serviceProvider.GetRequiredService<IPdfReportService>();
            var discoveredImageRepo = serviceProvider.GetRequiredService<IDiscoveredImageRepository>();
            var savingsEstimatorService = serviceProvider.GetRequiredService<ISavingsEstimatorService>();

            // Get discovered images for the report
            var discoveredImages = (await discoveredImageRepo.GetByScanJobIdOrderedBySavingsAsync(job.ScanId, cancellationToken)).ToList();

            // Calculate savings using the estimator service
            var imageEstimates = savingsEstimatorService.CalculateImageSavings(discoveredImages);
            var savingsSummary = savingsEstimatorService.CalculateSavingsSummary(discoveredImages);

            // Use GroupBy to handle potential duplicate ImageUrls, merging page URLs
            var imageToPagesMap = discoveredImages
                .GroupBy(img => img.ImageUrl)
                .ToDictionary(
                    g => g.Key,
                    g => g.SelectMany(img => img.GetPageUrls()).Distinct().ToList());

            var crawlDuration = job is { CompletedAt: not null, StartedAt: not null }
                ? job.CompletedAt.Value - job.StartedAt.Value
                : TimeSpan.Zero;

            var reportData = new PdfReportData
            {
                ScanId = job.ScanId,
                TargetUrl = job.TargetUrl,
                ScanDate = job.CompletedAt ?? job.CreatedAt,
                PagesScanned = job.PagesScanned,
                PagesDiscovered = job.PagesDiscovered,
                CrawlDuration = crawlDuration,
                ReachedPageLimit = job.PagesDiscovered > job.PagesScanned,
                SavingsSummary = savingsSummary,
                ImageEstimates = imageEstimates,
                ImageToPagesMap = imageToPagesMap
            };

            // Generate PDF report
            var pdfBytes = pdfReportService.GenerateReport(reportData);

            // Send email
            var result = await emailService.SendScanReportAsync(
                job.Email,
                reportData,
                pdfBytes,
                convertedImagesDownloadUrl,
                cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Sent scan report email for {ScanId} to {Email}",
                    job.ScanId, job.Email);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send scan report email for {ScanId}: {Error}",
                    job.ScanId, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending success email for scan {ScanId}", job.ScanId);
        }
    }

    /// <summary>
    /// Sends a failure notification email.
    /// </summary>
    private async Task SendFailureEmailAsync(
        IServiceProvider serviceProvider,
        ScanJob job,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        // Skip email if not provided
        if (string.IsNullOrWhiteSpace(job.Email))
        {
            _logger.LogInformation("Skipping failure email for scan {ScanId} - no email provided", job.ScanId);
            return;
        }

        try
        {
            var emailService = serviceProvider.GetRequiredService<IEmailService>();

            var result = await emailService.SendScanFailedNotificationAsync(
                job.Email,
                job.TargetUrl,
                job.ScanId,
                errorMessage ?? "Unknown error",
                cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Sent scan failure email for {ScanId} to {Email}",
                    job.ScanId, job.Email);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send scan failure email for {ScanId}: {Error}",
                    job.ScanId, result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending failure email for scan {ScanId}", job.ScanId);
        }
    }
}
