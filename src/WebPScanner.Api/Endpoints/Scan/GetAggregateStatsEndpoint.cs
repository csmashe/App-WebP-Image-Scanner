using FastEndpoints;
using WebPScanner.Core.DTOs;
using WebPScanner.Core.Interfaces;

namespace WebPScanner.Api.Endpoints.Scan;

public class GetAggregateStatsEndpoint : EndpointWithoutRequest<AggregateStatsDto>
{
    private readonly IAggregateStatsService _aggregateStatsService;

    public GetAggregateStatsEndpoint(IAggregateStatsService aggregateStatsService)
    {
        _aggregateStatsService = aggregateStatsService;
    }

    public override void Configure()
    {
        Get("/api/scan/stats");
        AllowAnonymous();
        Description(b => b.Produces<AggregateStatsDto>());
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var stats = await _aggregateStatsService.GetCombinedStatsAsync(ct);
        await Send.OkAsync(stats, ct);
    }
}
