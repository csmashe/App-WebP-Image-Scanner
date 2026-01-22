namespace WebPScanner.Core.Models;

/// <summary>
/// Parsed robots.txt rules for a website.
/// </summary>
internal class RobotsRules
{
    /// <summary>
    /// Paths that are disallowed for crawling.
    /// </summary>
    public List<string> DisallowedPaths { get; } = [];

    /// <summary>
    /// Paths that are explicitly allowed for crawling.
    /// </summary>
    public List<string> AllowedPaths { get; } = [];
}
