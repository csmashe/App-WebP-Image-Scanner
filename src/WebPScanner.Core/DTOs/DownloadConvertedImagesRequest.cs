// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace WebPScanner.Core.DTOs;

/// <summary>
/// Request to download converted WebP images zip by download ID.
/// </summary>
public class DownloadConvertedImagesRequest
{
    /// <summary>
    /// The unique download identifier.
    /// </summary>
    public Guid DownloadId { get; set; }
}

/// <summary>
/// Request to download converted WebP images zip by scan ID.
/// </summary>
public class DownloadConvertedImagesByScanRequest
{
    /// <summary>
    /// The scan ID to get converted images for.
    /// </summary>
    public Guid ScanId { get; set; }
}
