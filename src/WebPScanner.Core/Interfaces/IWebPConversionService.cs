using WebPScanner.Core.Entities;
using WebPScanner.Core.Models;

namespace WebPScanner.Core.Interfaces;

/// <summary>
/// Service for converting images to WebP format and creating download zips.
/// </summary>
public interface IWebPConversionService
{
    /// <summary>
    /// Converts all discovered non-WebP images from a scan to WebP format
    /// and creates a zip file for download.
    /// </summary>
    /// <param name="scanJob">The completed scan job.</param>
    /// <param name="discoveredImages">The non-WebP images to convert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing download info or error message.</returns>
    Task<WebPConversionResult> ConvertAndZipImagesAsync(
        ScanJob scanJob,
        IEnumerable<DiscoveredImage> discoveredImages,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the zip file info for a download ID if it exists and hasn't expired.
    /// </summary>
    /// <param name="downloadId">The download ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The zip info if found and valid, null otherwise.</returns>
    Task<ConvertedImageZip?> GetZipForDownloadAsync(
        Guid downloadId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the zip file info by scan ID if it exists and hasn't expired.
    /// </summary>
    /// <param name="scanId">The scan ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The zip info if found and valid, null otherwise.</returns>
    Task<ConvertedImageZip?> GetZipByScanIdAsync(
        Guid scanId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes expired zip files from storage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of files deleted.</returns>
    Task<int> CleanupExpiredZipsAsync(CancellationToken cancellationToken = default);
}
