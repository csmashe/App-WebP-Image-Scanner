using Microsoft.EntityFrameworkCore;
using WebPScanner.Core.Entities;
using WebPScanner.Core.Interfaces;
using WebPScanner.Data.Context;

namespace WebPScanner.Data.Repositories;

/// <summary>
/// Repository for managing crawl checkpoints.
/// </summary>
public class CrawlCheckpointRepository : ICrawlCheckpointRepository
{
    private readonly WebPScannerDbContext _context;

    public CrawlCheckpointRepository(WebPScannerDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<CrawlCheckpoint?> GetByScanJobIdAsync(Guid scanJobId, CancellationToken cancellationToken = default)
    {
        return await _context.CrawlCheckpoints
            .FirstOrDefaultAsync(c => c.ScanJobId == scanJobId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveCheckpointAsync(CrawlCheckpoint checkpoint, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Use provider-agnostic EF Core logic for compatibility with all database providers (including InMemory for tests)
        var existing = await _context.CrawlCheckpoints
            .FirstOrDefaultAsync(c => c.ScanJobId == checkpoint.ScanJobId, cancellationToken);

        if (existing != null)
        {
            // Update existing checkpoint
            existing.VisitedUrlsJson = checkpoint.VisitedUrlsJson;
            existing.PendingUrlsJson = checkpoint.PendingUrlsJson;
            existing.PagesVisited = checkpoint.PagesVisited;
            existing.PagesDiscovered = checkpoint.PagesDiscovered;
            existing.NonWebPImagesFound = checkpoint.NonWebPImagesFound;
            existing.CurrentUrl = checkpoint.CurrentUrl;
            existing.UpdatedAt = now;

            _context.Update(existing);
        }
        else
        {
            // Create new checkpoint, honoring caller-provided Id if non-empty
            checkpoint.Id = checkpoint.Id != Guid.Empty ? checkpoint.Id : Guid.NewGuid();
            checkpoint.CreatedAt = now;
            checkpoint.UpdatedAt = now;

            _context.Add(checkpoint);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteByScanJobIdAsync(Guid scanJobId, CancellationToken cancellationToken = default)
    {
        var checkpoint = await _context.CrawlCheckpoints
            .FirstOrDefaultAsync(c => c.ScanJobId == scanJobId, cancellationToken);

        if (checkpoint != null)
        {
            _context.CrawlCheckpoints.Remove(checkpoint);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
