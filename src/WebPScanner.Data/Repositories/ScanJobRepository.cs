using Microsoft.EntityFrameworkCore;
using WebPScanner.Core.Entities;
using WebPScanner.Core.Enums;
using WebPScanner.Core.Interfaces;
using WebPScanner.Data.Context;

namespace WebPScanner.Data.Repositories;

public class ScanJobRepository : IScanJobRepository
{
    private readonly WebPScannerDbContext _context;

    public ScanJobRepository(WebPScannerDbContext context)
    {
        _context = context;
    }

    public async Task<ScanJob?> GetByIdAsync(Guid scanId, CancellationToken cancellationToken = default)
    {
        return await _context.ScanJobs
            .FirstOrDefaultAsync(s => s.ScanId == scanId, cancellationToken);
    }

    public async Task<IEnumerable<ScanJob>> GetByStatusAsync(ScanStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.ScanJobs
            .Where(s => s.Status == status)
            .OrderBy(s => s.PriorityScore)
            .ThenBy(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ScanJob>> GetQueuedJobsOrderedByPriorityAsync(int limit, CancellationToken cancellationToken = default)
    {
        return await _context.ScanJobs
            .Where(s => s.Status == ScanStatus.Queued)
            .OrderBy(s => s.PriorityScore)
            .ThenBy(s => s.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ScanJob>> GetAllQueuedJobsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ScanJobs
            .Where(s => s.Status == ScanStatus.Queued)
            .OrderBy(s => s.PriorityScore)
            .ThenBy(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetQueuePositionAsync(Guid scanId, CancellationToken cancellationToken = default)
    {
        var job = await GetByIdAsync(scanId, cancellationToken);
        if (job is not { Status: ScanStatus.Queued })
            return 0;

        // Count jobs ahead in queue: lower priority score, or same score but created earlier
        return await _context.ScanJobs
            .CountAsync(s => s.Status == ScanStatus.Queued &&
                (s.PriorityScore < job.PriorityScore ||
                 (s.PriorityScore == job.PriorityScore && s.CreatedAt < job.CreatedAt)),
                cancellationToken) + 1;
    }

    public async Task<int> GetQueuedCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ScanJobs
            .CountAsync(s => s.Status == ScanStatus.Queued, cancellationToken);
    }

    public async Task<int> GetProcessingCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ScanJobs
            .CountAsync(s => s.Status == ScanStatus.Processing, cancellationToken);
    }

    public async Task<int> GetJobCountByIpAsync(string ip, CancellationToken cancellationToken = default)
    {
        return await _context.ScanJobs
            .CountAsync(s => s.SubmitterIp == ip &&
                            (s.Status == ScanStatus.Queued || s.Status == ScanStatus.Processing),
                        cancellationToken);
    }

    public async Task<ScanJob> AddAsync(ScanJob scanJob, CancellationToken cancellationToken = default)
    {
        _context.ScanJobs.Add(scanJob);
        await _context.SaveChangesAsync(cancellationToken);
        return scanJob;
    }

    public async Task UpdateAsync(ScanJob scanJob, CancellationToken cancellationToken = default)
    {
        _context.ScanJobs.Update(scanJob);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateManyAsync(IEnumerable<ScanJob> scanJobs, CancellationToken cancellationToken = default)
    {
        _context.ScanJobs.UpdateRange(scanJobs);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid scanId, CancellationToken cancellationToken = default)
    {
        var scanJob = await GetByIdAsync(scanId, cancellationToken);
        if (scanJob != null)
        {
            _context.ScanJobs.Remove(scanJob);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<ScanJob>> GetCompletedScansBeforeAsync(DateTime cutoffTime, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.ScanJobs
            .Where(s => s.Status == ScanStatus.Completed && s.CompletedAt < cutoffTime)
            .OrderBy(s => s.CompletedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
