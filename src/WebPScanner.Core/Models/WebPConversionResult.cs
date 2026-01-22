namespace WebPScanner.Core.Models;

/// <summary>
/// Result of a WebP conversion operation.
/// </summary>
public class WebPConversionResult
{
    /// <summary>
    /// Indicates whether the conversion succeeded.
    /// </summary>
    public bool Success { get; private init; }

    /// <summary>
    /// Unique identifier for downloading the converted zip file.
    /// </summary>
    public Guid? DownloadId { get; private init; }

    /// <summary>
    /// Number of images successfully converted to WebP.
    /// </summary>
    public int ImagesConverted { get; private init; }

    /// <summary>
    /// Error message when conversion fails.
    /// </summary>
    public string? ErrorMessage { get; private init; }

    /// <summary>
    /// Creates a successful conversion result.
    /// </summary>
    /// <param name="downloadId">Unique download identifier.</param>
    /// <param name="imagesConverted">Number of images converted.</param>
    /// <returns>A successful conversion result.</returns>
    public static WebPConversionResult Succeeded(Guid downloadId, int imagesConverted)
    {
        return new WebPConversionResult
        {
            Success = true,
            DownloadId = downloadId,
            ImagesConverted = imagesConverted
        };
    }

    /// <summary>
    /// Creates a failed conversion result.
    /// </summary>
    /// <param name="errorMessage">The error message describing the failure.</param>
    /// <returns>A failed conversion result.</returns>
    public static WebPConversionResult Failed(string errorMessage)
    {
        return new WebPConversionResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
