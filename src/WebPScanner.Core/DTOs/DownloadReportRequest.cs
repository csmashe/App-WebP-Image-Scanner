// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace WebPScanner.Core.DTOs;

/// <summary>
/// Request to download a PDF report for a completed scan.
/// </summary>
public class DownloadReportRequest
{
    /// <summary>
    /// The unique identifier of the scan to generate a report for.
    /// </summary>
    public Guid ScanId { get; set; }
}
