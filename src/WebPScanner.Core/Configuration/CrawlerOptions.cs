// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace WebPScanner.Core.Configuration;

/// <summary>
/// Configuration options for the website crawler.
/// </summary>
public class CrawlerOptions
{
    public const string SectionName = "Crawler";

    /// <summary>
    /// Maximum number of pages to crawl per scan.
    /// </summary>
    public int MaxPagesPerScan { get; init; } = 1000;

    /// <summary>
    /// Timeout in seconds for each page load.
    /// </summary>
    public int PageTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Network idle timeout in milliseconds for SPA support.
    /// </summary>
    public int NetworkIdleTimeoutMs { get; init; } = 2000;

    /// <summary>
    /// Delay in milliseconds between page requests.
    /// </summary>
    public int DelayBetweenPagesMs { get; init; } = 500;

    /// <summary>
    /// Maximum number of retry attempts for failed page loads.
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Whether to respect robots.txt directives.
    /// </summary>
    public bool RespectRobotsTxt { get; init; } = true;

    /// <summary>
    /// Path to the Chromium executable. If null, PuppeteerSharp will download it.
    /// </summary>
    public string? ChromiumPath { get; init; }

    /// <summary>
    /// User agent string to use for crawling.
    /// Uses a real Chrome User-Agent to ensure servers do proper content negotiation
    /// (some servers check User-Agent in addition to Accept headers for WebP).
    /// </summary>
    public string UserAgent { get; init; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    /// <summary>
    /// Whether to run Chromium in sandbox mode.
    /// Default is true for security. In Docker environments, this should be init to false
    /// via environment variable (Crawler__EnableSandbox=false) since Docker provides
    /// container-level isolation. Enabling sandbox in Docker requires SYS_ADMIN capability
    /// or host support for unprivileged user namespaces.
    /// </summary>
    public bool EnableSandbox { get; init; } = true;

    /// <summary>
    /// Whether to restrict network requests to only the target domain.
    /// </summary>
    public bool RestrictToTargetDomain { get; init; } = true;

    /// <summary>
    /// Maximum total request size in bytes allowed per page (to prevent memory exhaustion).
    /// </summary>
    public long MaxRequestSizeBytes { get; init; } = 50 * 1024 * 1024; // 50MB

    /// <summary>
    /// Maximum number of network requests allowed per page.
    /// </summary>
    public int MaxRequestsPerPage { get; init; } = 500;

    /// <summary>
    /// Whether to block known tracking and analytics domains.
    /// </summary>
    public bool BlockTrackingDomains { get; init; } = true;

    /// <summary>
    /// Additional domains to allow even when RestrictToTargetDomain is true (e.g., CDN domains).
    /// </summary>
    public string[] AllowedExternalDomains { get; init; } = [];

    /// <summary>
    /// Whether to enable checkpointing for scan resume functionality.
    /// </summary>
    public bool EnableCheckpointing { get; init; } = true;

    /// <summary>
    /// Number of pages between checkpoint saves.
    /// Lower values mean less lost work on restart but more database writes.
    /// </summary>
    public int CheckpointIntervalPages { get; init; } = 10;
}
