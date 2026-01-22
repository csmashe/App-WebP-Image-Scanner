using WebPScanner.Core.Entities;
using WebPScanner.Core.Interfaces;
using WebPScanner.Core.Models;

namespace WebPScanner.Core.Services;

/// <summary>
/// Service for estimating WebP conversion savings.
/// </summary>
public class SavingsEstimatorService : ISavingsEstimatorService
{
    /// <inheritdoc />
    public SavingsSummary CalculateSavingsSummary(IEnumerable<DiscoveredImage> images)
    {
        ArgumentNullException.ThrowIfNull(images);
        var imageList = images.ToList();
        var summary = new SavingsSummary
        {
            TotalImages = imageList.Count
        };

        if (imageList.Count == 0)
            return summary;

        var byType = new Dictionary<string, TypeSavingsSummary>(StringComparer.OrdinalIgnoreCase);

        foreach (var image in imageList.Where(image => image.FileSize > 0))
        {
	        summary.ConvertibleImages++;
	        summary.TotalOriginalSize += image.FileSize;
	        summary.TotalEstimatedWebPSize += image.EstimatedWebPSize;

	        var normalizedMimeType = NormalizeMimeType(image.MimeType);

	        if (!byType.TryGetValue(normalizedMimeType, out var typeSummary))
	        {
		        typeSummary = new TypeSavingsSummary
		        {
			        MimeType = normalizedMimeType
		        };
		        byType[normalizedMimeType] = typeSummary;
	        }

	        typeSummary.Count++;
	        typeSummary.TotalOriginalSize += image.FileSize;
	        typeSummary.TotalEstimatedWebPSize += image.EstimatedWebPSize;
        }

        summary.TotalSavingsBytes = Math.Max(0, summary.TotalOriginalSize - summary.TotalEstimatedWebPSize);
        summary.TotalSavingsPercentage = summary.TotalOriginalSize > 0
            ? Math.Round((double)summary.TotalSavingsBytes / summary.TotalOriginalSize * 100, 2)
            : 0;

        foreach (var typeSummary in byType.Values)
        {
            typeSummary.TotalSavingsBytes = Math.Max(0, typeSummary.TotalOriginalSize - typeSummary.TotalEstimatedWebPSize);
            typeSummary.SavingsPercentage = typeSummary.TotalOriginalSize > 0
                ? Math.Round((double)typeSummary.TotalSavingsBytes / typeSummary.TotalOriginalSize * 100, 2)
                : 0;
            typeSummary.ConversionRatio = typeSummary.TotalOriginalSize > 0
                ? (double)typeSummary.TotalEstimatedWebPSize / typeSummary.TotalOriginalSize
                : 0;
        }

        summary.ByType = byType;

        return summary;
    }

    private static ImageSavingsEstimate CalculateImageSavings(DiscoveredImage image)
    {
        var savingsBytes = Math.Max(0, image.FileSize - image.EstimatedWebPSize);
        var savingsPercentage = image.FileSize > 0
            ? Math.Round((double)savingsBytes / image.FileSize * 100, 2)
            : 0;

        var conversionRatio = image.FileSize > 0
            ? (double)image.EstimatedWebPSize / image.FileSize
            : 0;

        return new ImageSavingsEstimate
        {
            Url = image.ImageUrl,
            OriginalMimeType = image.MimeType,
            OriginalSize = image.FileSize,
            EstimatedWebPSize = image.EstimatedWebPSize,
            SavingsBytes = savingsBytes,
            SavingsPercentage = savingsPercentage,
            ConversionRatio = conversionRatio
        };
    }

    /// <inheritdoc />
    public List<ImageSavingsEstimate> CalculateImageSavings(IEnumerable<DiscoveredImage> images)
    {
        ArgumentNullException.ThrowIfNull(images);
        return images.Select(CalculateImageSavings).ToList();
    }

    /// <summary>
    /// Normalizes a MIME type by removing parameters and converting to lowercase.
    /// </summary>
    private static string NormalizeMimeType(string mimeType)
    {
	    return string.IsNullOrWhiteSpace(mimeType) ? "unknown" : mimeType.Split(';')[0].Trim().ToLowerInvariant();
    }
}
