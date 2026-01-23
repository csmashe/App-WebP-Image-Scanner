using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using WebPScanner.Core.Configuration;
using WebPScanner.Core.Entities;
using WebPScanner.Core.Enums;
using WebPScanner.Core.Interfaces;
using WebPScanner.Core.Models;

namespace WebPScanner.Core.Services;

/// <summary>
/// Implementation of website crawler using PuppeteerSharp.
/// </summary>
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public partial class CrawlerService : ICrawlerService, IDisposable
{
    private readonly CrawlerOptions _options;
    private readonly SecurityOptions _securityOptions;
    private readonly ILogger<CrawlerService> _logger;
    private readonly IImageAnalyzerService _imageAnalyzer;
    private readonly IValidationService _validationService;
    private readonly HttpClient _httpClient;
    private IBrowser? _browser;
    private readonly SemaphoreSlim _browserLock = new(1, 1);
    private bool _disposed;

    // Regex patterns for robots.txt parsing
    [GeneratedRegex(@"^User-agent:\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UserAgentRegex();

    [GeneratedRegex(@"^Disallow:\s*(.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex DisallowRegex();

    [GeneratedRegex(@"^Allow:\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AllowRegex();

    // Patterns for authentication detection
    private static readonly string[] AuthIndicators =
    [
        "login", "signin", "sign-in", "sign_in",
        "authenticate", "auth", "sso",
        "password", "credential"
    ];

    // Known tracking/analytics domains to block
    private static readonly HashSet<string> TrackingDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "google-analytics.com", "googletagmanager.com", "doubleclick.net",
        "facebook.com", "facebook.net", "fbcdn.net",
        "twitter.com", "t.co",
        "linkedin.com", "licdn.com",
        "hotjar.com", "mouseflow.com", "fullstory.com",
        "segment.io", "segment.com", "mixpanel.com",
        "amplitude.com", "heap.io", "heapanalytics.com",
        "intercom.io", "intercomcdn.com",
        "crisp.chat", "tawk.to",
        "ads.google.com", "adservice.google.com",
        "adsserver.com", "adroll.com", "advertising.com"
    };

    public CrawlerService(
        IOptions<CrawlerOptions> options,
        IOptions<SecurityOptions> securityOptions,
        ILogger<CrawlerService> logger,
        IImageAnalyzerService imageAnalyzer,
        IValidationService validationService)
    {
        _options = options.Value;
        _securityOptions = securityOptions.Value;
        _logger = logger;
        _imageAnalyzer = imageAnalyzer;
        _validationService = validationService;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_options.UserAgent);
    }

    public async Task<CrawlResult> CrawlAsync(
        ScanJob scanJob,
        CrawlCheckpoint? checkpoint = null,
        Func<CrawlProgress, Task>? progressCallback = null,
        Func<CrawlCheckpointData, Task>? checkpointCallback = null,
        CancellationToken cancellationToken = default)
    {
        var result = new CrawlResult
        {
            BaseUrl = scanJob.TargetUrl,
            Success = false
        };

        var stopwatch = Stopwatch.StartNew();
        var lastCheckpointPage = 0;

        try
        {
            var baseUri = new Uri(scanJob.TargetUrl);
            // Use case-sensitive comparison for URLs since paths are case-sensitive on most servers
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var toVisit = new Queue<string>();
            var allDiscoveredUrls = new HashSet<string>(StringComparer.Ordinal);
            var imageUrlToPages = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var detectedImages = new ConcurrentDictionary<string, DetectedImage>(StringComparer.Ordinal);

            if (checkpoint != null)
            {
                _logger.LogInformation("Resuming crawl from checkpoint for scan {ScanId}. Visited: {Visited}, Pending: {Pending}",
                    scanJob.ScanId, checkpoint.PagesVisited, checkpoint.PagesDiscovered - checkpoint.PagesVisited);

                var visitedUrls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(checkpoint.VisitedUrlsJson) ??
                                  [];
                var pendingUrls = System.Text.Json.JsonSerializer.Deserialize<List<string>>(checkpoint.PendingUrlsJson) ??
                                  [];

                foreach (var url in visitedUrls)
                {
                    visited.Add(url);
                    allDiscoveredUrls.Add(url);
                }

                foreach (var url in pendingUrls)
                {
                    toVisit.Enqueue(url);
                    allDiscoveredUrls.Add(url);
                }

                lastCheckpointPage = visited.Count;
            }

            var robotsRules = _options.RespectRobotsTxt
                ? await ParseRobotsTxtAsync(baseUri, cancellationToken)
                : new RobotsRules();

            if (checkpoint == null)
            {
                var normalizedStartUrl = NormalizeUrl(scanJob.TargetUrl);
                toVisit.Enqueue(normalizedStartUrl);
                allDiscoveredUrls.Add(normalizedStartUrl);
            }

            _logger.LogInformation("Ensuring browser is available for crawl...");
            await EnsureBrowserAsync(cancellationToken);
            _logger.LogInformation("Browser ready, starting to crawl pages. toVisit.Count={Count}, targetUrl={Url}, MaxPagesPerScan={MaxPages}, isResume={IsResume}",
                toVisit.Count, scanJob.TargetUrl, _options.MaxPagesPerScan, checkpoint != null);

            while (toVisit.Count > 0 && visited.Count < _options.MaxPagesPerScan)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentMemoryMb = GC.GetTotalMemory(forceFullCollection: false) / (1024 * 1024);
                if (_securityOptions.MaxMemoryPerScanMb > 0 && currentMemoryMb > _securityOptions.MaxMemoryPerScanMb)
                {
                    _logger.LogWarning(
                        "Memory limit exceeded for scan {ScanId}: {CurrentMb}MB > {LimitMb}MB. Stopping crawl.",
                        scanJob.ScanId, currentMemoryMb, _securityOptions.MaxMemoryPerScanMb);
                    result.ErrorMessage = $"Memory limit exceeded ({currentMemoryMb}MB > {_securityOptions.MaxMemoryPerScanMb}MB limit)";
                    break;
                }

                var currentUrl = toVisit.Dequeue();
                _logger.LogInformation("Processing URL: {Url} (visited: {Visited}, toVisit: {ToVisit})",
                    currentUrl, visited.Count, toVisit.Count);

                if (visited.Contains(currentUrl))
                {
                    _logger.LogDebug("URL already visited, skipping");
                    continue;
                }

                if (_options.RespectRobotsTxt && !IsUrlAllowed(currentUrl, robotsRules))
                {
                    _logger.LogDebug("Skipping {Url} due to robots.txt", currentUrl);
                    continue;
                }

                visited.Add(currentUrl);

                if (progressCallback != null)
                {
                    await progressCallback(new CrawlProgress
                    {
                        Type = CrawlProgressType.PageStarted,
                        CurrentUrl = currentUrl,
                        PagesScanned = visited.Count,
                        PagesDiscovered = allDiscoveredUrls.Count,
                        NonWebPImagesFound = detectedImages.Count(kv => _imageAnalyzer.IsNonWebPRasterImage(kv.Value.MimeType))
                    });
                }

                _logger.LogInformation("Crawling page: {Url}", currentUrl);
                var pageCrawlResult = await CrawlPageWithRetryAsync(currentUrl, baseUri, cancellationToken);
                _logger.LogInformation("Page crawl result for {Url}: Success={Success}, Status={Status}",
                    currentUrl, pageCrawlResult.Success, pageCrawlResult.StatusCode);

                // === DETAILED PAGE LOGGING FOR DEBUGGING ===
                if (pageCrawlResult.Success)
                {
                    var pageImages = pageCrawlResult.DetectedImages;
                    var pageNonWebPImages = pageImages.Where(img => _imageAnalyzer.IsNonWebPRasterImage(img.MimeType)).ToList();

                    _logger.LogInformation("Page scan completed: {Url} | Load time: {Duration}ms | Images: {Total} total, {NonWebP} non-WebP",
                        currentUrl, pageCrawlResult.CrawlDuration.TotalMilliseconds.ToString("F0"), pageImages.Count, pageNonWebPImages.Count);

                    if (pageImages.Count > 0)
                    {
                        _logger.LogDebug("=== PAGE SCAN DETAILS: {Url} ===", currentUrl);
                        foreach (var img in pageImages)
                        {
                            var isNonWebP = _imageAnalyzer.IsNonWebPRasterImage(img.MimeType);
                            // Safely parse URL to extract filename, falling back to raw URL if malformed
                            var imageName = Uri.TryCreate(img.Url, UriKind.Absolute, out var parsedUri)
                                ? Path.GetFileName(parsedUri.AbsolutePath)
                                : img.Url;
                            if (string.IsNullOrEmpty(imageName))
                                imageName = img.Url;
                            _logger.LogDebug("  [{Status}] {ImageName} | MIME: {MimeType} | Size: {Size} bytes | URL: {Url}",
                                isNonWebP ? "NON-WEBP" : "OK",
                                imageName,
                                img.MimeType,
                                img.Size,
                                img.Url);
                        }
                        _logger.LogDebug("=== END PAGE SCAN DETAILS ===");
                    }
                }

                if (progressCallback != null)
                {
                    await progressCallback(new CrawlProgress
                    {
                        Type = CrawlProgressType.PageCompleted,
                        CurrentUrl = currentUrl,
                        PagesScanned = visited.Count,
                        PagesDiscovered = allDiscoveredUrls.Count,
                        NonWebPImagesFound = detectedImages.Count(kv => _imageAnalyzer.IsNonWebPRasterImage(kv.Value.MimeType))
                    });
                }

                if (!pageCrawlResult.Success)
                {
                    _logger.LogWarning("Failed to crawl {Url}: {Error}", currentUrl, pageCrawlResult.ErrorMessage);
                    continue;
                }

                if (pageCrawlResult.IsAuthenticationPage)
                {
                    _logger.LogDebug("Skipping auth page: {Url}", currentUrl);
                    continue;
                }

                // Add discovered images and track all pages where each image appears
                foreach (var image in pageCrawlResult.DetectedImages)
                {
                    var isNewImage = detectedImages.TryAdd(image.Url, image);

                    // Track this page for the image (whether new or existing)
                    if (!imageUrlToPages.TryGetValue(image.Url, out var pages))
                    {
                        pages = [];
                        imageUrlToPages[image.Url] = pages;
                    }
                    if (!pages.Contains(currentUrl, StringComparer.Ordinal))
                    {
                        pages.Add(currentUrl);
                    }

                    // Only send progress callback for newly discovered non-WebP images
                    if (isNewImage && _imageAnalyzer.IsNonWebPRasterImage(image.MimeType) && progressCallback != null)
                    {
                        await progressCallback(new CrawlProgress
                        {
                            Type = CrawlProgressType.ImageFound,
                            CurrentUrl = image.Url,
                            PageUrl = currentUrl, // The page where the image was discovered
                            PagesScanned = visited.Count,
                            PagesDiscovered = allDiscoveredUrls.Count,
                            NonWebPImagesFound = detectedImages.Count(kv => _imageAnalyzer.IsNonWebPRasterImage(kv.Value.MimeType)),
                            ImageDetails = new CrawlProgressImageDetails
                            {
                                MimeType = image.MimeType,
                                FileSize = image.Size,
                                Width = image.Width ?? 0,
                                Height = image.Height ?? 0
                            }
                        });
                    }
                }

                // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                foreach (var discoveredUrl in pageCrawlResult.DiscoveredUrls)
                {
                    var normalized = NormalizeUrl(discoveredUrl);
                    if (!string.IsNullOrEmpty(normalized) &&
                        !visited.Contains(normalized) &&
                        allDiscoveredUrls.Add(normalized))
                    {
                        toVisit.Enqueue(normalized);
                    }
                }

                // Save checkpoint periodically if enabled
                if (_options.EnableCheckpointing &&
                    checkpointCallback != null &&
                    visited.Count - lastCheckpointPage >= _options.CheckpointIntervalPages)
                {
                    try
                    {
                        var nonWebPCount = detectedImages.Count(kv => _imageAnalyzer.IsNonWebPRasterImage(kv.Value.MimeType));
                        await checkpointCallback(new CrawlCheckpointData
                        {
                            VisitedUrls = visited.ToList(),
                            PendingUrls = toVisit.ToArray(),
                            PagesVisited = visited.Count,
                            PagesDiscovered = allDiscoveredUrls.Count,
                            NonWebPImagesFound = nonWebPCount,
                            CurrentUrl = currentUrl
                        });
                        lastCheckpointPage = visited.Count;
                        _logger.LogDebug("Saved checkpoint at page {PageCount} for scan {ScanId} (NonWebP: {NonWebP})",
                            visited.Count, scanJob.ScanId, nonWebPCount);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to save checkpoint for scan {ScanId}", scanJob.ScanId);
                    }
                }

                if (toVisit.Count > 0)
                {
                    await Task.Delay(_options.DelayBetweenPagesMs, cancellationToken);
                }
            }

            // Only mark as success if no error occurred (e.g., memory limit)
            result.Success = string.IsNullOrEmpty(result.ErrorMessage);
            result.PagesScanned = visited.Count;
            result.PagesDiscovered = allDiscoveredUrls.Count;
            result.DetectedImages = detectedImages.Values.ToList();
            result.NonWebPImages = detectedImages.Values
                .Where(img => _imageAnalyzer.IsNonWebPRasterImage(img.MimeType))
                .ToList();
            result.ImageToPagesMap = imageUrlToPages;
            result.ReachedPageLimit = toVisit.Count > 0;

            if (progressCallback != null)
            {
                await progressCallback(new CrawlProgress
                {
                    Type = CrawlProgressType.CrawlCompleted,
                    CurrentUrl = scanJob.TargetUrl,
                    PagesScanned = result.PagesScanned,
                    PagesDiscovered = result.PagesDiscovered,
                    NonWebPImagesFound = result.NonWebPImages.Count
                });
            }
        }
        catch (OperationCanceledException)
        {
            result.ErrorMessage = "Crawl was cancelled.";
            _logger.LogInformation("Crawl cancelled for {Url}", scanJob.TargetUrl);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Crawl failed: {ex.Message}";
            _logger.LogError(ex, "Crawl failed for {Url}", scanJob.TargetUrl);

            if (progressCallback != null)
            {
                await progressCallback(new CrawlProgress
                {
                    Type = CrawlProgressType.CrawlFailed,
                    CurrentUrl = scanJob.TargetUrl,
                    PagesScanned = result.PagesScanned,
                    PagesDiscovered = result.PagesDiscovered,
                    NonWebPImagesFound = result.NonWebPImages.Count
                });
            }
        }
        finally
        {
            stopwatch.Stop();
            result.TotalDuration = stopwatch.Elapsed;
        }

        return result;
    }

    private async Task<PageCrawlResult> CrawlPageWithRetryAsync(
        string url,
        Uri baseUri,
        CancellationToken cancellationToken)
    {
        var lastException = default(Exception);

        for (var attempt = 0; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                if (attempt <= 0)
                {
                    return await CrawlPageAsync(url, baseUri, cancellationToken);
                }

                // Exponential backoff: 1s, 2s, 4s
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                _logger.LogDebug("Retry {Attempt} for {Url} after {Delay}s", attempt, url, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);

                return await CrawlPageAsync(url, baseUri, cancellationToken);
            }
            catch (Exception ex)
            {
                // Propagate cancellation immediately - don't treat it as a retryable failure
                if (ex is OperationCanceledException || cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                lastException = ex;
                _logger.LogWarning("Attempt {Attempt} failed for {Url}: {Error}", attempt + 1, url, ex.Message);

                // If this was the final attempt, exit the loop and return failure
                if (attempt >= _options.MaxRetries)
                {
                    break;
                }
            }
        }

        return new PageCrawlResult
        {
            Url = url,
            Success = false,
            ErrorMessage = lastException?.Message ?? "Unknown error after all retries"
        };
    }

    private async Task<PageCrawlResult> CrawlPageAsync(
        string url,
        Uri baseUri,
        CancellationToken cancellationToken)
    {
        var result = new PageCrawlResult
        {
            Url = url,
            Success = false
        };

        var stopwatch = Stopwatch.StartNew();
        IPage? page = null;
        ICdpImageListenerHandle? imageListenerHandle = null;
        ICDPSession? cdpSizeTracker = null;
        // ReSharper disable AccessToModifiedClosure
        // These variables are captured in lambdas and modified with Interlocked operations (thread-safe)
        var requestCount = 0;
        long totalRequestSize = 0;
        // Using int with Interlocked for thread-safe boolean flag (0 = false, 1 = true)
        var pageSizeLimitExceeded = 0;
        // ReSharper restore AccessToModifiedClosure

        try
        {
            await EnsureBrowserAsync(cancellationToken);

            page = await _browser!.NewPageAsync();
            await page.SetUserAgentAsync(_options.UserAgent);

            // Enable request interception for domain restriction and resource limits
            await page.SetRequestInterceptionAsync(true);
            page.Request += async (_, e) =>
            {
                try
                {
                    var request = e.Request;
                    var requestUrl = request.Url;

                    // Check if page size limit was exceeded
                    // ReSharper disable once AccessToModifiedClosure
                    if (Interlocked.CompareExchange(ref pageSizeLimitExceeded, 0, 0) == 1)
                    {
                        await request.AbortAsync();
                        return;
                    }

                    if (Interlocked.Increment(ref requestCount) > _options.MaxRequestsPerPage)
                    {
                        _logger.LogWarning("Max requests per page exceeded for {Url}", url);
                        await request.AbortAsync();
                        return;
                    }

                    if (_options.RestrictToTargetDomain && !IsAllowedDomain(requestUrl, baseUri))
                    {
                        _logger.LogDebug("Blocked request to external domain: {RequestUrl}", requestUrl);
                        await request.AbortAsync();
                        return;
                    }

                    if (_options.BlockTrackingDomains && IsTrackingDomain(requestUrl))
                    {
                        _logger.LogDebug("Blocked tracking request: {RequestUrl}", requestUrl);
                        await request.AbortAsync();
                        return;
                    }

                    await request.ContinueAsync();
                }
                catch
                {
                    // Request may already be handled
                }
            };

            // Track actual response sizes using CDP to handle chunked/streaming responses
            // Content-Length header isn't reliable for chunked transfer encoding
            cdpSizeTracker = await page.CreateCDPSessionAsync();
            await cdpSizeTracker.SendAsync("Network.enable");

            cdpSizeTracker.MessageReceived += (_, e) =>
            {
                try
                {
                    // Track actual bytes as they're received (works for chunked responses)
                    if (e.MessageID != "Network.dataReceived")
                    {
                        return;
                    }

                    if (!e.MessageData.TryGetProperty("dataLength", out var dataLengthProp))
                    {
                        return;
                    }

                    var dataLength = dataLengthProp.GetInt64();
                    var newTotal = Interlocked.Add(ref totalRequestSize, dataLength);
                    if (newTotal > _options.MaxRequestSizeBytes &&
                        Interlocked.Exchange(ref pageSizeLimitExceeded, 1) == 0)
                    {
                        _logger.LogWarning(
                            "Max request size exceeded for {Url}: {Size} bytes (limit: {Limit} bytes)",
                            url, newTotal, _options.MaxRequestSizeBytes);
                    }
                }
                catch
                {
                    // Ignore response tracking errors
                }
            };

            // Track network requests for images using CDP
            var detectedImages = new List<DetectedImage>();
            imageListenerHandle = await _imageAnalyzer.AttachImageListenersAsync(page, image =>
            {
                lock (detectedImages)
                {
                    detectedImages.Add(image);
                }
            });

            // Defense-in-depth: Re-validate SSRF at crawl time to prevent DNS rebinding attacks
            // The hostname was validated at submission time, but DNS could have been rebound since then
            var targetUri = new Uri(url);
            var ssrfCheck = await _validationService.ValidateHostSsrfAsync(targetUri.Host, cancellationToken);
            if (!ssrfCheck.IsValid)
            {
                _logger.LogWarning("SSRF check failed at crawl time for {Url}: {Error}. Possible DNS rebinding attack.",
                    url, string.Join(", ", ssrfCheck.Errors));
                result.ErrorMessage = "URL failed security validation at crawl time";
                return result;
            }

            var stepTimer = Stopwatch.StartNew();

            var navigationResponse = await page.GoToAsync(url, new NavigationOptions
            {
                Timeout = _options.PageTimeoutSeconds * 1000,
                WaitUntil = [WaitUntilNavigation.Load]
            });

            var navigationTime = stepTimer.ElapsedMilliseconds;
            stepTimer.Restart();

            // Scroll the page to trigger lazy-loaded images
            if (_options.ScrollToTriggerLazyImages)
            {
                await ScrollPageToTriggerLazyImagesAsync(page, cancellationToken);
            }

            var scrollTime = stepTimer.ElapsedMilliseconds;
            stepTimer.Restart();

            // Wait for network to be idle using the configured idle timeout (SPA support)
            // This allows SPAs time to finish their API calls after initial load
            // MaxNetworkIdleWaitMs caps total wait time for pages with persistent connections (chat widgets, etc.)
            try
            {
                await page.WaitForNetworkIdleAsync(new WaitForNetworkIdleOptions
                {
                    IdleTime = _options.NetworkIdleTimeoutMs,
                    Timeout = _options.MaxNetworkIdleWaitMs
                });
            }
            catch (TimeoutException)
            {
                // Network didn't become idle within timeout - continue anyway
                // This is expected for pages with persistent connections (websockets, SSE, etc.)
                _logger.LogInformation("Network idle timeout after {Timeout}ms for {Url}, continuing with current state",
                    _options.MaxNetworkIdleWaitMs, url);
            }

            var networkIdleTime = stepTimer.ElapsedMilliseconds;
            stepTimer.Restart();

            // If there are images still loading after network idle, give them a grace period to finish
            // This catches late-loading images without waiting the full idle time for every page
            var gracePeriodTime = 0L;
            if (imageListenerHandle.HasPendingImages && _options.PendingImageGracePeriodMs > 0)
            {
                _logger.LogDebug("Waiting for pending images on {Url}", url);
                await imageListenerHandle.WaitForPendingImagesAsync(
                    TimeSpan.FromMilliseconds(_options.PendingImageGracePeriodMs),
                    cancellationToken);
                gracePeriodTime = stepTimer.ElapsedMilliseconds;
            }

            _logger.LogInformation(
                "Page timing for {Url}: Navigate={NavigateMs}ms, Scroll={ScrollMs}ms, NetworkIdle={IdleMs}ms, GracePeriod={GraceMs}ms",
                url, navigationTime, scrollTime, networkIdleTime, gracePeriodTime);

            if (navigationResponse == null)
            {
                result.ErrorMessage = "No response received";
                return result;
            }

            // Post-navigation SSRF check: verify the actual connected IP is not private
            // This catches any DNS rebinding that occurred between pre-navigation check and connection
            var remoteAddress = navigationResponse.RemoteAddress;
            if (remoteAddress != null && !string.IsNullOrEmpty(remoteAddress.IP))
            {
                if (_validationService.IsPrivateOrReservedIp(remoteAddress.IP))
                {
                    _logger.LogWarning(
                        "DNS rebinding attack detected for {Url}: connected to private IP {RemoteIp}. Aborting.",
                        url, remoteAddress.IP);
                    result.ErrorMessage = "Connection blocked: DNS rebinding to private IP detected";
                    return result;
                }
            }

            result.StatusCode = (int)navigationResponse.Status;

            var pageUrl = page.Url.ToLowerInvariant();
            var pageContent = await page.GetContentAsync();
            result.IsAuthenticationPage = IsAuthenticationPage(pageUrl, pageContent) || result.StatusCode == 401 || result.StatusCode == 403;

            result.DiscoveredUrls = await ExtractSameDomainLinksAsync(page, baseUri);
            result.DetectedImages = detectedImages;

            if (Interlocked.CompareExchange(ref pageSizeLimitExceeded, 0, 0) == 1)
            {
                result.Success = false;
                result.ErrorMessage = $"Page size limit exceeded ({totalRequestSize} bytes)";
            }
            else
            {
                result.Success = navigationResponse.Ok;
            }
        }
        catch (NavigationException ex)
        {
            result.ErrorMessage = $"Navigation error: {ex.Message}";
        }
        catch (TimeoutException)
        {
            result.ErrorMessage = "Page load timeout";
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            stopwatch.Stop();
            result.CrawlDuration = stopwatch.Elapsed;

            if (cdpSizeTracker != null)
            {
                try
                {
                    await cdpSizeTracker.DetachAsync();
                }
                catch
                {
                    // Ignore detach errors
                }
            }

            if (imageListenerHandle != null)
            {
                try
                {
                    await imageListenerHandle.DisposeAsync();
                }
                catch
                {
                    // Ignore dispose errors
                }
            }

            if (page != null)
            {
                try
                {
                    await page.CloseAsync();
                }
                catch
                {
                    // Ignore close errors
                }
            }
        }

        return result;
    }

    private async Task<List<string>> ExtractSameDomainLinksAsync(IPage page, Uri baseUri)
    {
        var links = new List<string>();

        try
        {
            var hrefs = await page.EvaluateFunctionAsync<string[]>("""
                                                                   () => {
                                                                                   const anchors = document.querySelectorAll('a[href]');
                                                                                   return Array.from(anchors).map(a => a.href).filter(h => h && h.startsWith('http'));
                                                                               }
                                                                   """);

            foreach (var href in hrefs ?? [])
            {
                if (!Uri.TryCreate(href, UriKind.Absolute, out var uri))
                {
                    continue;
                }

                // Only include same-domain links
                if (!uri.Host.Equals(baseUri.Host, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var normalized = NormalizeUrl(href);
                if (!string.IsNullOrEmpty(normalized))
                {
                    links.Add(normalized);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to extract links: {Error}", ex.Message);
        }

        return links.Distinct(StringComparer.Ordinal).ToList();
    }

    private static bool IsAuthenticationPage(string url, string content)
    {
        // Check URL for auth indicators
        if (AuthIndicators.Any(indicator => url.Contains(indicator, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // Check for common login form elements
        var lowerContent = content.ToLowerInvariant();
        var hasPasswordField = lowerContent.Contains("type=\"password\"") ||
                               lowerContent.Contains("type='password'");
        var hasLoginForm = lowerContent.Contains("login") ||
                           lowerContent.Contains("sign in") ||
                           lowerContent.Contains("signin");

        return hasPasswordField && hasLoginForm;
    }

    private static string NormalizeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return string.Empty;

        // Only allow http/https
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return string.Empty;

        // Normalize scheme and host to lowercase (per RFC 3986, these are case-insensitive)
        // but preserve path and query case (these are case-sensitive on most servers)
        var builder = new UriBuilder(uri)
        {
            Scheme = uri.Scheme.ToLowerInvariant(),
            Host = uri.Host.ToLowerInvariant(),
            Fragment = string.Empty
        };

        // Normalize path: remove trailing slash except for root
        var path = builder.Path;
        if (path.Length > 1 && path.EndsWith('/'))
        {
            builder.Path = path.TrimEnd('/');
        }

        // Normalize port (remove default ports)
        if ((uri.Scheme == Uri.UriSchemeHttp && uri.Port == 80) ||
            (uri.Scheme == Uri.UriSchemeHttps && uri.Port == 443))
        {
            builder.Port = -1;
        }

        return builder.Uri.ToString();
    }

    private async Task<RobotsRules> ParseRobotsTxtAsync(Uri baseUri, CancellationToken cancellationToken)
    {
        var rules = new RobotsRules();

        try
        {
            var robotsUrl = new Uri(baseUri, "/robots.txt");
            var response = await _httpClient.GetAsync(robotsUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return rules; // No robots.txt or not accessible - allow all
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var lines = content.Split('\n', StringSplitOptions.TrimEntries);

            var isRelevantSection = false;
            var groupStarted = false;

            foreach (var line in lines)
            {
                // Blank lines reset the group - start fresh for next User-agent block
                if (string.IsNullOrEmpty(line))
                {
                    groupStarted = false;
                    isRelevantSection = false;
                    continue;
                }

                // Strip inline comments and trim (RFC 9309 allows comments after #)
                var sanitizedLine = line.Split('#', 2)[0].Trim();
                if (string.IsNullOrEmpty(sanitizedLine))
                    continue;

                var userAgentMatch = UserAgentRegex().Match(sanitizedLine);
                if (userAgentMatch.Success)
                {
                    // Starting a new group - reset isRelevantSection
                    if (!groupStarted)
                    {
                        groupStarted = true;
                        isRelevantSection = false;
                    }

                    // OR multiple User-agent lines within the same group
                    var currentUserAgent = userAgentMatch.Groups[1].Value.Trim();
                    isRelevantSection |= currentUserAgent == "*" ||
                                         _options.UserAgent.Contains(currentUserAgent, StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (!isRelevantSection)
                    continue;

                var disallowMatch = DisallowRegex().Match(sanitizedLine);
                if (disallowMatch.Success)
                {
                    var path = disallowMatch.Groups[1].Value.Trim();
                    if (!string.IsNullOrEmpty(path))
                    {
                        rules.DisallowedPaths.Add(path);
                    }
                    continue;
                }

                var allowMatch = AllowRegex().Match(sanitizedLine);
                if (!allowMatch.Success)
                {
                    continue;
                }

                var pathToAdd = allowMatch.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(pathToAdd))
                {
                    rules.AllowedPaths.Add(pathToAdd);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to parse robots.txt: {Error}", ex.Message);
        }

        return rules;
    }

    private static bool IsUrlAllowed(string url, RobotsRules rules)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var path = uri.PathAndQuery;

        // Per RFC 9309, use longest-match precedence:
        // Find the longest matching Allow and Disallow patterns, then compare
        var longestAllowMatch = 0;
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var allowedPath in rules.AllowedPaths)
        {
            var matchLength = GetMatchLength(path, allowedPath);
            if (matchLength > longestAllowMatch)
                longestAllowMatch = matchLength;
        }

        var longestDisallowMatch = 0;
        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var disallowedPath in rules.DisallowedPaths)
        {
            var matchLength = GetMatchLength(path, disallowedPath);
            if (matchLength > longestDisallowMatch)
                longestDisallowMatch = matchLength;
        }

        // If no rules match, allow by default
        if (longestAllowMatch == 0 && longestDisallowMatch == 0)
            return true;

        // The longer match takes precedence; ties go to Allow per RFC 9309
        return longestAllowMatch >= longestDisallowMatch;
    }

    /// <summary>
    /// Returns the match length if the path matches the pattern, or 0 if no match.
    /// Per RFC 9309, the match length is the length of the pattern prefix that matches.
    /// Supports wildcards (*) anywhere in the pattern and $ as end-of-path anchor.
    /// </summary>
    private static int GetMatchLength(string path, string pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return 0;

        // Check for terminal $ (exact match anchor)
        var hasEndAnchor = pattern.EndsWith('$');
        var patternToMatch = hasEndAnchor ? pattern[..^1] : pattern;

        // If pattern contains wildcards, use regex matching
        if (patternToMatch.Contains('*'))
        {
            // Escape regex special characters except *, then replace * with .*
            var regexPattern = Regex.Escape(patternToMatch).Replace("\\*", ".*");

            // Add anchors: always start from beginning, optionally anchor to end
            regexPattern = "^" + regexPattern + (hasEndAnchor ? "$" : "");

            try
            {
                var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
                if (regex.IsMatch(path))
                {
                    // Return pattern length (excluding $ if present) per RFC 9309
                    return patternToMatch.Length;
                }
            }
            catch (ArgumentException)
            {
                // Invalid regex pattern, fall back to no match
                return 0;
            }

            return 0;
        }

        // No wildcards - use simple string matching (more efficient)
        // Case-insensitive matching to handle Windows/IIS servers
        if (hasEndAnchor)
        {
            // Exact match required
            return path.Equals(patternToMatch, StringComparison.OrdinalIgnoreCase) ? patternToMatch.Length : 0;
        }

        // Prefix match (default behavior per RFC 9309)
        return path.StartsWith(patternToMatch, StringComparison.OrdinalIgnoreCase) ? patternToMatch.Length : 0;
    }

    /// <summary>
    /// Checks if a request URL is allowed based on domain restrictions.
    /// </summary>
    private bool IsAllowedDomain(string requestUrl, Uri baseUri)
    {
        if (!Uri.TryCreate(requestUrl, UriKind.Absolute, out var requestUri))
            return true; // Allow relative URLs

        var requestHost = requestUri.Host.ToLowerInvariant();
        var baseHost = baseUri.Host.ToLowerInvariant();

        // Allow same domain
        if (requestHost.Equals(baseHost, StringComparison.OrdinalIgnoreCase))
            return true;

        // Allow subdomains (e.g., cdn.example.com for example.com)
        if (requestHost.EndsWith($".{baseHost}", StringComparison.OrdinalIgnoreCase))
            return true;

        // Check allowed external domains (CDNs, etc.)
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var allowedDomain in _options.AllowedExternalDomains)
        {
            if (string.IsNullOrEmpty(allowedDomain))
                continue;

            var domain = allowedDomain.ToLowerInvariant();
            if (requestHost.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
                requestHost.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        // Allow common CDN domains by default
        var commonCdnDomains = new[]
        {
            "cloudflare.com", "cloudflareinsights.com",
            "googleapis.com", "gstatic.com",
            "jsdelivr.net", "unpkg.com",
            "cdnjs.cloudflare.com",
            "bootstrapcdn.com",
            "fontawesome.com",
            "fonts.googleapis.com", "fonts.gstatic.com"
        };

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var cdnDomain in commonCdnDomains)
        {
            if (requestHost.Equals(cdnDomain, StringComparison.OrdinalIgnoreCase) ||
                requestHost.EndsWith($".{cdnDomain}", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a URL is for a known tracking/analytics domain.
    /// </summary>
    private static bool IsTrackingDomain(string requestUrl)
    {
        if (!Uri.TryCreate(requestUrl, UriKind.Absolute, out var requestUri))
            return false;

        var requestHost = requestUri.Host.ToLowerInvariant();

        // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
        foreach (var trackingDomain in TrackingDomains)
        {
            if (requestHost.Equals(trackingDomain, StringComparison.OrdinalIgnoreCase) ||
                requestHost.EndsWith($".{trackingDomain}", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private async Task EnsureBrowserAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("EnsureBrowserAsync called, checking if browser is connected...");
        if (_browser is { IsConnected: true })
        {
            _logger.LogDebug("Browser already connected");
            return;
        }

        _logger.LogDebug("Acquiring browser lock...");
        await _browserLock.WaitAsync(cancellationToken);
        _logger.LogDebug("Browser lock acquired");
        try
        {
            if (_browser is { IsConnected: true })
            {
                _logger.LogDebug("Browser connected after lock");
                return;
            }

            // Download browser if needed
            if (string.IsNullOrEmpty(_options.ChromiumPath))
            {
                _logger.LogInformation("ChromiumPath not set, downloading browser...");
                var browserFetcher = new BrowserFetcher();
                await browserFetcher.DownloadAsync();
                _logger.LogInformation("Browser downloaded");
            }
            else
            {
                _logger.LogInformation("Using ChromiumPath: {Path}", _options.ChromiumPath);
            }

            var args = new List<string>
            {
                "--disable-dev-shm-usage",
                "--disable-gpu",
                "--disable-software-rasterizer",
                "--disable-extensions",
                "--disable-sync",
                "--disable-translate",
                "--disable-default-apps",
                "--disable-background-networking",
                "--disable-background-timer-throttling",
                "--disable-backgrounding-occluded-windows",
                "--disable-breakpad",
                "--disable-component-extensions-with-background-pages",
                "--disable-component-update",
                "--disable-domain-reliability",
                "--disable-features=TranslateUI",
                "--disable-hang-monitor",
                "--disable-ipc-flooding-protection",
                "--disable-popup-blocking",
                "--disable-prompt-on-repost",
                "--disable-renderer-backgrounding",
                "--metrics-recording-only",
                "--mute-audio",
                "--no-first-run"
            };

            // Configure sandbox mode based on settings
            if (!_options.EnableSandbox)
            {
                args.Add("--no-sandbox");
                args.Add("--disable-setuid-sandbox");
            }

            var launchOptions = new LaunchOptions
            {
                Headless = true,
                Args = args.ToArray()
            };

            if (!string.IsNullOrEmpty(_options.ChromiumPath))
            {
                launchOptions.ExecutablePath = _options.ChromiumPath;
            }

            _logger.LogInformation("Launching browser with ExecutablePath: {Path}", launchOptions.ExecutablePath ?? "default");
            _browser = await Puppeteer.LaunchAsync(launchOptions);
            _logger.LogInformation("Browser launched successfully, IsConnected: {Connected}", _browser.IsConnected);
        }
        finally
        {
            _browserLock.Release();
            _logger.LogDebug("Browser lock released");
        }
    }

    /// <summary>
    /// Scrolls the page using discrete steps and simulates mouse movement to trigger lazy-loaded images.
    /// Smooth scroll doesn't animate in headless Chrome, so we use discrete scroll steps instead.
    /// Mouse movement helps trigger hover-based lazy loaders that some sites use.
    /// Step count is dynamic based on page height to ensure proper coverage on long pages.
    /// </summary>
    private async Task ScrollPageToTriggerLazyImagesAsync(IPage page, CancellationToken cancellationToken)
    {
        try
        {
            // Get page dimensions
            var dimensions = await page.EvaluateFunctionAsync<PageDimensions>("""
                () => ({
                    scrollHeight: document.body.scrollHeight,
                    viewportWidth: window.innerWidth,
                    viewportHeight: window.innerHeight
                })
                """);

            if (dimensions.ScrollHeight <= 0)
                return;

            // Dynamic scroll steps based on page height:
            // - Step every ~400px of content (half a typical viewport)
            // - Minimum 8 steps to ensure basic coverage
            // - Maximum 30 steps to cap scroll time on extremely long pages
            const int pixelsPerStep = 400;
            const int minSteps = 8;
            const int maxSteps = 30;
            var scrollDelayMs = _options.ScrollStepDelayMs;

            var calculatedSteps = dimensions.ScrollHeight / pixelsPerStep;
            var scrollSteps = Math.Clamp(calculatedSteps, minSteps, maxSteps);
            var stepSize = dimensions.ScrollHeight / scrollSteps;

            _logger.LogDebug("Scrolling page: height={Height}px, steps={Steps}, stepSize={StepSize}px",
                dimensions.ScrollHeight, scrollSteps, stepSize);

            for (var i = 1; i <= scrollSteps; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var scrollPosition = stepSize * i;
                await page.EvaluateFunctionAsync($"() => window.scrollTo(0, {scrollPosition})");

                // Simulate mouse movement across the viewport at current scroll position
                // This triggers hover-based lazy loaders that some sites use
                await SimulateMouseMovementAsync(page, dimensions.ViewportWidth, dimensions.ViewportHeight);

                await Task.Delay(scrollDelayMs, cancellationToken);
            }

            // Ensure we scroll to absolute bottom (catches footer content like author bios)
            await page.EvaluateFunctionAsync($"() => window.scrollTo(0, {dimensions.ScrollHeight})");
            await SimulateMouseMovementAsync(page, dimensions.ViewportWidth, dimensions.ViewportHeight);
            await Task.Delay(scrollDelayMs, cancellationToken);

            // Brief scroll back up to trigger any "scroll-up" lazy loaders
            await page.EvaluateFunctionAsync($"() => window.scrollTo(0, {dimensions.ScrollHeight / 2})");
            await Task.Delay(50, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Error during page scroll: {Error}", ex.Message);
            // Non-fatal - continue without scroll
        }
    }

    /// <summary>
    /// Simulates mouse movement across the viewport to trigger hover-based lazy loaders.
    /// </summary>
    private static async Task SimulateMouseMovementAsync(IPage page, int viewportWidth, int viewportHeight)
    {
        try
        {
            // Move mouse in a pattern across the viewport
            // Start from top-left, move to center, then sweep across
            var positions = new[]
            {
                (x: viewportWidth / 4, y: viewportHeight / 4),
                (x: viewportWidth / 2, y: viewportHeight / 2),
                (x: viewportWidth * 3 / 4, y: viewportHeight / 2),
                (x: viewportWidth / 2, y: viewportHeight * 3 / 4)
            };

            foreach (var (x, y) in positions)
            {
                await page.Mouse.MoveAsync(x, y);
            }
        }
        catch
        {
            // Non-fatal - continue if mouse simulation fails
        }
    }

    // ReSharper disable once ClassNeverInstantiated.Local - Instantiated by Puppeteer JSON deserializer
    private record PageDimensions(int ScrollHeight, int ViewportWidth, int ViewportHeight);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _browser?.CloseAsync().GetAwaiter().GetResult();
            _browser?.Dispose();
            _httpClient.Dispose();
            _browserLock.Dispose();
        }

        _disposed = true;
    }
}
