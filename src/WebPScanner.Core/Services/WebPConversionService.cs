using System.IO.Compression;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;
using WebPScanner.Core.Configuration;
using WebPScanner.Core.Entities;
using WebPScanner.Core.Interfaces;
using WebPScanner.Core.Models;

namespace WebPScanner.Core.Services;

/// <summary>
/// Service for converting images to WebP format and creating download zips.
/// </summary>
public class WebPConversionService : IWebPConversionService, IDisposable
{
    private readonly IConvertedImageZipRepository _zipRepository;
    private readonly IValidationService _validationService;
    private readonly WebPConversionOptions _options;
    private readonly ILogger<WebPConversionService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _storageDirectory;
    private readonly bool _ownsHttpClient;
    private bool _disposed;

    public WebPConversionService(
        IConvertedImageZipRepository zipRepository,
        IValidationService validationService,
        IOptions<WebPConversionOptions> options,
        ILogger<WebPConversionService> logger,
        IHttpClientFactory? httpClientFactory = null)
    {
        _zipRepository = zipRepository;
        _validationService = validationService;
        _options = options.Value;
        _logger = logger;

        if (httpClientFactory != null)
        {
            _httpClient = httpClientFactory.CreateClient("WebPConversion");
            _ownsHttpClient = false;
        }
        else
        {
            // Disable automatic redirects so manual redirect handling in DownloadAndConvertImageAsync
            // can validate each redirect target for SSRF before following
            _httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
            _ownsHttpClient = true;
        }

        _httpClient.Timeout = TimeSpan.FromSeconds(_options.ImageDownloadTimeoutSeconds);

        // Ensure storage directory exists
        _storageDirectory = Path.IsPathRooted(_options.StorageDirectory)
            ? _options.StorageDirectory
            : Path.Combine(AppContext.BaseDirectory, _options.StorageDirectory);

        Directory.CreateDirectory(_storageDirectory);
    }

