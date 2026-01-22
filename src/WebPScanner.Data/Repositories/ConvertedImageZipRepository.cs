using Microsoft.EntityFrameworkCore;
using WebPScanner.Core.Entities;
using WebPScanner.Core.Interfaces;
using WebPScanner.Data.Context;

namespace WebPScanner.Data.Repositories;

/// <summary>
/// Repository implementation for ConvertedImageZip entities.
/// </summary>
public class ConvertedImageZipRepository : IConvertedImageZipRepository
{
    private readonly WebPScannerDbContext _context;

    public ConvertedImageZipRepository(WebPScannerDbContext context)
    {
        _context = context;
    }

    public async Task<ConvertedImageZip> AddAsync(ConvertedImageZip zip, CancellationToken cancellationToken = default)
    {
        _context.ConvertedImageZips.Add(zip);
        await _context.SaveChangesAsync(cancellationToken);
        return zip;
    }

    public async Task<ConvertedImageZip?> GetByDownloadIdAsync(Guid downloadId, CancellationToken cancellationToken = default)
    {
        return await _context.ConvertedImageZips
            .FirstOrDefaultAsync(z => z.DownloadId == downloadId, cancellationToken);
    }

    public async Task<ConvertedImageZip?> GetByScanIdAsync(Guid scanId, CancellationToken cancellationToken = default)
    {
        return await _context.ConvertedImageZips
            .FirstOrDefaultAsync(z => z.ScanJobId == scanId, cancellationToken);
    }

    public async Task<IEnumerable<ConvertedImageZip>> GetExpiredZipsAsync(DateTime expiryTime, int maxCount = 100, CancellationToken cancellationToken = default)
    {
        return await _context.ConvertedImageZips
            .Where(z => z.ExpiresAt <= expiryTime)
            .OrderBy(z => z.ExpiresAt)
            .Take(maxCount)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteRangeAsync(IEnumerable<Guid> downloadIds, CancellationToken cancellationToken = default)
    {
        var idList = downloadIds.ToList();
        if (idList.Count == 0) return;

        var zips = await _context.ConvertedImageZips
            .Where(z => idList.Contains(z.DownloadId))
            .ToListAsync(cancellationToken);

        if (zips.Count > 0)
        {
            _context.ConvertedImageZips.RemoveRange(zips);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
