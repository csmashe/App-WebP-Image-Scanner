using WebPScanner.Core.Models;

namespace WebPScanner.Core.Interfaces;

/// <summary>
/// Service for generating PDF reports from scan results.
/// </summary>
public interface IPdfReportService
{
    /// <summary>
    /// Generates a PDF report from the scan data.
    /// </summary>
    /// <param name="reportData">The data to include in the report.</param>
    /// <returns>The PDF document as a byte array.</returns>
    byte[] GenerateReport(PdfReportData reportData);

    /// <summary>
    /// Generates a PDF report and writes it to a stream.
    /// </summary>
    /// <param name="reportData">The data to include in the report.</param>
    /// <param name="stream">The stream to write the PDF to.</param>
    void GenerateReportToStream(PdfReportData reportData, Stream stream);
}
