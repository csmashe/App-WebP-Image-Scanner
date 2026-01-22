using System.Net;
using System.Threading.RateLimiting;
using FastEndpoints;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebPScanner.Api.Hubs;
using WebPScanner.Api.Middleware;
using WebPScanner.Api.Services;
using WebPScanner.Core.Configuration;
using WebPScanner.Core.Interfaces;
using WebPScanner.Core.Services;
using WebPScanner.Data.Context;
using WebPScanner.Data.Repositories;
using WebPScanner.Core.Utilities;
using WebPScanner.Data.Services;

var builder = WebApplication.CreateBuilder(args);

var sentryDsn = builder.Configuration["Sentry:Dsn"] ?? Environment.GetEnvironmentVariable("SENTRY_DSN");
if (!string.IsNullOrWhiteSpace(sentryDsn))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = sentryDsn;
        options.Environment = builder.Environment.EnvironmentName;
        options.SendDefaultPii = builder.Configuration.GetValue("Sentry:SendDefaultPii", false);
        options.TracesSampleRate = builder.Configuration.GetValue("Sentry:TracesSampleRate", 0.1);
    });
}

builder.Services.AddFastEndpoints();
builder.Services.AddOpenApi();

builder.Services.Configure<WebPScanner.Core.Configuration.SecurityOptions>(builder.Configuration
    .GetSection(WebPScanner.Core.Configuration.SecurityOptions.SectionName));

// Must be configured before rate limiting so RemoteIpAddress reflects the real client IP
var securitySection = builder.Configuration.GetSection(WebPScanner.Core.Configuration.SecurityOptions.SectionName)
    .Get<WebPScanner.Core.Configuration.SecurityOptions>() ?? new WebPScanner.Core.Configuration.SecurityOptions();
if (securitySection.ForwardedHeadersEnabled)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = securitySection.ForwardLimit;

        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();

        foreach (var proxy in securitySection.TrustedProxies)
        {
            if (string.IsNullOrWhiteSpace(proxy)) continue;

            if (proxy.Contains('/'))
            {
                var parts = proxy.Split('/');
                if (parts.Length != 2 ||
                    !IPAddress.TryParse(parts[0], out var networkAddress) ||
                    !int.TryParse(parts[1], out var prefixLength))
                {
                    continue;
                }

                // 0-32 for IPv4, 0-128 for IPv6
                var maxPrefixLength = networkAddress.GetAddressBytes().Length * 8;
                if (prefixLength >= 0 && prefixLength <= maxPrefixLength)
                {
                    options.KnownIPNetworks.Add(new System.Net.IPNetwork(networkAddress, prefixLength));
                }
            }
            else if (IPAddress.TryParse(proxy, out var proxyAddress))
            {
                options.KnownProxies.Add(proxyAddress);
            }
        }
    });
}

// For multi-node deployments, consider Redis-backed rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("scan-submission", context =>
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var securityOpts = context.RequestServices.GetService<IOptions<WebPScanner.Core.Configuration.SecurityOptions>>()?.Value
            ?? new WebPScanner.Core.Configuration.SecurityOptions();

        if (IsIpExemptFromRateLimiting(clientIp, securityOpts.RateLimitExemptIps))
        {
            return RateLimitPartition.GetNoLimiter(clientIp);
        }

        // Per-hour limits are secondary protection
        return RateLimitPartition.GetSlidingWindowLimiter(clientIp, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = securityOpts.MaxRequestsPerMinute,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 4, // 15-second segments for smoother limiting
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0, // Reject immediately, don't queue
            AutoReplenishment = true
        });
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        var logger = context.HttpContext.RequestServices.GetService<ILogger<Program>>();
        var clientIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        logger?.LogWarning("Rate limit exceeded for IP {ClientIp} on {Path}",
            clientIp, context.HttpContext.Request.Path);

        context.HttpContext.Response.ContentType = "application/json";

        context.HttpContext.Response.Headers.RetryAfter =
	        context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
		        ? ((int)retryAfter.TotalSeconds).ToString() : "60";

        await context.HttpContext.Response.WriteAsync(
            "{\"error\": \"Rate limit exceeded. Please try again later.\"}", cancellationToken);
    };
});

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.MaximumReceiveMessageSize = 32 * 1024; // 32KB
})
.AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

