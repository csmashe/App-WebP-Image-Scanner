using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WebPScanner.Core.Interfaces;
using WebPScanner.Core.Models;
using WebPScanner.Core.Utilities;

namespace WebPScanner.Core.Services;

/// <summary>
/// Service for generating PDF reports using QuestPDF.
/// </summary>
public class PdfReportService : IPdfReportService
{
    // Brand colors - Maroon theme
    private const string PrimaryColor = "#722F37"; // Maroon
    private const string TextPrimary = "#1F2937";
    private const  string TextSecondary = "#6B7280";
    private const string BackgroundLight = "#F9FAFB";
    private const  string SuccessColor = "#10B981";
    private const  string WarningColor = "#F59E0B";

    static PdfReportService()
    {
        // Configure QuestPDF license for community use
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc />
    public byte[] GenerateReport(PdfReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        var document = CreateDocument(reportData);
        return document.GeneratePdf();
    }

    /// <inheritdoc />
    public void GenerateReportToStream(PdfReportData reportData, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(reportData);
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanWrite)
        {
            throw new ArgumentException("Stream must be writable.", nameof(stream));
        }

        var document = CreateDocument(reportData);
        document.GeneratePdf(stream);
    }

    private static IDocument CreateDocument(PdfReportData reportData)
    {
        return Document.Create(container =>
        {
            // Cover Page
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(0);

                page.Content().Column(column =>
                {
                    // Header gradient background
                    column.Item().Height(300).Background(PrimaryColor).Padding(40).Column(header =>
                    {
                        header.Item().PaddingTop(40);
                        header.Item().Text("WebP Image Scanner").FontSize(36).Bold().FontColor(Colors.White);
                        header.Item().PaddingTop(10);
                        header.Item().Text("Website Optimization Report").FontSize(20).FontColor(Colors.White).Light();
                    });

                    // Website info
                    column.Item().Padding(40).Column(info =>
                    {
                        info.Item().PaddingBottom(30);

                        info.Item().Text("Scanned Website").FontSize(12).FontColor(TextSecondary);
                        info.Item().Text(TruncateUrl(reportData.TargetUrl, 60)).FontSize(18).Bold().FontColor(TextPrimary);

                        info.Item().PaddingTop(20);
                        info.Item().Text("Scan Date").FontSize(12).FontColor(TextSecondary);
                        // Ensure the timestamp is in UTC before formatting with UTC label
                        var scanDateUtc = reportData.ScanDate.Kind == DateTimeKind.Utc
                            ? reportData.ScanDate
                            : reportData.ScanDate.ToUniversalTime();
                        info.Item().Text(scanDateUtc.ToString("MMMM dd, yyyy 'at' HH:mm 'UTC'")).FontSize(16).FontColor(TextPrimary);

                        info.Item().PaddingTop(20);
                        info.Item().Text("Scan ID").FontSize(12).FontColor(TextSecondary);
                        info.Item().Text(reportData.ScanId.ToString()).FontSize(12).FontColor(TextSecondary);
                    });

                    // Quick stats
                    column.Item().PaddingHorizontal(40).PaddingBottom(40).Row(row =>
                    {
                        row.RelativeItem().Background(BackgroundLight).Padding(15).Column(stat =>
                        {
                            stat.Item().Text(reportData.PagesScanned.ToString()).FontSize(28).Bold().FontColor(PrimaryColor);
                            stat.Item().Text("Pages Scanned").FontSize(11).FontColor(TextSecondary);
                        });
                        row.ConstantItem(10);
                        row.RelativeItem().Background(BackgroundLight).Padding(15).Column(stat =>
                        {
                            stat.Item().Text(reportData.SavingsSummary.ConvertibleImages.ToString()).FontSize(28).Bold().FontColor(WarningColor);
                            stat.Item().Text("Images to Convert").FontSize(11).FontColor(TextSecondary);
                        });
                        row.ConstantItem(10);
                        row.RelativeItem().Background(BackgroundLight).Padding(15).Column(stat =>
                        {
                            stat.Item().Text(FormatPercentage(reportData.SavingsSummary.TotalSavingsPercentage)).FontSize(28).Bold().FontColor(SuccessColor);
                            stat.Item().Text("Potential Savings").FontSize(11).FontColor(TextSecondary);
                        });
                    });
                });

                page.Footer().AlignCenter().Padding(20).Text("Generated by WebP Image Scanner - https://webpscanner.com")
                    .FontSize(9).FontColor(TextSecondary);
            });

            // Executive Summary Page
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                page.Header().Column(header =>
                {
                    header.Item().Text("Executive Summary").FontSize(24).Bold().FontColor(TextPrimary);
                    header.Item().PaddingTop(5);
                    header.Item().LineHorizontal(2).LineColor(PrimaryColor);
                });

                page.Content().PaddingTop(20).Column(column =>
                {
                    // Summary text
                    column.Item().Text(text =>
                    {
                        text.Span("Our scan of ").FontColor(TextPrimary);
                        text.Span(GetHostFromUrl(reportData.TargetUrl)).Bold().FontColor(PrimaryColor);
                        text.Span(" analyzed ").FontColor(TextPrimary);
                        text.Span(reportData.PagesScanned.ToString()).Bold();
                        text.Span(" pages and found ").FontColor(TextPrimary);
                        text.Span(reportData.SavingsSummary.ConvertibleImages.ToString()).Bold().FontColor(WarningColor);
                        text.Span(" images that could be converted to WebP format for better performance.").FontColor(TextPrimary);
                    });

                    if (reportData.ReachedPageLimit)
                    {
                        column.Item().PaddingTop(10).Background("#FEF3C7").Padding(10).Text(
                            "Note: The scan was limited to the maximum page count. There may be additional images on unscanned pages.")
                            .FontSize(10).FontColor("#92400E");
                    }

                    column.Item().PaddingTop(30);

                    // Statistics cards
                    column.Item().Text("Key Statistics").FontSize(16).Bold().FontColor(TextPrimary);
                    column.Item().PaddingTop(15);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor("#E5E7EB").Padding(15).Column(card =>
                        {
                            card.Item().Text("Total Original Size").FontSize(11).FontColor(TextSecondary);
                            card.Item().Text(ByteFormatUtility.FormatBytes(reportData.SavingsSummary.TotalOriginalSize)).FontSize(20).Bold().FontColor(TextPrimary);
                        });
                        row.ConstantItem(15);
                        row.RelativeItem().Border(1).BorderColor("#E5E7EB").Padding(15).Column(card =>
                        {
                            card.Item().Text("Estimated WebP Size").FontSize(11).FontColor(TextSecondary);
                            card.Item().Text(ByteFormatUtility.FormatBytes(reportData.SavingsSummary.TotalEstimatedWebPSize)).FontSize(20).Bold().FontColor(SuccessColor);
                        });
                        row.ConstantItem(15);
                        row.RelativeItem().Border(1).BorderColor("#E5E7EB").Padding(15).Column(card =>
                        {
                            card.Item().Text("Potential Savings").FontSize(11).FontColor(TextSecondary);
                            card.Item().Text(ByteFormatUtility.FormatBytes(reportData.SavingsSummary.TotalSavingsBytes)).FontSize(20).Bold().FontColor(SuccessColor);
                        });
                    });

                    column.Item().PaddingTop(30);

                    // Breakdown by type
                    if (reportData.SavingsSummary.ByType.Count > 0)
                    {
                        column.Item().Text("Savings by Image Type").FontSize(16).Bold().FontColor(TextPrimary);
                        column.Item().PaddingTop(15);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn();
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn();
                            });

