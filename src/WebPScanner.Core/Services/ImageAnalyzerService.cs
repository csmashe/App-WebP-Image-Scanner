using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using WebPScanner.Core.Interfaces;
using WebPScanner.Core.Models;

namespace WebPScanner.Core.Services;

/// <summary>
/// Service for analyzing images on web pages using Chrome DevTools Protocol.
/// </summary>
public class ImageAnalyzerService : IImageAnalyzerService
{
    private readonly ILogger<ImageAnalyzerService> _logger;

    /// <summary>
    /// Set of non-WebP raster image MIME types that we want to detect.
    /// </summary>
    private static readonly HashSet<string> NonWebPRasterMimeTypesSet = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/gif",
        "image/bmp",
        "image/tiff",
        "image/x-ms-bmp"
    };

    public ImageAnalyzerService(ILogger<ImageAnalyzerService> logger)
    {
        _logger = logger;
    }

    public bool IsNonWebPRasterImage(string mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
            return false;

        // Handle MIME types with parameters (e.g., "image/jpeg; charset=utf-8")
        var normalizedMimeType = mimeType.Split(';')[0].Trim();
        return NonWebPRasterMimeTypesSet.Contains(normalizedMimeType);
    }

    public async Task<ICdpImageListenerHandle> AttachImageListenersAsync(IPage page, Action<DetectedImage> imageCollector)
    {
        var handle = new CdpImageListenerHandle(page, imageCollector, _logger);
        await handle.AttachAsync();
        return handle;
    }

    /// <summary>
    /// Handle for managing CDP image listeners on a page.
    /// </summary>
    private class CdpImageListenerHandle : ICdpImageListenerHandle
    {
        private readonly IPage _page;
        private readonly Action<DetectedImage> _imageCollector;
        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, NetworkResponseInfo> _pendingResponses = new();
        private ICDPSession? _cdpSession;
        private bool _disposed;

        public CdpImageListenerHandle(IPage page, Action<DetectedImage> imageCollector, ILogger logger)
        {
            _page = page;
            _imageCollector = imageCollector;
            _logger = logger;
        }

        public bool HasPendingImages => !_pendingResponses.IsEmpty;

        public async Task AttachAsync()
        {
            // Create CDP session
            _cdpSession = await _page.CreateCDPSessionAsync();

            // Enable Network domain
            await _cdpSession.SendAsync("Network.enable", new Dictionary<string, object>
            {
                ["maxTotalBufferSize"] = 10000000,
                ["maxResourceBufferSize"] = 5000000
            });

            // Listen for Network.responseReceived events
            _cdpSession.MessageReceived += OnCdpMessageReceived;
        }

        public async Task<bool> WaitForPendingImagesAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (_disposed || _pendingResponses.IsEmpty)
                return true;

            var deadline = DateTime.UtcNow + timeout;
            const int pollIntervalMs = 50;

            while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
            {
                if (_pendingResponses.IsEmpty)
                    return true;

                await Task.Delay(pollIntervalMs, cancellationToken);
            }

            return _pendingResponses.IsEmpty;
        }

        private void OnCdpMessageReceived(object? sender, MessageEventArgs e)
        {
            // Guard against late CDP messages arriving during or after disposal
            if (_disposed)
                return;

            try
            {
	            switch (e.MessageID)
	            {
		            case "Network.responseReceived":
			            ProcessResponseReceived(e.MessageData);
			            break;
		            case "Network.loadingFinished":
			            ProcessLoadingFinished(e.MessageData);
			            break;
	            }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Error processing CDP message {MessageId}: {Error}", e.MessageID, ex.Message);
            }
        }

        private void ProcessResponseReceived(JsonElement messageData)
        {
            try
            {
                var requestId = messageData.GetProperty("requestId").GetString();

                if (string.IsNullOrEmpty(requestId))
                {
                    _logger.LogDebug("Skipping responseReceived with null or empty requestId");
                    return;
                }

                var response = messageData.GetProperty("response");

                var mimeType = response.TryGetProperty("mimeType", out var mimeTypeProp)
                    ? mimeTypeProp.GetString() ?? string.Empty
                    : string.Empty;

                // Only process image responses
                if (!mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                    return;

                var url = response.TryGetProperty("url", out var urlProp)
                    ? urlProp.GetString() ?? string.Empty
                    : string.Empty;

                // Skip data URIs and empty URLs
                if (string.IsNullOrEmpty(url) || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    return;

                var headers = response.TryGetProperty("headers", out var headersProp)
                    ? headersProp
                    : default;

                // Get content-length from headers if available
                long contentLength = 0;
                if (headers.ValueKind == JsonValueKind.Object)
                {
                    if (headers.TryGetProperty("content-length", out var contentLengthProp) ||
                        headers.TryGetProperty("Content-Length", out contentLengthProp))
                    {
                        var lengthStr = contentLengthProp.GetString();
                        if (!string.IsNullOrEmpty(lengthStr))
                        {
	                        // If parsing fails, contentLength remains 0 which is acceptable
		                   // since encodedDataLength from loadingFinished is preferred anyway
		                   _ = long.TryParse(lengthStr, out contentLength);
                        }
                    }
                }

                // Log detected image for debugging
                _logger.LogDebug("CDP detected image: {Url} with MIME type: {MimeType}", url, mimeType);

                // Store pending response info for when loadingFinished provides encoded data length
                _pendingResponses[requestId] = new NetworkResponseInfo
                {
                    Url = url,
                    MimeType = mimeType,
                    ContentLength = contentLength
                };
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Error parsing responseReceived: {Error}", ex.Message);
            }
        }

        private void ProcessLoadingFinished(JsonElement messageData)
        {
            try
            {
                var requestId = messageData.GetProperty("requestId").GetString();

                if (requestId == null || !_pendingResponses.TryRemove(requestId, out var responseInfo))
                    return;

                // Get encoded data length (actual bytes transferred over the network)
                long encodedDataLength = 0;
                if (messageData.TryGetProperty("encodedDataLength", out var encodedLengthProp))
                {
                    encodedDataLength = encodedLengthProp.GetInt64();
                }

                // Use encodedDataLength if available, otherwise fall back to content-length
                var size = encodedDataLength > 0 ? encodedDataLength : responseInfo.ContentLength;

                var detectedImage = new DetectedImage
                {
                    Url = responseInfo.Url,
                    MimeType = responseInfo.MimeType,
                    Size = size
                };

                _imageCollector(detectedImage);
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Error parsing loadingFinished: {Error}", ex.Message);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;

            // Unsubscribe from CDP events immediately to prevent new callbacks during drain
            if (_cdpSession != null)
            {
                _cdpSession.MessageReceived -= OnCdpMessageReceived;
            }

            // Process any remaining pending responses
            foreach (var kvp in _pendingResponses)
            {
                var responseInfo = kvp.Value;
                var detectedImage = new DetectedImage
                {
                    Url = responseInfo.Url,
                    MimeType = responseInfo.MimeType,
                    Size = responseInfo.ContentLength
                };
                _imageCollector(detectedImage);
            }
            _pendingResponses.Clear();

            if (_cdpSession != null)
            {
                try
                {
                    await _cdpSession.DetachAsync();
                }
                catch
                {
                    // Ignore detach errors
                }
            }
        }

        private class NetworkResponseInfo
        {
            public string Url { get; init; } = string.Empty;
            public string MimeType { get; init; } = string.Empty;
            public long ContentLength { get; init; }
        }
    }
}
