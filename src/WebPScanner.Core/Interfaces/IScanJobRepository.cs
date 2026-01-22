using WebPScanner.Core.Entities;
using WebPScanner.Core.Enums;

namespace WebPScanner.Core.Interfaces;

/// <summary>
/// Repository for persisting and querying scan job records.
/// </summary>
public interface IScanJobRepository
{
    /// <summary>
    /// Retrieves a scan job by its unique identifier.
    /// </summary>
    /// <param name="scanId">The unique identifier of the scan job.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The scan job if found; otherwise, null.</returns>
    Task<ScanJob?> GetByIdAsync(Guid scanId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all scan jobs with the specified status.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A collection of scan jobs matching the status.</returns>
    Task<IEnumerable<ScanJob>> GetByStatusAsync(ScanStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves queued jobs ordered by priority, limited to a specified count.
    /// </summary>
    /// <param name="limit">The maximum number of jobs to return.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A collection of queued scan jobs ordered by priority.</returns>
    Task<IEnumerable<ScanJob>> GetQueuedJobsOrderedByPriorityAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all queued scan jobs.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A collection of all queued scan jobs.</returns>
    Task<IEnumerable<ScanJob>> GetAllQueuedJobsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the queue position for a specific scan job.
    /// </summary>
    /// <param name="scanId">The unique identifier of the scan job.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The 1-based position in the queue, or 0 if not queued.</returns>
    Task<int> GetQueuePositionAsync(Guid scanId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of queued scan jobs.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The number of queued jobs.</returns>
    Task<int> GetQueuedCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of currently processing scan jobs.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The number of processing jobs.</returns>
    Task<int> GetProcessingCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of scan jobs submitted from a specific IP address.
    /// </summary>
    /// <param name="ip">The IP address to filter by.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The number of jobs from the specified IP.</returns>
    Task<int> GetJobCountByIpAsync(string ip, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new scan job to the repository.
    /// </summary>
    /// <param name="scanJob">The scan job to add.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The added scan job with any generated values populated.</returns>
    Task<ScanJob> AddAsync(ScanJob scanJob, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing scan job in the repository.
    /// </summary>
    /// <param name="scanJob">The scan job with updated values.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task UpdateAsync(ScanJob scanJob, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple scan jobs in a single operation.
    /// </summary>
    /// <param name="scanJobs">The collection of scan jobs to update.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task UpdateManyAsync(IEnumerable<ScanJob> scanJobs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a scan job from the repository.
    /// </summary>
    /// <param name="scanId">The unique identifier of the scan job to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task DeleteAsync(Guid scanId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves completed scan jobs that finished before a specified time.
    /// </summary>
    /// <param name="cutoffTime">The cutoff datetime; jobs completed before this are returned.</param>
    /// <param name="limit">The maximum number of jobs to return.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A collection of completed scan jobs older than the cutoff time.</returns>
    Task<IEnumerable<ScanJob>> GetCompletedScansBeforeAsync(DateTime cutoffTime, int limit, CancellationToken cancellationToken = default);
}