                            // Header
                            table.Header(header =>
                            {
                                header.Cell().Background(PrimaryColor).Padding(8).Text("Type").FontSize(10).Bold().FontColor(Colors.White);
                                header.Cell().Background(PrimaryColor).Padding(8).Text("Count").FontSize(10).Bold().FontColor(Colors.White);
                                header.Cell().Background(PrimaryColor).Padding(8).Text("Original").FontSize(10).Bold().FontColor(Colors.White);
                                header.Cell().Background(PrimaryColor).Padding(8).Text("Est. WebP").FontSize(10).Bold().FontColor(Colors.White);
                                header.Cell().Background(PrimaryColor).Padding(8).Text("Savings").FontSize(10).Bold().FontColor(Colors.White);
                                header.Cell().Background(PrimaryColor).Padding(8).Text("%").FontSize(10).Bold().FontColor(Colors.White);
                            });

                            var isAlternate = false;
                            foreach (var type in reportData.SavingsSummary.ByType.Values.OrderByDescending(t => t.TotalSavingsBytes))
                            {
                                var bgColor = isAlternate ? BackgroundLight : "#FFFFFF";

                                table.Cell().Background(bgColor).Padding(8).Text(GetFriendlyTypeName(type.MimeType)).FontSize(10);
                                table.Cell().Background(bgColor).Padding(8).Text(type.Count.ToString()).FontSize(10);
                                table.Cell().Background(bgColor).Padding(8).Text(ByteFormatUtility.FormatBytes(type.TotalOriginalSize)).FontSize(10);
                                table.Cell().Background(bgColor).Padding(8).Text(ByteFormatUtility.FormatBytes(type.TotalEstimatedWebPSize)).FontSize(10);
                                table.Cell().Background(bgColor).Padding(8).Text(ByteFormatUtility.FormatBytes(type.TotalSavingsBytes)).FontSize(10).FontColor(SuccessColor);
                                table.Cell().Background(bgColor).Padding(8).Text(FormatPercentage(type.SavingsPercentage)).FontSize(10).FontColor(SuccessColor);

                                isAlternate = !isAlternate;
                            }
                        });
                    }

                    // Disclaimer
                    column.Item().PaddingTop(30);
                    column.Item().Background(BackgroundLight).Padding(15).Text(reportData.SavingsSummary.Disclaimer)
                        .FontSize(9).Italic().FontColor(TextSecondary);
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ").FontSize(9).FontColor(TextSecondary);
                    text.CurrentPageNumber().FontSize(9).FontColor(TextSecondary);
                    text.Span(" of ").FontSize(9).FontColor(TextSecondary);
                    text.TotalPages().FontSize(9).FontColor(TextSecondary);
                });
            });

            // Detailed Findings Pages
            if (reportData.ImageEstimates.Count > 0)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);

                    page.Header().Column(header =>
                    {
                        header.Item().Text("Detailed Findings").FontSize(24).Bold().FontColor(TextPrimary);
                        header.Item().PaddingTop(5);
                        header.Item().LineHorizontal(2).LineColor(PrimaryColor);
                        header.Item().PaddingTop(10);
                        header.Item().Text($"Showing {reportData.ImageEstimates.Count} images sorted by potential savings (highest first)")
                            .FontSize(10).FontColor(TextSecondary);
                    });

                    page.Content().PaddingTop(15).Column(column =>
                    {
                        var index = 0;
                        foreach (var image in reportData.ImageEstimates.OrderByDescending(i => i.SavingsBytes))
                        {
                            var currentIndex = index; // Capture for closure
                            var bgColor = currentIndex % 2 == 0 ? "#FFFFFF" : BackgroundLight;

                            // Normalize page list - ensure we always have at least one entry
                            var rawPageUrls = reportData.ImageToPagesMap.GetValueOrDefault(image.Url);
                            var pageUrls = rawPageUrls is { Count: > 0 } ? rawPageUrls : ["Unknown"];
                            var firstPage = pageUrls.FirstOrDefault() ?? "Unknown";
                            var additionalPageCount = Math.Max(0, pageUrls.Count - 1);

                            column.Item().Background(bgColor).Padding(10).Column(imageBlock =>
                            {
                                // Image filename (extracted from URL)
                                var filename = GetFilenameFromUrl(image.Url);
                                imageBlock.Item().Text(text =>
                                {
                                    text.Span($"{currentIndex + 1}. ").FontSize(10).Bold().FontColor(TextSecondary);
                                    text.Span(filename).FontSize(10).Bold().FontColor(TextPrimary);
                                    text.Span($"  ({GetFriendlyTypeName(image.OriginalMimeType)})").FontSize(9).FontColor(TextSecondary);
                                });

                                // Full image URL
                                imageBlock.Item().PaddingTop(3).Text(text =>
                                {
                                    text.Span("Image: ").FontSize(8).Bold().FontColor(TextSecondary);
                                    text.Span(image.Url).FontSize(8).FontColor(PrimaryColor);
                                });

                                // Page(s) where found
                                imageBlock.Item().PaddingTop(2).Text(text =>
                                {
                                    text.Span("Found on: ").FontSize(8).Bold().FontColor(TextSecondary);
                                    text.Span(firstPage).FontSize(8).FontColor(TextSecondary);
                                    if (additionalPageCount > 0)
                                    {
                                        text.Span($" (and {additionalPageCount} other page{(additionalPageCount > 1 ? "s" : "")})").FontSize(8).Italic().FontColor(TextSecondary);
                                    }
                                });

                                // Size stats in a row
                                imageBlock.Item().PaddingTop(5).Row(row =>
                                {
                                    row.AutoItem().Text($"Original: {ByteFormatUtility.FormatBytes(image.OriginalSize)}").FontSize(9).FontColor(TextPrimary);
                                    row.ConstantItem(20);
                                    row.AutoItem().Text($"WebP Est: {ByteFormatUtility.FormatBytes(image.EstimatedWebPSize)}").FontSize(9).FontColor(TextPrimary);
                                    row.ConstantItem(20);
                                    row.AutoItem().Text($"Savings: {ByteFormatUtility.FormatBytes(image.SavingsBytes)} ({FormatPercentage(image.SavingsPercentage)})").FontSize(9).Bold().FontColor(SuccessColor);
                                });
                            });

                            index++;
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ").FontSize(9).FontColor(TextSecondary);
                        text.CurrentPageNumber().FontSize(9).FontColor(TextSecondary);
                        text.Span(" of ").FontSize(9).FontColor(TextSecondary);
                        text.TotalPages().FontSize(9).FontColor(TextSecondary);
                    });
                });
            }

            // Recommendations Page
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                page.Header().Column(header =>
                {
                    header.Item().Text("Recommendations").FontSize(24).Bold().FontColor(TextPrimary);
                    header.Item().PaddingTop(5);
                    header.Item().LineHorizontal(2).LineColor(PrimaryColor);
                });

                page.Content().PaddingTop(20).Column(column =>
                {
                    // Why WebP section
                    column.Item().Text("Why Convert to WebP?").FontSize(18).Bold().FontColor(TextPrimary);
                    column.Item().PaddingTop(15);

                    column.Item().Row(row =>
                    {
                        row.ConstantItem(8).Background(PrimaryColor).Height(8);
                        row.ConstantItem(10);
                        row.RelativeItem().Column(benefit =>
                        {
                            benefit.Item().Text("Smaller File Sizes").FontSize(12).Bold().FontColor(TextPrimary);
                            benefit.Item().Text("WebP images are typically 25-34% smaller than JPEG and 26% smaller than PNG for equivalent quality.")
                                .FontSize(10).FontColor(TextSecondary);
                        });
                    });

                    column.Item().PaddingTop(15);
                    column.Item().Row(row =>
                    {
                        row.ConstantItem(8).Background(PrimaryColor).Height(8);
                        row.ConstantItem(10);
                        row.RelativeItem().Column(benefit =>
                        {
                            benefit.Item().Text("Faster Page Load Times").FontSize(12).Bold().FontColor(TextPrimary);
                            benefit.Item().Text("Smaller images mean faster downloads, improving user experience and Core Web Vitals scores.")
                                .FontSize(10).FontColor(TextSecondary);
                        });
                    });

                    column.Item().PaddingTop(15);
                    column.Item().Row(row =>
                    {
                        row.ConstantItem(8).Background(PrimaryColor).Height(8);
                        row.ConstantItem(10);
                        row.RelativeItem().Column(benefit =>
                        {
                            benefit.Item().Text("Reduced Bandwidth Costs").FontSize(12).Bold().FontColor(TextPrimary);
                            benefit.Item().Text("Lower file sizes reduce CDN and hosting bandwidth costs, especially for high-traffic sites.")
                                .FontSize(10).FontColor(TextSecondary);
                        });
                    });

                    column.Item().PaddingTop(15);
                    column.Item().Row(row =>
                    {
                        row.ConstantItem(8).Background(PrimaryColor).Height(8);
                        row.ConstantItem(10);
                        row.RelativeItem().Column(benefit =>
                        {
                            benefit.Item().Text("Universal Browser Support").FontSize(12).Bold().FontColor(TextPrimary);
                            benefit.Item().Text("WebP is now supported by all major browsers including Chrome, Firefox, Safari, and Edge.")
                                .FontSize(10).FontColor(TextSecondary);
                        });
                    });

                    column.Item().PaddingTop(30);

                    // How to convert section
                    column.Item().Text("How to Convert Your Images").FontSize(18).Bold().FontColor(TextPrimary);
                    column.Item().PaddingTop(15);

                    column.Item().Background(BackgroundLight).Padding(20).Column(howTo =>
                    {
                        howTo.Item().Text("1. Use WebP Image Scanner's Conversion Feature").FontSize(12).Bold().FontColor(PrimaryColor);
                        howTo.Item().Text("When submitting a scan at webpscanner.com, check the \"Convert images to WebP\" option. We'll convert all detected images and provide a download link with your optimized WebP files.")
                            .FontSize(10).FontColor(TextSecondary);

                        howTo.Item().PaddingTop(15);
                        howTo.Item().Text("2. Use a CDN with Automatic Conversion").FontSize(12).Bold().FontColor(TextPrimary);
                        howTo.Item().Text("Services like Cloudflare, imgix, or CloudImage can automatically serve WebP versions to supported browsers.")
                            .FontSize(10).FontColor(TextSecondary);

                        howTo.Item().PaddingTop(15);
                        howTo.Item().Text("3. Convert Images at Build Time").FontSize(12).Bold().FontColor(TextPrimary);
                        howTo.Item().Text("Use tools like imagemin, sharp, or cwebp to convert images during your build process.")
                            .FontSize(10).FontColor(TextSecondary);

                        howTo.Item().PaddingTop(15);
                        howTo.Item().Text("4. Implement the <picture> Element").FontSize(12).Bold().FontColor(TextPrimary);
                        howTo.Item().Text("Use HTML <picture> element to serve WebP with fallbacks: <picture><source srcset=\"image.webp\" type=\"image/webp\"><img src=\"image.jpg\"></picture>")
                            .FontSize(10).FontColor(TextSecondary);

                        howTo.Item().PaddingTop(15);
                        howTo.Item().Text("5. Server-Side Content Negotiation").FontSize(12).Bold().FontColor(TextPrimary);
                        howTo.Item().Text("Configure your web server to serve WebP images when the client's Accept header includes 'image/webp'.")
                            .FontSize(10).FontColor(TextSecondary);
                    });

                    column.Item().PaddingTop(30);

                    // Tools section
                    column.Item().Text("Recommended Tools").FontSize(18).Bold().FontColor(TextPrimary);
                    column.Item().PaddingTop(15);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor("#E5E7EB").Padding(15).Column(tool =>
                        {
                            tool.Item().Text("cwebp").FontSize(11).Bold().FontColor(PrimaryColor);
                            tool.Item().Text("Official Google command-line tool for WebP conversion").FontSize(9).FontColor(TextSecondary);
                        });
                        row.ConstantItem(10);
                        row.RelativeItem().Border(1).BorderColor("#E5E7EB").Padding(15).Column(tool =>
                        {
                            tool.Item().Text("Squoosh").FontSize(11).Bold().FontColor(PrimaryColor);
                            tool.Item().Text("Browser-based image optimizer by Google Chrome team").FontSize(9).FontColor(TextSecondary);
                        });
                    });

                    column.Item().PaddingTop(10);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).BorderColor("#E5E7EB").Padding(15).Column(tool =>
                        {
                            tool.Item().Text("Sharp (Node.js)").FontSize(11).Bold().FontColor(PrimaryColor);
                            tool.Item().Text("High-performance image processing library for Node.js").FontSize(9).FontColor(TextSecondary);
                        });
                        row.ConstantItem(10);
                        row.RelativeItem().Border(1).BorderColor("#E5E7EB").Padding(15).Column(tool =>
                        {
                            tool.Item().Text("Pillow (Python)").FontSize(11).Bold().FontColor(PrimaryColor);
                            tool.Item().Text("Python imaging library with WebP support").FontSize(9).FontColor(TextSecondary);
                        });
                    });
                });

                page.Footer().Column(footer =>
                {
                    footer.Item().LineHorizontal(1).LineColor("#E5E7EB");
                    footer.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Text("WebP Image Scanner - Free & Open Source").FontSize(9).FontColor(TextSecondary);
                        row.RelativeItem().AlignRight().Text(text =>
                        {
                            text.Span("Page ").FontSize(9).FontColor(TextSecondary);
                            text.CurrentPageNumber().FontSize(9).FontColor(TextSecondary);
                            text.Span(" of ").FontSize(9).FontColor(TextSecondary);
                            text.TotalPages().FontSize(9).FontColor(TextSecondary);
                        });
                    });
                });
            });
        });
    }

    private static string FormatPercentage(double percentage)
    {
        return $"{percentage:F1}%";
    }

    private static string TruncateUrl(string url, int maxLength)
    {
        if (string.IsNullOrEmpty(url) || url.Length <= maxLength)
            return url;
        // For very small maxLength values, just truncate without ellipsis
        if (maxLength <= 3)
            return url[..maxLength];
        return url[..(maxLength - 3)] + "...";
    }

    private static string GetHostFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host;
        }
        catch
        {
            return url;
        }
    }

    private static string GetFilenameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var filename = Path.GetFileName(path);
            // URL decode the filename (e.g., %20 -> space)
            return Uri.UnescapeDataString(string.IsNullOrEmpty(filename) ? path : filename);
        }
        catch
        {
            // Fallback: try to get everything after the last /
            var lastSlash = url.LastIndexOf('/');
            return lastSlash >= 0 && lastSlash < url.Length - 1
                ? url[(lastSlash + 1)..]
                : url;
        }
    }

    private static string GetFriendlyTypeName(string mimeType)
    {
        if (string.IsNullOrEmpty(mimeType))
            return "Unknown";

        return mimeType.ToLowerInvariant() switch
        {
            "image/jpeg" => "JPEG",
            "image/jpg" => "JPEG",
            "image/png" => "PNG",
            "image/gif" => "GIF",
            "image/bmp" => "BMP",
            "image/tiff" => "TIFF",
            "image/webp" => "WebP",
            "image/svg+xml" => "SVG",
            "image/avif" => "AVIF",
            _ => mimeType.Replace("image/", "").ToUpperInvariant()
        };
    }
}