builder.Services.Configure<QueueOptions>(builder.Configuration.GetSection(QueueOptions.SectionName));
builder.Services.Configure<CrawlerOptions>(builder.Configuration.GetSection(CrawlerOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.Configure<DataRetentionOptions>(builder.Configuration.GetSection(DataRetentionOptions.SectionName));
builder.Services.Configure<WebPConversionOptions>(builder.Configuration.GetSection(WebPConversionOptions.SectionName));

builder.Services.AddDbContext<WebPScannerDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=webpscanner.db"));

builder.Services.AddScoped<IScanJobRepository, ScanJobRepository>();
builder.Services.AddScoped<IDiscoveredImageRepository, DiscoveredImageRepository>();
builder.Services.AddScoped<IConvertedImageZipRepository, ConvertedImageZipRepository>();
builder.Services.AddScoped<ICrawlCheckpointRepository, CrawlCheckpointRepository>();

builder.Services.AddSingleton<IValidationService, ValidationService>();
builder.Services.AddScoped<IQueueService, QueueService>();
builder.Services.AddSingleton<IImageAnalyzerService, ImageAnalyzerService>();
builder.Services.AddSingleton<ICrawlerService, CrawlerService>();
builder.Services.AddSingleton<ISavingsEstimatorService, SavingsEstimatorService>();
builder.Services.AddSingleton<IPdfReportService, PdfReportService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddSingleton<ILiveScanStatsTracker, LiveScanStatsTracker>();

// AllowAutoRedirect disabled to prevent SSRF via redirects
builder.Services.AddHttpClient("WebPConversion", client =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "WebPScanner/1.0");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = false
});
builder.Services.AddScoped<IWebPConversionService, WebPConversionService>();

builder.Services.AddScoped<IScanProgressService, ScanProgressService>();
builder.Services.AddScoped<IAggregateStatsService, AggregateStatsService>();

builder.Services.AddHostedService<QueueProcessorService>();
builder.Services.AddHostedService<DataRetentionService>();
builder.Services.AddHostedService<StatsBroadcastService>();
builder.Services.AddHostedService<ImageZipCleanupService>();

var securityConfig = builder.Configuration.GetSection(WebPScanner.Core.Configuration.SecurityOptions.SectionName)
    .Get<WebPScanner.Core.Configuration.SecurityOptions>() ?? new WebPScanner.Core.Configuration.SecurityOptions();
if (securityConfig.EnableRequestSizeLimit)
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.Limits.MaxRequestBodySize = securityConfig.MaxRequestBodySizeBytes;
    });
}

var app = builder.Build();

// Skip for Testing environment (InMemory database)
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<WebPScannerDbContext>();
    dbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Early so RemoteIpAddress reflects real client IP
if (securitySection.ForwardedHeadersEnabled)
{
    app.UseForwardedHeaders();
}

app.UseSecurityHeaders();

// Must come before UseStaticFiles
var securityOptions = builder.Configuration.GetSection(WebPScanner.Core.Configuration.SecurityOptions.SectionName).Get<WebPScanner.Core.Configuration.SecurityOptions>();
if (securityOptions?.EnforceHttps == true)
{
    app.UseHttpsRedirection();
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }
}

app.UseRateLimiter();
app.UseStaticFiles();

app.UseFastEndpoints(c =>
{
    c.Serializer.Options.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
});

app.MapHub<ScanProgressHub>("/hubs/scanprogress");
app.MapFallbackToFile("index.html");

app.Run();
return;

static bool IsIpExemptFromRateLimiting(string clientIp, string[] exemptIps)
{
	if (exemptIps.Length == 0) return false;

	foreach (var exemptIp in exemptIps)
	{
		if (string.IsNullOrEmpty(exemptIp)) continue;

		if (exemptIp.Equals(clientIp, StringComparison.OrdinalIgnoreCase))
			return true;

		if (exemptIp.Contains('/') && IpRangeHelper.IsInCidrRange(clientIp, exemptIp))
			return true;
	}

	return false;
}

// Make the implicit Program class public so it can be referenced by integration tests
// ReSharper disable once ClassNeverInstantiated.Global
#pragma warning disable ASP0027
public partial class Program;
#pragma warning restore ASP0027
