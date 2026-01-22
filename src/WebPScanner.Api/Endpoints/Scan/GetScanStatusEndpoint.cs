using FastEndpoints;
using WebPScanner.Core.DTOs;
using WebPScanner.Core.Enums;
using WebPScanner.Core.Interfaces;

namespace WebPScanner.Api.Endpoints.Scan;

public class GetScanStatusEndpoint : Endpoint<GetScanStatusRequest, ScanStatusDto>
{
    private readonly IScanJobRepository _scanJobRepository;

    public GetScanStatusEndpoint(IScanJobRepository scanJobRepository)
    {
        _scanJobRepository = scanJobRepository;
    }

    public override void Configure()
    {
        Get("/api/scan/{scanId}/status");
        AllowAnonymous();
        Description(b => b
            .Produces<ScanStatusDto>()
            .Produces(404));
    }

    public override async Task HandleAsync(GetScanStatusRequest req, CancellationToken ct)
    {
        if (!Guid.TryParse(req.ScanId, out var scanId))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var scanJob = await _scanJobRepository.GetByIdAsync(scanId, ct);
        if (scanJob == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Calculate current queue position if still queued
        int? currentQueuePosition = null;
        if (scanJob.Status == ScanStatus.Queued)
        {
            currentQueuePosition = await _scanJobRepository.GetQueuePositionAsync(scanId, ct);
        }

        var status = new ScanStatusDto
        {
            ScanId = scanJob.ScanId,
            Status = scanJob.Status,
            QueuePosition = currentQueuePosition,
            TargetUrl = scanJob.TargetUrl,
            PagesDiscovered = scanJob.PagesDiscovered,
            PagesScanned = scanJob.PagesScanned,
            NonWebPImagesFound = scanJob.NonWebPImagesFound,
            CreatedAt = scanJob.CreatedAt,
            StartedAt = scanJob.StartedAt,
            CompletedAt = scanJob.CompletedAt,
            ErrorMessage = scanJob.ErrorMessage
        };

        await Send.OkAsync(status, ct);
    }
}
