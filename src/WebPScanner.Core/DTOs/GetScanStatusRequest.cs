// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace WebPScanner.Core.DTOs;

/// <summary>
/// Request to get the current status of a scan.
/// </summary>
public class GetScanStatusRequest
{
    /// <summary>
    /// The unique identifier of the scan.
    /// </summary>
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    public string ScanId { get; set; } = string.Empty;
}
