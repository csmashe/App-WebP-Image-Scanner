using Microsoft.Playwright;

namespace WebPScanner.E2E.Tests;

/// <summary>
/// E2E tests for mobile responsive layout functionality.
/// </summary>
[TestFixture]
public class ResponsiveLayoutTests : E2ETestBase
{

    [Test]
    public async Task Layout_ShouldWorkOnMobileViewport()
    {
        // Set mobile viewport (iPhone 12)
        await Page.SetViewportSizeAsync(390, 844);
        await Page.GotoAsync(BaseUrl);

        // Page should load without errors
        var header = Page.Locator("header").First;
        await Assertions.Expect(header).ToBeVisibleAsync();

        // Form should be visible and usable
        var urlInput = Page.Locator("input[type='url'], input[placeholder*='URL'], input[placeholder*='url']").First;
        await Assertions.Expect(urlInput).ToBeVisibleAsync();

        // Check that content fits within viewport (no horizontal scrollbar)
        var bodyWidth = await Page.EvaluateAsync<int>("document.body.scrollWidth");
        var viewportWidth = await Page.EvaluateAsync<int>("window.innerWidth");

        // Body should not be significantly wider than viewport
        Assert.That(bodyWidth <= viewportWidth + 5, Is.True, "Content should fit within mobile viewport");
    }

    [Test]
    public async Task Layout_ShouldWorkOnTabletViewport()
    {
        // Set tablet viewport (iPad)
        await Page.SetViewportSizeAsync(768, 1024);
        await Page.GotoAsync(BaseUrl);

        // Check header is visible
        var header = Page.Locator("header").First;
        await Assertions.Expect(header).ToBeVisibleAsync();

        // Check form is visible
        var urlInput = Page.Locator("input[type='url'], input[placeholder*='URL'], input[placeholder*='url']").First;
        await Assertions.Expect(urlInput).ToBeVisibleAsync();

        // Check footer is visible
        var footer = Page.Locator("footer").First;
        await Assertions.Expect(footer).ToBeVisibleAsync();
    }

    [Test]
    public async Task Layout_ShouldWorkOnDesktopViewport()
    {
        // Set desktop viewport
        await Page.SetViewportSizeAsync(1920, 1080);
        await Page.GotoAsync(BaseUrl);

        // Check all major sections are visible
        var header = Page.Locator("header").First;
        await Assertions.Expect(header).ToBeVisibleAsync();

        var footer = Page.Locator("footer").First;
        await Assertions.Expect(footer).ToBeVisibleAsync();
    }

    [Test]
    public async Task FormInputs_ShouldBeUsableOnMobile()
    {
        // Set mobile viewport
        await Page.SetViewportSizeAsync(390, 844);
        await Page.GotoAsync(BaseUrl);

        // Should be able to interact with URL input
        var urlInput = Page.Locator("input[type='url'], input[placeholder*='URL'], input[placeholder*='url']").First;
        await urlInput.FillAsync("https://example.com");

        var urlValue = await urlInput.InputValueAsync();
        Assert.That(urlValue, Is.EqualTo("https://example.com"));
    }

    [Test]
    public async Task Buttons_ShouldBeTappableOnMobile()
    {
        // Set mobile viewport
        await Page.SetViewportSizeAsync(390, 844);
        await Page.GotoAsync(BaseUrl);

        // Check submit button is visible and appropriately sized
        var submitButton = Page.Locator("button[type='submit'], button:has-text('Scan'), button:has-text('Start')").First;
        await Assertions.Expect(submitButton).ToBeVisibleAsync();

        // Get button bounding box
        var boundingBox = await submitButton.BoundingBoxAsync();
        Assert.That(boundingBox, Is.Not.Null);

        // Button should have minimum tap target size (44x44 is iOS recommendation)
        Assert.That(boundingBox!.Width >= 44, Is.True, "Button should have adequate tap width");
        Assert.That(boundingBox.Height >= 44, Is.True, "Button should have adequate tap height");
    }

    [Test]
    public async Task TextContent_ShouldBeReadableOnSmallScreens()
    {
        // Set small mobile viewport
        await Page.SetViewportSizeAsync(320, 568); // iPhone SE
        await Page.GotoAsync(BaseUrl);

        // Check that main heading is visible
        var heading = Page.Locator("h1, h2").First;
        await Assertions.Expect(heading).ToBeVisibleAsync();

        // Check form labels/inputs are visible
        var urlInput = Page.Locator("input[type='url'], input[placeholder*='URL'], input[placeholder*='url']").First;
        await Assertions.Expect(urlInput).ToBeVisibleAsync();
    }

    [Test]
    public async Task Navigation_ShouldWorkOnMobile()
    {
        // Set mobile viewport
        await Page.SetViewportSizeAsync(390, 844);
        await Page.GotoAsync(BaseUrl);

        // Check that navigation elements are accessible
        var headerLinks = Page.Locator("header a, header button");
        var linkCount = await headerLinks.CountAsync();

        // Should have at least the logo and theme toggle
        Assert.That(linkCount >= 1, Is.True, "Should have navigation elements");
    }
}
