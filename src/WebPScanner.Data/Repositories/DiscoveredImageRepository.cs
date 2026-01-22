using Microsoft.EntityFrameworkCore;
using WebPScanner.Core.Entities;
using WebPScanner.Core.Interfaces;
using WebPScanner.Data.Context;

namespace WebPScanner.Data.Repositories;

public class DiscoveredImageRepository : IDiscoveredImageRepository
{
    private readonly WebPScannerDbContext _context;

    public DiscoveredImageRepository(WebPScannerDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DiscoveredImage>> GetByScanJobIdAsync(Guid scanJobId, CancellationToken cancellationToken = default)
    {
        return await _context.DiscoveredImages
            .Where(i => i.ScanJobId == scanJobId)
            .OrderBy(i => i.DiscoveredAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<DiscoveredImage>> GetByScanJobIdOrderedBySavingsAsync(Guid scanJobId, CancellationToken cancellationToken = default)
    {
        return await _context.DiscoveredImages
            .Where(i => i.ScanJobId == scanJobId)
            .OrderByDescending(i => i.PotentialSavingsPercent)
            .ThenByDescending(i => i.FileSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountByScanJobIdAsync(Guid scanJobId, CancellationToken cancellationToken = default)
    {
        return await _context.DiscoveredImages
            .CountAsync(i => i.ScanJobId == scanJobId, cancellationToken);
    }

    public async Task<DiscoveredImage> AddAsync(DiscoveredImage image, CancellationToken cancellationToken = default)
    {
        _context.DiscoveredImages.Add(image);
        await _context.SaveChangesAsync(cancellationToken);
        return image;
    }

    public async Task UpdatePageUrlsAsync(Guid scanJobId, Dictionary<string, List<string>> imageToPagesMap, CancellationToken cancellationToken = default)
    {
        var images = await _context.DiscoveredImages
            .Where(i => i.ScanJobId == scanJobId)
            .ToListAsync(cancellationToken);

        foreach (var image in images)
        {
            if (imageToPagesMap.TryGetValue(image.ImageUrl, out var pageUrls) && pageUrls.Count > 0)
            {
                image.SetPageUrls(pageUrls);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
