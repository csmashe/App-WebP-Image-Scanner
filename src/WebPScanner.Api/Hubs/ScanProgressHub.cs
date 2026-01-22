using Microsoft.AspNetCore.SignalR;
using WebPScanner.Core.DTOs;
using WebPScanner.Core.Enums;
using WebPScanner.Core.Interfaces;

namespace WebPScanner.Api.Hubs;

/// <summary>
/// SignalR hub for real-time scan progress updates.
/// Clients can subscribe to specific scans and receive progress notifications.
/// </summary>
public class ScanProgressHub : Hub<IScanProgressClient>
{
    private readonly ILogger<ScanProgressHub> _logger;
    private readonly IScanJobRepository _scanJobRepository;
    private readonly ICrawlCheckpointRepository _checkpointRepository;
    private readonly IDiscoveredImageRepository _discoveredImageRepository;

    /// <summary>
    /// Group name for clients subscribed to aggregate stats updates.
    /// </summary>
    internal const string StatsGroupName = "stats-updates";

    public ScanProgressHub(
        ILogger<ScanProgressHub> logger,
        IScanJobRepository scanJobRepository,
        ICrawlCheckpointRepository checkpointRepository,
        IDiscoveredImageRepository discoveredImageRepository)
    {
        _logger = logger;
        _scanJobRepository = scanJobRepository;
        _checkpointRepository = checkpointRepository;
        _discoveredImageRepository = discoveredImageRepository;
    }

    /// <summary>
    /// Subscribe to updates for a specific scan job.
    /// </summary>
    /// <param name="scanId">The scan job ID to subscribe to.</param>
    public async Task SubscribeToScan(Guid scanId)
    {
        var groupName = GetGroupName(scanId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogDebug(
            "Client {ConnectionId} subscribed to scan {ScanId}",
            Context.ConnectionId, scanId);
    }

    /// <summary>
    /// Unsubscribe from updates for a specific scan job.
    /// </summary>
    /// <param name="scanId">The scan job ID to unsubscribe from.</param>
    public async Task UnsubscribeFromScan(Guid scanId)
    {
        var groupName = GetGroupName(scanId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogDebug(
            "Client {ConnectionId} unsubscribed from scan {ScanId}",
            Context.ConnectionId, scanId);
    }

    /// <summary>
    /// Subscribe to aggregate stats updates.
    /// </summary>
    public async Task SubscribeToStats()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, StatsGroupName);

        _logger.LogDebug(
            "Client {ConnectionId} subscribed to stats updates",
            Context.ConnectionId);
    }

    /// <summary>
    /// Unsubscribe from aggregate stats updates.
    /// </summary>
    public async Task UnsubscribeFromStats()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, StatsGroupName);

        _logger.LogDebug(
            "Client {ConnectionId} unsubscribed from stats updates",
            Context.ConnectionId);
    }

    /// <summary>
    /// Get current progress for a scan. Called by client on reconnect to sync state.
    /// </summary>
    /// <param name="scanId">The scan job ID.</param>
    /// <returns>Current progress snapshot, or null if scan not found.</returns>
    public async Task<ScanProgressSnapshot?> GetCurrentProgress(Guid scanId)
    {
        try
        {
            var job = await _scanJobRepository.GetByIdAsync(scanId);
            if (job == null)
            {
                _logger.LogDebug("GetCurrentProgress: Scan {ScanId} not found", scanId);
                return null;
            }

            var checkpoint = await _checkpointRepository.GetByScanJobIdAsync(scanId);

            // Use checkpoint data if available (more current), otherwise use job data
            var pagesScanned = checkpoint?.PagesVisited ?? job.PagesScanned;
            var pagesDiscovered = checkpoint?.PagesDiscovered ?? job.PagesDiscovered;

            // Database is source of truth - images are saved incrementally during scanning
            var nonWebPImagesCount = await _discoveredImageRepository.GetCountByScanJobIdAsync(scanId);

            var progressPercent = pagesDiscovered > 0
                ? Math.Clamp((int)((double)pagesScanned / pagesDiscovered * 100), 0, 100)
                : 0;

            var queuePosition = 0;
            if (job.Status == ScanStatus.Queued)
            {
                queuePosition = await _scanJobRepository.GetQueuePositionAsync(scanId);
            }

            var snapshot = new ScanProgressSnapshot
            {
                Status = job.Status.ToString(),
                PagesScanned = pagesScanned,
                PagesDiscovered = pagesDiscovered,
                NonWebPImagesCount = nonWebPImagesCount,
                QueuePosition = queuePosition,
                ProgressPercent = progressPercent,
                CurrentUrl = checkpoint?.CurrentUrl,
                ErrorMessage = job.ErrorMessage
            };

            _logger.LogDebug(
                "GetCurrentProgress: Returning snapshot for scan {ScanId}: Status={Status}, Pages={Pages}/{Discovered}",
                scanId, snapshot.Status, snapshot.PagesScanned, snapshot.PagesDiscovered);

            return snapshot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current progress for scan {ScanId}", scanId);
            return null;
        }
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogDebug("Client {ConnectionId} connected to ScanProgressHub", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    /// <param name="exception">The exception that caused the disconnect, if any.</param>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(
                exception,
                "Client {ConnectionId} disconnected with error from ScanProgressHub",
                Context.ConnectionId);
        }
        else
        {
            _logger.LogDebug(
                "Client {ConnectionId} disconnected from ScanProgressHub",
                Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Gets the group name for a scan ID.
    /// </summary>
    /// <param name="scanId">The scan job ID.</param>
    /// <returns>The group name.</returns>
    internal static string GetGroupName(Guid scanId) => $"scan-{scanId}";
}
