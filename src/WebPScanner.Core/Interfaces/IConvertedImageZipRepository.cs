using WebPScanner.Core.Entities;

namespace WebPScanner.Core.Interfaces;

/// <summary>
/// Repository interface for ConvertedImageZip entities.
/// </summary>
public interface IConvertedImageZipRepository
{
    /// <summary>
    /// Adds a new converted image zip record.
    /// </summary>
    Task<ConvertedImageZip> AddAsync(ConvertedImageZip zip, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a zip by its download ID.
    /// </summary>
    Task<ConvertedImageZip?> GetByDownloadIdAsync(Guid downloadId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a zip by the scan ID it was generated for.
    /// </summary>
    Task<ConvertedImageZip?> GetByScanIdAsync(Guid scanId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all expired zips (where ExpiresAt is before the specified time).
    /// </summary>
    Task<IEnumerable<ConvertedImageZip>> GetExpiredZipsAsync(DateTime expiryTime, int maxCount = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple zip records.
    /// </summary>
    Task DeleteRangeAsync(IEnumerable<Guid> downloadIds, CancellationToken cancellationToken = default);
}
