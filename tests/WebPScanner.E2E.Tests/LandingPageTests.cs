using Microsoft.Playwright;

namespace WebPScanner.E2E.Tests;

/// <summary>
/// E2E tests for the landing page functionality.
/// </summary>
[TestFixture]
public class LandingPageTests : E2ETestBase
{

    [Test]
    public async Task LandingPage_ShouldLoadSuccessfully()
    {
        // Navigate to the landing page
        var response = await Page.GotoAsync(BaseUrl);

        // Assert page loads successfully
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Ok, Is.True);

        // Assert title is present
        var title = await Page.TitleAsync();
        Assert.That(title, Is.Not.Empty);
    }

    [Test]
    public async Task LandingPage_ShouldDisplayHeader()
    {
        await Page.GotoAsync(BaseUrl);

        // Check for WebP Scanner branding in header
        var header = Page.Locator("header").First;
        await Assertions.Expect(header).ToBeVisibleAsync();
    }

    [Test]
    public async Task LandingPage_ShouldDisplayScanForm()
    {
        await Page.GotoAsync(BaseUrl);

        // Check for URL input field
        var urlInput = Page.Locator("input[type='url'], input[placeholder*='URL'], input[placeholder*='url']").First;
        await Assertions.Expect(urlInput).ToBeVisibleAsync();

        // Check for submit button
        var submitButton = Page.Locator("button[type='submit'], button:has-text('Scan'), button:has-text('Start')").First;
        await Assertions.Expect(submitButton).ToBeVisibleAsync();
    }

    [Test]
    public async Task LandingPage_ShouldDisplayHowItWorksSection()
    {
        await Page.GotoAsync(BaseUrl);

        // Look for the "How It Works" section
        var howItWorksText = Page.Locator("text=How It Works").First;
        await Assertions.Expect(howItWorksText).ToBeVisibleAsync();
    }

    [Test]
    public async Task LandingPage_ShouldDisplayWhyWebPSection()
    {
        await Page.GotoAsync(BaseUrl);

        // Look for WebP benefits content
        var whyWebPText = Page.Locator("text=Why WebP").First;
        await Assertions.Expect(whyWebPText).ToBeVisibleAsync();
    }

    [Test]
    public async Task LandingPage_ShouldDisplayFooter()
    {
        await Page.GotoAsync(BaseUrl);

        // Check for footer
        var footer = Page.Locator("footer").First;
        await Assertions.Expect(footer).ToBeVisibleAsync();
    }

    [Test]
    public async Task ScanForm_ShouldShowValidationErrorForInvalidUrl()
    {
        await Page.GotoAsync(BaseUrl);

        // Find and fill URL input with invalid URL
        var urlInput = Page.Locator("input[type='url'], input[placeholder*='URL'], input[placeholder*='url']").First;
        await urlInput.FillAsync("not-a-valid-url");

        // Tab out to trigger validation
        await urlInput.PressAsync("Tab");

        // Wait briefly for validation to trigger
        await Page.WaitForTimeoutAsync(500);

        // Check for validation error (either error class or error message)
        var hasError = await Page.Locator("[class*='error'], [class*='invalid'], [aria-invalid='true']").CountAsync() > 0
            || await Page.Locator("text=valid URL").CountAsync() > 0;

        // The form should show some indication of invalid input
        Assert.That(hasError || await urlInput.GetAttributeAsync("aria-invalid") == "true" ||
            await Page.Locator("span:has-text('Invalid'), p:has-text('Invalid')").CountAsync() > 0, Is.True);
    }

    [Test]
    public async Task ScanForm_ShouldAcceptValidInputs()
    {
        await Page.GotoAsync(BaseUrl);

        // Fill URL input with valid URL
        var urlInput = Page.Locator("input[type='url'], input[placeholder*='URL'], input[placeholder*='url']").First;
        await urlInput.FillAsync("https://example.com");

        // Submit button should be enabled or form should be submittable
        var submitButton = Page.Locator("button[type='submit'], button:has-text('Scan'), button:has-text('Start')").First;
        await Assertions.Expect(submitButton).ToBeEnabledAsync();
    }
}
