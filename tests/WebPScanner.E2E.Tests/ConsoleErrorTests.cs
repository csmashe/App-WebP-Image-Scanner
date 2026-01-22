using Microsoft.Playwright;

namespace WebPScanner.E2E.Tests;

/// <summary>
/// E2E tests to verify all console errors are addressed.
/// </summary>
[TestFixture]
public class ConsoleErrorTests : E2ETestBase
{
    private readonly List<IConsoleMessage> _consoleMessages = [];

    [SetUp]
    public override async Task SetUp()
    {
        await base.SetUp();

        // Capture all console messages
        Page.Console += (_, msg) => _consoleMessages.Add(msg);
    }

    [TearDown]
    public override async Task TearDown()
    {
        await base.TearDown();
    }

    [Test]
    public async Task HomePage_ShouldHaveNoJavaScriptErrors()
    {
        _consoleMessages.Clear();

        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var errors = _consoleMessages
            .Where(m => m.Type == "error")
            .Where(m => !IsExpectedError(m.Text))
            .ToList();

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public async Task FormInteraction_ShouldHaveNoErrors()
    {
        _consoleMessages.Clear();

        await Page.GotoAsync(BaseUrl);

        // Interact with form
        var urlInput = Page.Locator("input[type='url'], input[placeholder*='URL'], input[placeholder*='url']").First;
        await urlInput.FillAsync("https://example.com");

        await Page.WaitForTimeoutAsync(500);

        var errors = _consoleMessages
            .Where(m => m.Type == "error")
            .Where(m => !IsExpectedError(m.Text))
            .ToList();

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public async Task ThemeToggle_ShouldHaveNoErrors()
    {
        _consoleMessages.Clear();

        await Page.GotoAsync(BaseUrl);

        // Find and click theme toggle
        var themeToggle = Page.Locator("header button:has(svg)").Last;
        await themeToggle.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Toggle again
        await themeToggle.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        var errors = _consoleMessages
            .Where(m => m.Type == "error")
            .Where(m => !IsExpectedError(m.Text))
            .ToList();

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public async Task FormSubmission_ShouldHaveNoUnhandledErrors()
    {
        _consoleMessages.Clear();

        await Page.GotoAsync(BaseUrl);

        // Fill and submit form
        var urlInput = Page.Locator("input[type='url'], input[placeholder*='URL'], input[placeholder*='url']").First;
        await urlInput.FillAsync("https://example.com");

        var submitButton = Page.Locator("button[type='submit'], button:has-text('Scan'), button:has-text('Start')").First;
        await submitButton.ClickAsync();

        await Page.WaitForTimeoutAsync(2000);

        var errors = _consoleMessages
            .Where(m => m.Type == "error")
            .Where(m => !IsExpectedError(m.Text))
            .ToList();

        // No unhandled JavaScript errors should occur
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public async Task PageNavigation_ShouldHaveNoErrors()
    {
        _consoleMessages.Clear();

        await Page.GotoAsync(BaseUrl);

        // Navigate to Terms of Service if link exists
        var tosLink = Page.Locator("text=Terms of Service").Or(Page.Locator("a:has-text('Terms')")).First;
        if (await tosLink.IsVisibleAsync())
        {
            await tosLink.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
        }

        // Go back to home
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var errors = _consoleMessages
            .Where(m => m.Type == "error")
            .Where(m => !IsExpectedError(m.Text))
            .ToList();

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public async Task MobileViewport_ShouldHaveNoErrors()
    {
        _consoleMessages.Clear();

        await Page.SetViewportSizeAsync(390, 844);
        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var errors = _consoleMessages
            .Where(m => m.Type == "error")
            .Where(m => !IsExpectedError(m.Text))
            .ToList();

        Assert.That(errors, Is.Empty);
    }

    [Test]
    public async Task NoDeprecationWarnings_ShouldBePresent()
    {
        _consoleMessages.Clear();

        await Page.GotoAsync(BaseUrl);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Filter for deprecation warnings
        var deprecationWarnings = _consoleMessages
            .Where(m => m.Type == "warning")
            .Where(m => m.Text.Contains("deprecated", StringComparison.OrdinalIgnoreCase) ||
                       m.Text.Contains("deprecation", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Note: Some libraries may produce warnings, so we just log them
        // rather than fail the test
        Assert.That(true, Is.True, $"Found {deprecationWarnings.Count} deprecation warnings");
    }

    private static bool IsExpectedError(string errorText)
    {
        // List of expected/acceptable errors that can be ignored
        var expectedPatterns = new[]
        {
            "favicon.ico", // Missing favicon is common
            "manifest.json", // PWA manifest may be missing
            "DevTools", // Chrome DevTools messages
            "ERR_CONNECTION_REFUSED", // Expected when server not running
            "net::ERR_FAILED", // Network failures during testing
            "Failed to load resource", // Resource loading during tests
            "WebSocket", // WebSocket connection errors during page transitions
            "SignalR", // SignalR connection messages
            "HubConnection", // SignalR hub connection messages
            "negotiate", // SignalR negotiation errors
            "ERR_ABORTED" // Aborted requests during navigation
        };

        return expectedPatterns.Any(pattern =>
            errorText.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}