    public async Task<WebPConversionResult> ConvertAndZipImagesAsync(
        ScanJob scanJob,
        IEnumerable<DiscoveredImage> discoveredImages,
        CancellationToken cancellationToken = default)
    {
        var images = discoveredImages.ToList();

        if (images.Count == 0)
        {
            return WebPConversionResult.Failed("No images to convert");
        }

        // Limit number of images
        if (images.Count > _options.MaxImagesPerScan)
        {
            _logger.LogWarning(
                "Scan {ScanId} has {Count} images, limiting to {Max}",
                scanJob.ScanId, images.Count, _options.MaxImagesPerScan);
            images = images.Take(_options.MaxImagesPerScan).ToList();
        }

        var downloadId = Guid.NewGuid();
        var domain = GetDomainFromUrl(scanJob.TargetUrl);
        var zipFileName = $"webp-images-{domain}-{downloadId:N}.zip";
        var zipFilePath = Path.Combine(_storageDirectory, zipFileName);
        var tempDirectory = Path.Combine(_storageDirectory, $"temp-{downloadId:N}");

        try
        {
            Directory.CreateDirectory(tempDirectory);

            _logger.LogInformation(
                "Starting WebP conversion for scan {ScanId}: {Count} images",
                scanJob.ScanId, images.Count);

            // Download and convert images
            var (convertedFiles, failedCount) = await DownloadAndConvertImagesAsync(
                images, tempDirectory, cancellationToken);

            if (convertedFiles.Count == 0)
            {
                return WebPConversionResult.Failed("Failed to convert any images");
            }

            // Create zip file
            _logger.LogInformation(
                "Creating zip file for scan {ScanId}: {Count} converted images",
                scanJob.ScanId, convertedFiles.Count);

            await CreateZipFileAsync(convertedFiles, zipFilePath, cancellationToken);

            var fileInfo = new FileInfo(zipFilePath);
            var expiresAt = DateTime.UtcNow.AddHours(_options.RetentionHours);

            // Save zip record to database
            var zipRecord = new ConvertedImageZip
            {
                DownloadId = downloadId,
                ScanJobId = scanJob.ScanId,
                FilePath = zipFilePath,
                FileName = $"webp-images-{domain}.zip",
                FileSizeBytes = fileInfo.Length,
                ImageCount = convertedFiles.Count,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };

            await _zipRepository.AddAsync(zipRecord, cancellationToken);

            _logger.LogInformation(
                "WebP conversion complete for scan {ScanId}: {Converted} converted, {Failed} failed, zip size: {Size} bytes",
                scanJob.ScanId, convertedFiles.Count, failedCount, fileInfo.Length);

            return WebPConversionResult.Succeeded(downloadId, convertedFiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during WebP conversion for scan {ScanId}", scanJob.ScanId);

            // Cleanup on failure
            TryDeleteFile(zipFilePath);
            TryDeleteDirectory(tempDirectory);

            return WebPConversionResult.Failed($"Conversion failed: {ex.Message}");
        }
        finally
        {
            // Always cleanup temp directory
            TryDeleteDirectory(tempDirectory);
        }
    }

    public async Task<ConvertedImageZip?> GetZipForDownloadAsync(
        Guid downloadId,
        CancellationToken cancellationToken = default)
    {
        var zip = await _zipRepository.GetByDownloadIdAsync(downloadId, cancellationToken);

        if (zip == null)
            return null;

        // Check if expired
        if (zip.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogDebug("Zip {DownloadId} has expired", downloadId);
            return null;
        }

        // Check if file still exists
        if (File.Exists(zip.FilePath))
        {
            return zip;
        }

        _logger.LogWarning("Zip file missing for download {DownloadId}: {Path}", downloadId, zip.FilePath);
        return null;

    }

    public async Task<ConvertedImageZip?> GetZipByScanIdAsync(
        Guid scanId,
        CancellationToken cancellationToken = default)
    {
        var zip = await _zipRepository.GetByScanIdAsync(scanId, cancellationToken);

        if (zip == null)
            return null;

        // Check if expired
        if (zip.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogDebug("Zip for scan {ScanId} has expired", scanId);
            return null;
        }

        // Check if file still exists
        if (File.Exists(zip.FilePath))
        {
            return zip;
        }

        _logger.LogWarning("Zip file missing for scan {ScanId}: {Path}", scanId, zip.FilePath);
        return null;

    }

    public async Task<int> CleanupExpiredZipsAsync(CancellationToken cancellationToken = default)
    {
        var expiredZips = (await _zipRepository.GetExpiredZipsAsync(DateTime.UtcNow, 100, cancellationToken)).ToList();

        if (expiredZips.Count == 0)
        {
            return 0;
        }

        var deletedCount = 0;
        var downloadIdsToDelete = new List<Guid>();

        foreach (var zip in expiredZips)
        {
            if (TryDeleteFileWithResult(zip.FilePath))
            {
                downloadIdsToDelete.Add(zip.DownloadId);
                deletedCount++;

                _logger.LogDebug(
                    "Deleted expired zip {DownloadId} for scan {ScanId}",
                    zip.DownloadId, zip.ScanJobId);
            }
            else
            {
                _logger.LogWarning("Failed to delete expired zip file: {Path}", zip.FilePath);
            }
        }

        if (downloadIdsToDelete.Count > 0)
        {
            await _zipRepository.DeleteRangeAsync(downloadIdsToDelete, cancellationToken);
        }

        _logger.LogInformation("Cleaned up {Count} expired zip files", deletedCount);
        return deletedCount;
    }

    private async Task<(List<string> ConvertedFiles, int FailedCount)> DownloadAndConvertImagesAsync(
        List<DiscoveredImage> images,
        string tempDirectory,
        CancellationToken cancellationToken)
    {
        var convertedFiles = new List<string>();
        var failedCount = 0;
        using var semaphore = new SemaphoreSlim(_options.MaxConcurrentDownloads);
        long totalDownloadedBytes = 0;
        var maxDownloadBytes = (long)_options.MaxTotalDownloadSizeMb * 1024 * 1024;
        var maxSingleImageBytes = (long)_options.MaxSingleImageSizeMb * 1024 * 1024;
        var usedFilenames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lockObj = new object();
        var totalLimitReached = false;

        // ReSharper disable AccessToDisposedClosure
        var tasks = images.Select(async (image, index) =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                // Check if total limit has already been reached by another task
                if (Volatile.Read(ref totalLimitReached))
                {
                    _logger.LogDebug("Skipping image {Url} - total download limit already reached", image.ImageUrl);
                    Interlocked.Increment(ref failedCount);
                    return;
                }

                var (filePath, actualBytes) = await DownloadAndConvertImageAsync(
                    image, tempDirectory, index, usedFilenames, lockObj, maxSingleImageBytes, cancellationToken);

                // Always count downloaded bytes toward the total limit, even if conversion failed
                // This prevents abuse via malformed images that download but fail to decode
                if (actualBytes > 0)
                {
                    var newTotal = Interlocked.Add(ref totalDownloadedBytes, actualBytes);
                    if (newTotal > maxDownloadBytes)
                    {
                        // We've exceeded the total limit - mark for other tasks and count as failed
                        Volatile.Write(ref totalLimitReached, true);
                        _logger.LogDebug(
                            "Image {Url} pushed total over limit ({Total} > {Max}), discarding",
                            image.ImageUrl, newTotal, maxDownloadBytes);
                        Interlocked.Increment(ref failedCount);
                        // Clean up the file if one was created
                        if (filePath != null)
                        {
                            TryDeleteFile(filePath);
                        }
                    }
                    else if (filePath != null)
                    {
                        // Successfully converted and within limits
                        lock (lockObj)
                        {
                            convertedFiles.Add(filePath);
                        }
                    }
                    else
                    {
                        // Downloaded but failed to convert (decode/encode failure)
                        Interlocked.Increment(ref failedCount);
                    }
                }
                else
                {
                    // No bytes downloaded (HTTP error, timeout, etc.)
                    Interlocked.Increment(ref failedCount);
                }
            }
            finally
            {
                semaphore.Release();
            }
        });
        // ReSharper restore AccessToDisposedClosure

