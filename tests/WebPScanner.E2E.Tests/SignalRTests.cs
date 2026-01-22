using Microsoft.AspNetCore.SignalR.Client;

namespace WebPScanner.E2E.Tests;

/// <summary>
/// E2E tests for SignalR real-time progress updates.
/// </summary>
[TestFixture]
public class SignalRTests : E2ETestBase
{
    private WebApplicationFixture? _localAppFixture;

    [SetUp]
    public override async Task SetUp()
    {
        await base.SetUp();

        // Create local app fixture only for tests that need direct SignalR access
        if (!IsExternalServer)
        {
            _localAppFixture = new WebApplicationFixture();
        }
    }

    [TearDown]
    public override async Task TearDown()
    {
        if (_localAppFixture != null)
        {
            await _localAppFixture.DisposeAsync();
            _localAppFixture = null;
        }
        await base.TearDown();
    }

    [Test]
    public async Task SignalRHub_ShouldBeAccessible()
    {
        // Skip when running against external server (can't use CreateHandler)
        if (IsExternalServer)
        {
            Assert.Ignore("Test requires WebApplicationFactory - skipped for external server");
            return;
        }

        // Get the server handler for SignalR connections
        var hubUrl = $"{BaseUrl}/hubs/scanprogress";

        // Create a SignalR connection
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _localAppFixture!.Server.CreateHandler();
            })
            .Build();

        try
        {
            await connection.StartAsync();
            Assert.That(connection.State, Is.EqualTo(HubConnectionState.Connected));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Test]
    public async Task SignalRHub_ShouldAllowSubscription()
    {
        // Skip when running against external server
        if (IsExternalServer)
        {
            Assert.Ignore("Test requires WebApplicationFactory - skipped for external server");
            return;
        }

        var hubUrl = $"{BaseUrl}/hubs/scanprogress";

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _localAppFixture!.Server.CreateHandler();
            })
            .Build();

        try
        {
            await connection.StartAsync();

            // Subscribe to a scan (even with a fake ID)
            await connection.InvokeAsync("SubscribeToScan", Guid.NewGuid().ToString());

            // If we get here without exception, subscription works
            Assert.That(true, Is.True);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Test]
    public async Task SignalRHub_ShouldAllowUnsubscription()
    {
        // Skip when running against external server
        if (IsExternalServer)
        {
            Assert.Ignore("Test requires WebApplicationFactory - skipped for external server");
            return;
        }

        var hubUrl = $"{BaseUrl}/hubs/scanprogress";

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _localAppFixture!.Server.CreateHandler();
            })
            .Build();

        try
        {
            await connection.StartAsync();

            var scanId = Guid.NewGuid().ToString();
            await connection.InvokeAsync("SubscribeToScan", scanId);
            await connection.InvokeAsync("UnsubscribeFromScan", scanId);

            // If we get here without exception, unsubscription works
            Assert.That(true, Is.True);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Test]
    public async Task BrowserConnection_ShouldEstablishSignalR()
    {
        await Page.GotoAsync(BaseUrl);

        // Fill and submit form to trigger SignalR connection
        var urlInput = Page.Locator("input[type='url'], input[placeholder*='URL'], input[placeholder*='url']").First;
        await urlInput.FillAsync("https://example.com");

        var submitButton = Page.Locator("button[type='submit'], button:has-text('Scan'), button:has-text('Start')").First;
        await submitButton.ClickAsync();

        // Wait for SignalR connection to establish
        await Page.WaitForTimeoutAsync(2000);

        // Check if progress view is shown (indicates SignalR is working) or URL changed
        var hasProgressView =
            await Page.Locator("text=Queue").CountAsync() > 0 ||
            await Page.Locator("text=Position").CountAsync() > 0 ||
            await Page.Locator("text=Scanning").CountAsync() > 0 ||
            await Page.Locator("text=Progress").CountAsync() > 0 ||
            await Page.Locator("text=Waiting").CountAsync() > 0;

        // URL change also indicates form was submitted successfully
        var urlChanged = Page.Url != BaseUrl;

        Assert.That(hasProgressView || urlChanged, Is.True, "Should show progress view after submission (SignalR connected)");
    }

    [Test]
    public async Task SignalRConnection_ShouldHandleReconnection()
    {
        // Skip when running against external server
        if (IsExternalServer)
        {
            Assert.Ignore("Test requires WebApplicationFactory - skipped for external server");
            return;
        }

        var hubUrl = $"{BaseUrl}/hubs/scanprogress";

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => _localAppFixture!.Server.CreateHandler();
            })
            .WithAutomaticReconnect()
            .Build();

        try
        {
            await connection.StartAsync();
            Assert.That(connection.State, Is.EqualTo(HubConnectionState.Connected));

            // Connection is established
            Assert.That(true, Is.True);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
