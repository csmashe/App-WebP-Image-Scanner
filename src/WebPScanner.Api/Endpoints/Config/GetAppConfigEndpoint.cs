using FastEndpoints;
using Microsoft.Extensions.Options;
using WebPScanner.Core.Configuration;
using WebPScanner.Core.DTOs;

namespace WebPScanner.Api.Endpoints.Config;

public class GetAppConfigEndpoint : EndpointWithoutRequest<AppConfigResponseDto>
{
    private readonly EmailOptions _emailOptions;

    public GetAppConfigEndpoint(IOptions<EmailOptions> emailOptions)
    {
        _emailOptions = emailOptions.Value;
    }

    public override void Configure()
    {
        Get("/api/config");
        AllowAnonymous();
        Description(b => b.Produces<AppConfigResponseDto>());
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // Check if email is actually usable - must be enabled AND have an API key configured
        // This mirrors the logic in EmailService which checks the environment variable first,
        // then falls back to the config ApiKey
        var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY") ?? _emailOptions.ApiKey;
        var emailEnabled = _emailOptions.Enabled && !string.IsNullOrEmpty(apiKey);

        var response = new AppConfigResponseDto
        {
            EmailEnabled = emailEnabled
        };

        await Send.OkAsync(response, ct);
    }
}