        await Task.WhenAll(tasks);

        return (convertedFiles, failedCount);
    }

    private async Task<(string? FilePath, long ActualBytes)> DownloadAndConvertImageAsync(
        DiscoveredImage image,
        string tempDirectory,
        int index,
        HashSet<string> usedFilenames,
        object lockObj,
        long maxSingleImageBytes,
        CancellationToken cancellationToken)
    {
        const int maxRedirects = 5;

        try
        {
            // SSRF prevention: validate the image URL before downloading
            // This catches DNS rebinding attacks that may have occurred since the crawl
            if (!Uri.TryCreate(image.ImageUrl, UriKind.Absolute, out var currentUri))
            {
                _logger.LogDebug("Invalid image URL format: {Url}", image.ImageUrl);
                return (null, 0);
            }

            var ssrfCheck = await _validationService.ValidateHostSsrfAsync(currentUri.Host, cancellationToken);
            if (!ssrfCheck.IsValid)
            {
                _logger.LogWarning(
                    "SSRF check failed for image {Url}: {Errors}. Possible DNS rebinding attack.",
                    image.ImageUrl, string.Join(", ", ssrfCheck.Errors));
                return (null, 0);
            }

            // Download image with manual redirect handling to prevent SSRF via redirects
            // Each redirect target is validated for SSRF before following
            HttpResponseMessage? response = null;
            try
            {
                for (var redirectCount = 0; redirectCount <= maxRedirects; redirectCount++)
                {
                    response?.Dispose();
                    response = await _httpClient.GetAsync(
                        currentUri,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken);

                    // Check for redirects (3xx status codes)
                    var statusCode = (int)response.StatusCode;
                    if (statusCode is >= 300 and < 400)
                    {
                        var locationHeader = response.Headers.Location;
                        if (locationHeader == null)
                        {
                            _logger.LogDebug("Redirect without Location header for image {Url}", image.ImageUrl);
                            return (null, 0);
                        }

                        // Resolve relative URLs against the current URI
                        var redirectUri = locationHeader.IsAbsoluteUri
                            ? locationHeader
                            : new Uri(currentUri, locationHeader);

                        // Only allow http/https schemes
                        if (redirectUri.Scheme != Uri.UriSchemeHttp && redirectUri.Scheme != Uri.UriSchemeHttps)
                        {
                            _logger.LogDebug(
                                "Redirect to non-HTTP scheme rejected for image {Url}: {RedirectUrl}",
                                image.ImageUrl, redirectUri);
                            return (null, 0);
                        }

                        // SSRF check on redirect target - this is critical to prevent redirect-based SSRF attacks
                        var redirectSsrfCheck = await _validationService.ValidateHostSsrfAsync(
                            redirectUri.Host, cancellationToken);
                        if (!redirectSsrfCheck.IsValid)
                        {
                            _logger.LogWarning(
                                "SSRF check failed for redirect from {OriginalUrl} to {RedirectUrl}: {Errors}",
                                image.ImageUrl, redirectUri, string.Join(", ", redirectSsrfCheck.Errors));
                            return (null, 0);
                        }

                        currentUri = redirectUri;
                        continue;
                    }

                    // Not a redirect - check if successful
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogDebug(
                            "Failed to download image {Url}: {StatusCode}",
                            currentUri, response.StatusCode);
                        return (null, 0);
                    }

                    // Successful response - break out of redirect loop to process
                    break;
                }

                // Check if we exhausted redirects without getting a successful response
                if (response == null || ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400))
                {
                    _logger.LogDebug("Too many redirects for image {Url}", image.ImageUrl);
                    return (null, 0);
                }

                // Check Content-Length header if available - reject early if too large
                var contentLength = response.Content.Headers.ContentLength;
                if (contentLength > maxSingleImageBytes)
                {
                    _logger.LogDebug(
                        "Image {Url} Content-Length ({Length} bytes) exceeds max allowed ({Max} bytes)",
                        image.ImageUrl, contentLength.Value, maxSingleImageBytes);
                    return (null, 0);
                }

                // Stream the response with a hard size limit
                var (imageBytes, actualBytes, exceeded) = await ReadWithLimitAsync(
                    response.Content,
                    maxSingleImageBytes,
                    cancellationToken);

                if (exceeded)
                {
                    _logger.LogDebug(
                        "Image {Url} exceeded max size limit during download ({BytesRead} bytes read, {Max} bytes limit)",
                        image.ImageUrl, actualBytes, maxSingleImageBytes);
                    // Return actualBytes so the caller can account for consumed bandwidth
                    return (null, actualBytes);
                }

                if (imageBytes == null || imageBytes.Length == 0)
                {
                    _logger.LogDebug("Downloaded image is empty: {Url}", image.ImageUrl);
                    return (null, actualBytes);
                }

                // Convert to WebP
                using var originalBitmap = SKBitmap.Decode(imageBytes);
                if (originalBitmap == null)
                {
                    _logger.LogDebug("Failed to decode image: {Url}", image.ImageUrl);
                    return (null, actualBytes);
                }

                // Generate unique filename
                var baseName = _options.PreserveOriginalFilenames
                    ? GetFilenameFromUrl(image.ImageUrl)
                    : $"image_{index + 1}";

                var webpFileName = GetUniqueFilename(baseName, usedFilenames, lockObj);
                var webpFilePath = Path.Combine(tempDirectory, webpFileName);

                // Encode to WebP
                using var webpData = originalBitmap.Encode(SKEncodedImageFormat.Webp, _options.WebPQuality);
                if (webpData == null)
                {
                    _logger.LogDebug("Failed to encode image to WebP: {Url}", image.ImageUrl);
                    return (null, actualBytes);
                }

                await using var fileStream = File.Create(webpFilePath);
                webpData.SaveTo(fileStream);

                return (webpFilePath, actualBytes);
            }
            finally
            {
                response?.Dispose();
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("Image download timed out: {Url}", image.ImageUrl);
            return (null, 0);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error processing image: {Url}", image.ImageUrl);
            return (null, 0);
        }
    }

    /// <summary>
    /// Reads HTTP content into a byte array with a hard size limit.
    /// Returns a tuple with the data (null if exceeded), total bytes read, and whether the limit was exceeded.
    /// BytesRead is always populated so callers can account for consumed bandwidth even when the limit is exceeded.
    /// </summary>
    private static async Task<(byte[]? Data, long BytesRead, bool Exceeded)> ReadWithLimitAsync(
        HttpContent content,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        await using var sourceStream = await content.ReadAsStreamAsync(cancellationToken);
        using var memoryStream = new MemoryStream();

        var buffer = new byte[81920]; // 80KB buffer
        long totalRead = 0;

        while (true)
        {
            var bytesRead = await sourceStream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
                break;

            totalRead += bytesRead;
            if (totalRead > maxBytes)
            {
                // Exceeded limit - return with Exceeded=true and report actual bytes read
                // This ensures the caller can account for consumed bandwidth
                return (null, totalRead, Exceeded: true);
            }

            await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }

        return (memoryStream.ToArray(), totalRead, Exceeded: false);
    }

    private static async Task CreateZipFileAsync(
        List<string> files,
        string zipFilePath,
        CancellationToken cancellationToken)
    {
        await using var zipStream = File.Create(zipFilePath);
        await using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var entryName = Path.GetFileName(file);
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);

            await using var entryStream = await entry.OpenAsync(cancellationToken);
            await using var fileStream = File.OpenRead(file);
            await fileStream.CopyToAsync(entryStream, cancellationToken);
        }
    }

    private static string GetDomainFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var domain = uri.Host.Replace("www.", "");
            // Sanitize for filename
            return string.Join("_", domain.Split(Path.GetInvalidFileNameChars()));
        }
        catch
        {
            return "unknown";
        }
    }

    private static string GetFilenameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var fileName = Path.GetFileNameWithoutExtension(path);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "image";
            }

            // Sanitize filename
            var invalidChars = Path.GetInvalidFileNameChars();
            fileName = string.Join("_", fileName.Split(invalidChars));

            // Limit length
            if (fileName.Length > 50)
            {
                fileName = fileName[..50];
            }

            return fileName;
        }
        catch
        {
            return "image";
        }
    }

    private static string GetUniqueFilename(string baseName, HashSet<string> usedFilenames, object lockObj)
    {
        lock (lockObj)
        {
            var fileName = $"{baseName}.webp";
            var counter = 1;

            while (usedFilenames.Contains(fileName))
            {
                fileName = $"{baseName}_{counter}.webp";
                counter++;
            }

            usedFilenames.Add(fileName);
            return fileName;
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Ignore deletion errors
        }
    }

    /// <summary>
    /// Attempts to delete a file and returns whether the operation succeeded.
    /// </summary>
    /// <param name="path">The file path to delete.</param>
    /// <returns>True if the file was deleted or didn't exist; false if deletion failed.</returns>
    private static bool TryDeleteFileWithResult(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            return true; // File deleted or didn't exist
        }
        catch
        {
            return false; // Deletion failed
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Ignore deletion errors
        }
    }

    /// <summary>
    /// Disposes the HttpClient if this service owns it (created without IHttpClientFactory).
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
