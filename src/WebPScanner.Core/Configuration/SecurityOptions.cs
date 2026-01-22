namespace WebPScanner.Core.Configuration;

/// <summary>
/// Configuration options for security and rate limiting.
/// </summary>
public class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>
    /// Maximum number of requests per minute per IP address.
    /// </summary>
    public int MaxRequestsPerMinute { get; init; } = 100;

    /// <summary>
    /// Whether to enforce HTTPS in production (redirect HTTP to HTTPS).
    /// </summary>
    public bool EnforceHttps { get; init; } = true;

    /// <summary>
    /// Maximum scan duration in minutes before timeout.
    /// </summary>
    public int MaxScanDurationMinutes { get; init; } = 10;

    /// <summary>
    /// Maximum memory usage in MB per scan before termination.
    /// </summary>
    public int MaxMemoryPerScanMb { get; init; } = 512;

    /// <summary>
    /// List of IP addresses or CIDR ranges that are exempt from rate limiting.
    /// </summary>
    public string[] RateLimitExemptIps { get; init; } = [];

    /// <summary>
    /// Whether to enable request body size limits.
    /// </summary>
    public bool EnableRequestSizeLimit { get; init; } = true;

    /// <summary>
    /// Maximum request body size in bytes.
    /// </summary>
    public int MaxRequestBodySizeBytes { get; init; } = 1024 * 100; // 100KB

    /// <summary>
    /// Whether to enable forwarded headers processing for proxy support.
    /// When enabled, X-Forwarded-For and X-Forwarded-Proto headers from trusted proxies
    /// will be used to determine the client IP and protocol.
    /// SECURITY: Only enable this when running behind a trusted reverse proxy.
    /// </summary>
    public bool ForwardedHeadersEnabled { get; init; }

    /// <summary>
    /// List of trusted proxy IP addresses or CIDR ranges.
    /// Only proxies in this list will have their X-Forwarded-* headers trusted.
    /// Required when ForwardedHeadersEnabled is true.
    /// Examples: ["10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16", "127.0.0.1"]
    /// </summary>
    public string[] TrustedProxies { get; init; } = [];

    /// <summary>
    /// Maximum number of proxy hops to process from X-Forwarded-For header.
    /// Set to the number of proxies between clients and your server.
    /// Default is 1 for a single reverse proxy.
    /// </summary>
    public int ForwardLimit { get; init; } = 1;
}
