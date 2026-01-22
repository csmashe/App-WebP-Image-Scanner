namespace WebPScanner.Core.Models;

/// <summary>
/// Data model for generating a PDF report.
/// </summary>
public class PdfReportData
{
    /// <summary>
    /// The scan ID (GUID).
    /// </summary>
    public Guid ScanId { get; init; }

    /// <summary>
    /// The URL that was scanned.
    /// </summary>
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>
    /// The date and time when the scan was completed.
    /// </summary>
    public DateTime ScanDate { get; init; }

    /// <summary>
    /// Total number of pages scanned.
    /// </summary>
    public int PagesScanned { get; init; }

    /// <summary>
    /// Total number of pages discovered.
    /// </summary>
    public int PagesDiscovered { get; init; }

    /// <summary>
    /// Total crawl duration.
    /// </summary>
    public TimeSpan CrawlDuration { get; init; }

    /// <summary>
    /// Whether the crawl was stopped due to reaching the page limit.
    /// </summary>
    public bool ReachedPageLimit { get; set; }

    /// <summary>
    /// Savings summary with aggregate statistics.
    /// </summary>
    public SavingsSummary SavingsSummary { get; init; } = new();

    /// <summary>
    /// Individual image savings estimates, sorted by potential savings descending.
    /// </summary>
    public List<ImageSavingsEstimate> ImageEstimates { get; init; } = [];

    /// <summary>
    /// Map of image URL to all page URLs where it was found.
    /// </summary>
    public Dictionary<string, List<string>> ImageToPagesMap { get; init; } = new();
}
