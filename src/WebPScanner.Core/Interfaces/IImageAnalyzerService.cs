using PuppeteerSharp;
using WebPScanner.Core.Models;

namespace WebPScanner.Core.Interfaces;

/// <summary>
/// Service for analyzing images on web pages using Chrome DevTools Protocol.
/// </summary>
public interface IImageAnalyzerService
{
    /// <summary>
    /// Sets up CDP listeners on the page to capture network image responses.
    /// </summary>
    /// <param name="page">The Puppeteer page to attach listeners to.</param>
    /// <param name="imageCollector">Callback to collect detected images.</param>
    /// <returns>A disposable handle to detach the listeners when done.</returns>
    Task<ICdpImageListenerHandle> AttachImageListenersAsync(IPage page, Action<DetectedImage> imageCollector);

    /// <summary>
    /// Determines if a MIME type represents a non-WebP raster image.
    /// </summary>
    /// <param name="mimeType">The MIME type to check.</param>
    /// <returns>True if the MIME type is a non-WebP raster image.</returns>
    bool IsNonWebPRasterImage(string mimeType);
}

/// <summary>
/// Handle for detaching CDP image listeners.
/// </summary>
public interface ICdpImageListenerHandle : IAsyncDisposable;
