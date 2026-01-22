using FastEndpoints;
using WebPScanner.Core.DTOs;
using WebPScanner.Core.Enums;
using WebPScanner.Core.Interfaces;
using WebPScanner.Core.Models;

namespace WebPScanner.Api.Endpoints.Scan;

public class DownloadReportEndpoint : Endpoint<DownloadReportRequest>
{
    private readonly IScanJobRepository _scanJobRepository;
    private readonly IDiscoveredImageRepository _discoveredImageRepository;
    private readonly IPdfReportService _pdfReportService;
    private readonly ISavingsEstimatorService _savingsEstimatorService;
    private readonly ILogger<DownloadReportEndpoint> _logger;

    public DownloadReportEndpoint(
        IScanJobRepository scanJobRepository,
        IDiscoveredImageRepository discoveredImageRepository,
        IPdfReportService pdfReportService,
        ISavingsEstimatorService savingsEstimatorService,
        ILogger<DownloadReportEndpoint> logger)
    {
        _scanJobRepository = scanJobRepository;
        _discoveredImageRepository = discoveredImageRepository;
        _pdfReportService = pdfReportService;
        _savingsEstimatorService = savingsEstimatorService;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/api/scan/{scanId}/report");
        AllowAnonymous();
        Description(b => b
            .Produces<byte[]>(200, "application/pdf")
            .Produces(404)
            .Produces(400));
    }

    public override async Task HandleAsync(DownloadReportRequest req, CancellationToken ct)
    {
        var scanJob = await _scanJobRepository.GetByIdAsync(req.ScanId, ct);
        if (scanJob == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        if (scanJob.Status != ScanStatus.Completed)
        {
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(new { Message = "Report is only available for completed scans." }, ct);
            return;
        }

        var discoveredImages = await _discoveredImageRepository.GetByScanJobIdOrderedBySavingsAsync(req.ScanId, ct);
        var imagesList = discoveredImages.ToList();

        var imageEstimates = _savingsEstimatorService.CalculateImageSavings(imagesList);
        var savingsSummary = _savingsEstimatorService.CalculateSavingsSummary(imagesList);

        // GroupBy handles potential duplicate ImageUrls, merging their page URLs
        var imageToPagesMap = imagesList
            .GroupBy(img => img.ImageUrl)
            .ToDictionary(
                g => g.Key,
                g => g.SelectMany(img => img.GetPageUrls()).Distinct().ToList());

        var crawlDuration = scanJob is { CompletedAt: not null, StartedAt: not null }
            ? scanJob.CompletedAt.Value - scanJob.StartedAt.Value
            : TimeSpan.Zero;

        var reportData = new PdfReportData
        {
            ScanId = scanJob.ScanId,
            TargetUrl = scanJob.TargetUrl,
            ScanDate = scanJob.CompletedAt ?? scanJob.CreatedAt,
            PagesScanned = scanJob.PagesScanned,
            PagesDiscovered = scanJob.PagesDiscovered,
            CrawlDuration = crawlDuration,
            ReachedPageLimit = scanJob.PagesDiscovered > scanJob.PagesScanned,
            SavingsSummary = savingsSummary,
            ImageEstimates = imageEstimates,
            ImageToPagesMap = imageToPagesMap
        };

        var pdfBytes = _pdfReportService.GenerateReport(reportData);

        var domain = "report";
        try
        {
            var uri = new Uri(scanJob.TargetUrl);
            domain = uri.Host.Replace("www.", "");
        }
        catch
        {
            // Ignore URI parsing errors
        }

        var fileName = $"webp-scan-{domain}-{scanJob.ScanId:N}.pdf";

        _logger.LogInformation("Generated PDF report for scan {ScanId}, size: {Size} bytes", req.ScanId, pdfBytes.Length);

        await Send.BytesAsync(pdfBytes, fileName, "application/pdf", cancellation: ct);
    }
}
