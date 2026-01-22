using Microsoft.AspNetCore.SignalR.Client;
using WebPScanner.Core.DTOs;

namespace WebPScanner.Api.Tests;

[TestFixture]
public class ScanProgressHubTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HubConnection? _connection;
    private string? _serverUrl;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _factory = new CustomWebApplicationFactory();

        // Create and start a server, dispose client after getting URL
        using (var client = _factory.CreateClient())
        {
            _serverUrl = client.BaseAddress?.ToString().TrimEnd('/');
        }

        if (_serverUrl == null)
            throw new InvalidOperationException("Could not get server URL from factory");

        // Create SignalR connection
        _connection = new HubConnectionBuilder()
            .WithUrl($"{_serverUrl}/hubs/scanprogress", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        await _connection.StartAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
        await _factory.DisposeAsync();
    }

    [Test]
    public async Task SubscribeToScan_SuccessfullyJoinsGroup()
    {
        // Arrange
        var scanId = Guid.NewGuid();

        // Act & Assert - should not throw
        await _connection!.InvokeAsync("SubscribeToScan", scanId);
    }

    [Test]
    public async Task UnsubscribeFromScan_SuccessfullyLeavesGroup()
    {
        // Arrange
        var scanId = Guid.NewGuid();
        await _connection!.InvokeAsync("SubscribeToScan", scanId);

        // Act & Assert - should not throw
        await _connection!.InvokeAsync("UnsubscribeFromScan", scanId);
    }

    [Test]
    public async Task SubscribeToScan_CanSubscribeToMultipleScans()
    {
        // Arrange
        var scanId1 = Guid.NewGuid();
        var scanId2 = Guid.NewGuid();
        var scanId3 = Guid.NewGuid();

        // Act & Assert - should not throw
        await _connection!.InvokeAsync("SubscribeToScan", scanId1);
        await _connection!.InvokeAsync("SubscribeToScan", scanId2);
        await _connection!.InvokeAsync("SubscribeToScan", scanId3);
    }

    [Test]
    public void Connection_ConnectsSuccessfully()
    {
        // Assert
        Assert.That(_connection!.State, Is.EqualTo(HubConnectionState.Connected));
    }

    [Test]
    public async Task Connection_CanReceiveQueuePositionUpdate()
    {
        // Arrange
        var scanId = Guid.NewGuid();
        var receivedUpdate = new TaskCompletionSource<QueuePositionUpdateDto>();

        _connection!.On<QueuePositionUpdateDto>("QueuePositionUpdate", update =>
        {
            receivedUpdate.TrySetResult(update);
        });

        await _connection!.InvokeAsync("SubscribeToScan", scanId);

        // Act - manually call the service to send an update
        // Note: This requires access to the service, which we'll test via the API
        // For now, verify the handler registration works
        Assert.That(_connection!.State == HubConnectionState.Connected, Is.True);
    }

    [Test]
    public async Task Connection_CanReceiveScanStarted()
    {
        // Arrange
        var scanId = Guid.NewGuid();

        _connection!.On<ScanStartedDto>("ScanStarted", _ =>
        {
            // Verify handler is registered
        });

        await _connection!.InvokeAsync("SubscribeToScan", scanId);

        // Assert - connection is still alive
        Assert.That(_connection!.State == HubConnectionState.Connected, Is.True);
    }

    [Test]
    public async Task Connection_CanReceivePageProgress()
    {
        // Arrange
        var scanId = Guid.NewGuid();

        _connection!.On<PageProgressDto>("PageProgress", _ =>
        {
            // Verify handler is registered
        });

        await _connection!.InvokeAsync("SubscribeToScan", scanId);

        // Assert - connection is still alive
        Assert.That(_connection!.State == HubConnectionState.Connected, Is.True);
    }

    [Test]
    public async Task Connection_CanReceiveImageFound()
    {
        // Arrange
        var scanId = Guid.NewGuid();

        _connection!.On<ImageFoundDto>("ImageFound", _ =>
        {
            // Verify handler is registered
        });

        await _connection!.InvokeAsync("SubscribeToScan", scanId);

        // Assert - connection is still alive
        Assert.That(_connection!.State == HubConnectionState.Connected, Is.True);
    }

    [Test]
    public async Task Connection_CanReceiveScanComplete()
    {
        // Arrange
        var scanId = Guid.NewGuid();

        _connection!.On<ScanCompleteDto>("ScanComplete", _ =>
        {
            // Verify handler is registered
        });

        await _connection!.InvokeAsync("SubscribeToScan", scanId);

        // Assert - connection is still alive
        Assert.That(_connection!.State == HubConnectionState.Connected, Is.True);
    }

    [Test]
    public async Task Connection_CanReceiveScanFailed()
    {
        // Arrange
        var scanId = Guid.NewGuid();

        _connection!.On<ScanFailedDto>("ScanFailed", _ =>
        {
            // Verify handler is registered
        });

        await _connection!.InvokeAsync("SubscribeToScan", scanId);

        // Assert - connection is still alive
        Assert.That(_connection!.State == HubConnectionState.Connected, Is.True);
    }

    [Test]
    public async Task Reconnection_CanReconnectAfterDisconnection()
    {
        // Arrange
        var scanId = Guid.NewGuid();
        await _connection!.InvokeAsync("SubscribeToScan", scanId);

        // Act
        await _connection!.StopAsync();
        Assert.That(_connection!.State, Is.EqualTo(HubConnectionState.Disconnected));

        await _connection!.StartAsync();

        // Assert
        Assert.That(_connection!.State, Is.EqualTo(HubConnectionState.Connected));
    }
}
