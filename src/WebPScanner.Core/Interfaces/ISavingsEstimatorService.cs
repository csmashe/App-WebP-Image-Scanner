using WebPScanner.Core.Entities;
using WebPScanner.Core.Models;

namespace WebPScanner.Core.Interfaces;

/// <summary>
/// Service for estimating WebP conversion savings.
/// </summary>
public interface ISavingsEstimatorService
{
    /// <summary>
    /// Calculates aggregate savings statistics for a collection of discovered images.
    /// Uses pre-calculated EstimatedWebPSize from the entities.
    /// </summary>
    /// <param name="images">The collection of discovered images from the database.</param>
    /// <returns>The aggregate savings summary.</returns>
    SavingsSummary CalculateSavingsSummary(IEnumerable<DiscoveredImage> images);

    /// <summary>
    /// Calculates detailed savings information for a collection of discovered images.
    /// </summary>
    /// <param name="images">The collection of discovered images from the database.</param>
    /// <returns>List of detailed savings estimates.</returns>
    List<ImageSavingsEstimate> CalculateImageSavings(IEnumerable<DiscoveredImage> images);
}
