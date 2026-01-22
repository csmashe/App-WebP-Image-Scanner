using FastEndpoints;
using WebPScanner.Core.DTOs;
using WebPScanner.Core.Interfaces;

namespace WebPScanner.Api.Endpoints.Scan;

/// <summary>
/// Endpoint to download converted WebP images zip by download ID.
/// Returns 410 Gone if the link has expired.
/// </summary>
public class DownloadConvertedImagesEndpoint : Endpoint<DownloadConvertedImagesRequest>
{
    private readonly IWebPConversionService _webPConversionService;
    private readonly ILogger<DownloadConvertedImagesEndpoint> _logger;

    public DownloadConvertedImagesEndpoint(
        IWebPConversionService webPConversionService,
        ILogger<DownloadConvertedImagesEndpoint> logger)
    {
        _webPConversionService = webPConversionService;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/api/images/{downloadId}");
        AllowAnonymous();
        Description(b => b
            .Produces<byte[]>(200, "application/zip")
            .Produces(404)
            .Produces(410));
    }

    public override async Task HandleAsync(DownloadConvertedImagesRequest req, CancellationToken ct)
    {
        var zip = await _webPConversionService.GetZipForDownloadAsync(req.DownloadId, ct);

        if (zip == null)
        {
            _logger.LogDebug("Download requested for expired or missing zip: {DownloadId}", req.DownloadId);

            HttpContext.Response.StatusCode = 410;
            await HttpContext.Response.WriteAsJsonAsync(new
            {
                Message = "This download link has expired. Converted image zips are only available for 6 hours after creation.",
                Expired = true
            }, ct);
            return;
        }

        try
        {
            await using var stream = File.OpenRead(zip.FilePath);

            _logger.LogInformation(
                "Serving converted images zip: {DownloadId}, size: {Size} bytes",
                req.DownloadId, zip.FileSizeBytes);

            HttpContext.Response.ContentType = "application/zip";

            await Send.StreamAsync(stream, zip.FileName, zip.FileSizeBytes, contentType: "application/zip", cancellation: ct);
        }
        catch (FileNotFoundException)
        {
            _logger.LogWarning("Zip file not found on disk: {Path}", zip.FilePath);
            HttpContext.Response.StatusCode = 410;
            await HttpContext.Response.WriteAsJsonAsync(new
            {
                Message = "This download link has expired. The file is no longer available.",
                Expired = true
            }, ct);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "IO error accessing zip file: {Path}", zip.FilePath);
            HttpContext.Response.StatusCode = 410;
            await HttpContext.Response.WriteAsJsonAsync(new
            {
                Message = "This download link has expired. The file is no longer available.",
                Expired = true
            }, ct);
        }
    }
}

/// <summary>
/// Endpoint to download converted WebP images zip by scan ID.
/// Returns 410 Gone if the link has expired or 404 if no conversion was requested.
/// </summary>
public class DownloadConvertedImagesByScanEndpoint : Endpoint<DownloadConvertedImagesByScanRequest>
{
    private readonly IWebPConversionService _webPConversionService;
    private readonly IScanJobRepository _scanJobRepository;
    private readonly ILogger<DownloadConvertedImagesByScanEndpoint> _logger;

    public DownloadConvertedImagesByScanEndpoint(
        IWebPConversionService webPConversionService,
        IScanJobRepository scanJobRepository,
        ILogger<DownloadConvertedImagesByScanEndpoint> logger)
    {
        _webPConversionService = webPConversionService;
        _scanJobRepository = scanJobRepository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/api/scan/{scanId}/images");
        AllowAnonymous();
        Description(b => b
            .Produces<byte[]>(200, "application/zip")
            .Produces(404)
            .Produces(410));
    }

    public override async Task HandleAsync(DownloadConvertedImagesByScanRequest req, CancellationToken ct)
    {
        var scanJob = await _scanJobRepository.GetByIdAsync(req.ScanId, ct);
        if (scanJob == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (!scanJob.ConvertToWebP)
        {
            HttpContext.Response.StatusCode = 404;
            await HttpContext.Response.WriteAsJsonAsync(new
            {
                Message = "WebP conversion was not requested for this scan."
            }, ct);
            return;
        }

        var zip = await _webPConversionService.GetZipByScanIdAsync(req.ScanId, ct);

        if (zip == null)
        {
            _logger.LogDebug("Download requested for expired or missing zip for scan: {ScanId}", req.ScanId);

            HttpContext.Response.StatusCode = 410;
            await HttpContext.Response.WriteAsJsonAsync(new
            {
                Message = "This download link has expired. Converted image zips are only available for 6 hours after creation.",
                Expired = true
            }, ct);
            return;
        }

        try
        {
            await using var stream = File.OpenRead(zip.FilePath);

            _logger.LogInformation(
                "Serving converted images zip for scan {ScanId}: size: {Size} bytes",
                req.ScanId, zip.FileSizeBytes);

            HttpContext.Response.ContentType = "application/zip";

            await Send.StreamAsync(stream, zip.FileName, zip.FileSizeBytes, contentType: "application/zip", cancellation: ct);
        }
        catch (FileNotFoundException)
        {
            _logger.LogWarning("Zip file not found on disk: {Path}", zip.FilePath);
            HttpContext.Response.StatusCode = 410;
            await HttpContext.Response.WriteAsJsonAsync(new
            {
                Message = "This download link has expired. The file is no longer available.",
                Expired = true
            }, ct);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "IO error accessing zip file: {Path}", zip.FilePath);
            HttpContext.Response.StatusCode = 410;
            await HttpContext.Response.WriteAsJsonAsync(new
            {
                Message = "This download link has expired. The file is no longer available.",
                Expired = true
            }, ct);
        }
    }
}
