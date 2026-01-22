using FastEndpoints;
using WebPScanner.Core.DTOs;
using WebPScanner.Core.Entities;
using WebPScanner.Core.Enums;
using WebPScanner.Core.Interfaces;

namespace WebPScanner.Api.Endpoints.Scan;

public class SubmitScanEndpoint : Endpoint<ScanRequestDto, ScanResponseDto>
{
    private readonly IValidationService _validationService;
    private readonly IScanJobRepository _scanJobRepository;
    private readonly IQueueService _queueService;
    private readonly IScanProgressService _scanProgressService;
    private readonly ILogger<SubmitScanEndpoint> _logger;

    public SubmitScanEndpoint(
        IValidationService validationService,
        IScanJobRepository scanJobRepository,
        IQueueService queueService,
        IScanProgressService scanProgressService,
        ILogger<SubmitScanEndpoint> logger)
    {
        _validationService = validationService;
        _scanJobRepository = scanJobRepository;
        _queueService = queueService;
        _scanProgressService = scanProgressService;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/api/scan");
        AllowAnonymous();
        Options(x => x.RequireRateLimiting("scan-submission"));
        Description(b => b
            .Produces<ScanResponseDto>(201)
            .Produces<ValidationErrorDto>(400)
            .Produces(429));
    }

    public override async Task HandleAsync(ScanRequestDto req, CancellationToken ct)
    {
        // Server-side re-validation before queue insertion
        var validationResult = await _validationService.ValidateScanRequestAsync(req.Url, req.Email, ct);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Scan request validation failed: {Errors}", string.Join(", ", validationResult.Errors));
            HttpContext.Response.StatusCode = 400;
            await HttpContext.Response.WriteAsJsonAsync(ValidationErrorDto.FromErrors(validationResult.Errors), ct);
            return;
        }

        var submitterIp = GetClientIpAddress();

        if (!await _queueService.CanEnqueueAsync(ct))
        {
            _logger.LogWarning("Queue is full, rejecting scan request from {IP}", submitterIp);
            HttpContext.Response.StatusCode = 429;
            await HttpContext.Response.WriteAsJsonAsync(new { Message = "The queue is currently full. Please try again later." }, ct);
            return;
        }

        if (await _queueService.HasIpReachedQueueLimitAsync(submitterIp, ct))
        {
            _logger.LogWarning("IP {IP} has reached the maximum queued jobs limit", submitterIp);
            HttpContext.Response.StatusCode = 429;
            await HttpContext.Response.WriteAsJsonAsync(new { Message = "You have reached the maximum number of queued scans. Please wait for your existing scans to complete." }, ct);
            return;
        }

        if (_queueService.IsIpInCooldown(submitterIp))
        {
            _logger.LogWarning("IP {IP} is in cooldown period", submitterIp);
            HttpContext.Response.StatusCode = 429;
            await HttpContext.Response.WriteAsJsonAsync(new { Message = "Please wait before submitting another scan." }, ct);
            return;
        }

        var ipJobCount = await _scanJobRepository.GetJobCountByIpAsync(submitterIp, ct);

        var scanJob = new ScanJob
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = req.Url,
            Email = req.Email,
            Status = ScanStatus.Queued,
            SubmitterIp = submitterIp,
            SubmissionCount = ipJobCount + 1,
            CreatedAt = DateTime.UtcNow,
            ConvertToWebP = req.ConvertToWebP
        };

        var enqueuedJob = await _queueService.EnqueueAsync(scanJob, ct);

        _logger.LogInformation(
            "Scan job created: {ScanId} for URL {Url}, queue position {Position}",
            enqueuedJob.ScanId, enqueuedJob.TargetUrl, enqueuedJob.QueuePosition);

        await _scanProgressService.BroadcastQueuePositionsAsync(ct);

        var response = new ScanResponseDto
        {
            ScanId = enqueuedJob.ScanId,
            QueuePosition = enqueuedJob.QueuePosition,
            Message = $"Scan queued successfully. You are position {enqueuedJob.QueuePosition} in the queue.",
            ConvertToWebP = enqueuedJob.ConvertToWebP
        };

        await Send.CreatedAtAsync<GetScanStatusEndpoint>(new { scanId = enqueuedJob.ScanId }, response, cancellation: ct);
    }

    private string GetClientIpAddress()
    {
        // Use RemoteIpAddress to prevent IP spoofing via X-Forwarded-For headers.
        // For reverse proxy setups, configure ForwardedHeadersMiddleware with KnownProxies.
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
