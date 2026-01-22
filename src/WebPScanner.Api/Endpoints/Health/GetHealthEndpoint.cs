using FastEndpoints;
using WebPScanner.Core.DTOs;
using WebPScanner.Core.Interfaces;

namespace WebPScanner.Api.Endpoints.Health;

public class GetHealthEndpoint : EndpointWithoutRequest<HealthResponseDto>
{
    private readonly IScanJobRepository _scanJobRepository;
    private readonly ILogger<GetHealthEndpoint> _logger;

    public GetHealthEndpoint(
        IScanJobRepository scanJobRepository,
        ILogger<GetHealthEndpoint> logger)
    {
        _scanJobRepository = scanJobRepository;
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/api/health");
        AllowAnonymous();
        Description(b => b.Produces<HealthResponseDto>());
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var queuedJobs = await _scanJobRepository.GetQueuedCountAsync(ct);
        var processingJobs = await _scanJobRepository.GetProcessingCountAsync(ct);

        var response = new HealthResponseDto
        {
            Status = "Healthy",
            QueuedJobs = queuedJobs,
            ProcessingJobs = processingJobs,
            Timestamp = DateTime.UtcNow
        };

        _logger.LogDebug("Health check: {QueuedJobs} queued, {ProcessingJobs} processing", queuedJobs, processingJobs);

        await Send.OkAsync(response, ct);
    }
}
