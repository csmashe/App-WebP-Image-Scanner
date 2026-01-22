using WebPScanner.Core.Entities;

namespace WebPScanner.Core.Interfaces;

/// <summary>
/// Repository for persisting and querying discovered images from scan jobs.
/// </summary>
public interface IDiscoveredImageRepository
{
    /// <summary>
    /// Gets all discovered images for a scan job.
    /// </summary>
    Task<IEnumerable<DiscoveredImage>> GetByScanJobIdAsync(Guid scanJobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all discovered images for a scan job, ordered by potential savings descending.
    /// </summary>
    Task<IEnumerable<DiscoveredImage>> GetByScanJobIdOrderedBySavingsAsync(Guid scanJobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of discovered images for a scan job.
    /// </summary>
    Task<int> GetCountByScanJobIdAsync(Guid scanJobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new discovered image.
    /// </summary>
    Task<DiscoveredImage> AddAsync(DiscoveredImage image, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the page URLs for images after crawl completes.
    /// </summary>
    Task UpdatePageUrlsAsync(Guid scanJobId, Dictionary<string, List<string>> imageToPagesMap, CancellationToken cancellationToken = default);
}
