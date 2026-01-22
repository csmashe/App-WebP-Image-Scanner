using Microsoft.Playwright;

namespace WebPScanner.E2E.Tests;

/// <summary>
/// Shared fixture for Playwright browser instance across all E2E tests.
/// This is a SetUpFixture that initializes once per test assembly.
/// </summary>
[SetUpFixture]
public class PlaywrightFixture
{
    private static IPlaywright Playwright { get; set; } = null!;
    public static IBrowser Browser { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        // Use Chromium for consistent cross-platform testing
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args =
            [
                "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-dev-shm-usage"
            ]
        });
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await Browser.CloseAsync();
        Playwright.Dispose();
    }
}
