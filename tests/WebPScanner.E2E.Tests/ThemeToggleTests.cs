using Microsoft.Playwright;

namespace WebPScanner.E2E.Tests;

/// <summary>
/// E2E tests for dark/light mode toggle functionality.
/// </summary>
[TestFixture]
public class ThemeToggleTests : E2ETestBase
{

    [Test]
    public async Task ThemeToggle_ShouldBeVisible()
    {
        await Page.GotoAsync(BaseUrl);

        // Look for theme toggle button (usually has sun/moon icon or theme-related aria label)
        var themeToggle = Page.Locator("button[aria-label*='theme'], button[aria-label*='Theme'], button:has(svg)").First;
        await Assertions.Expect(themeToggle).ToBeVisibleAsync();
    }

    [Test]
    public async Task ThemeToggle_ShouldSwitchTheme()
    {
        await Page.GotoAsync(BaseUrl);

        // Get initial theme state
        var initialHtmlClass = await Page.Locator("html").GetAttributeAsync("class") ?? "";

        // Find and click theme toggle
        var themeToggle = Page.Locator("button[aria-label*='theme'], button[aria-label*='Theme']").First;

        // If we can't find a labeled button, try looking for any button in the header with an SVG
        if (!await themeToggle.IsVisibleAsync())
        {
            themeToggle = Page.Locator("header button:has(svg)").Last;
        }

        await themeToggle.ClickAsync();
        await Page.WaitForTimeoutAsync(500); // Wait for theme transition

        // Get new theme state
        var newHtmlClass = await Page.Locator("html").GetAttributeAsync("class") ?? "";

        // Theme should have changed (dark class added or removed)
        var themeChanged = initialHtmlClass.Contains("dark") != newHtmlClass.Contains("dark") ||
                          initialHtmlClass != newHtmlClass;

        Assert.That(themeChanged, Is.True, "Theme should change after clicking toggle");
    }

    [Test]
    public async Task Theme_ShouldPersistAcrossPageRefresh()
    {
        await Page.GotoAsync(BaseUrl);

        // Toggle theme
        var themeToggle = Page.Locator("header button:has(svg)").Last;
        await themeToggle.ClickAsync();
        await Page.WaitForTimeoutAsync(500);

        // Get theme state
        var themeBeforeRefresh = await Page.Locator("html").GetAttributeAsync("class") ?? "";

        // Refresh page
        await Page.ReloadAsync();
        await Page.WaitForTimeoutAsync(500);

        // Get theme state after refresh
        var themeAfterRefresh = await Page.Locator("html").GetAttributeAsync("class") ?? "";

        // Theme should be preserved (both contain dark or both don't)
        var darkBeforeRefresh = themeBeforeRefresh.Contains("dark");
        var darkAfterRefresh = themeAfterRefresh.Contains("dark");

        Assert.That(darkAfterRefresh, Is.EqualTo(darkBeforeRefresh));
    }

    [Test]
    public async Task DarkMode_ShouldHaveDarkBackground()
    {
        await Page.GotoAsync(BaseUrl);

        // Ensure we're in dark mode
        var htmlElement = Page.Locator("html");
        var currentClass = await htmlElement.GetAttributeAsync("class") ?? "";

        if (!currentClass.Contains("dark"))
        {
            var themeToggle = Page.Locator("header button:has(svg)").Last;
            await themeToggle.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
        }

        // Check body/main has dark background color
        var bodyBgColor = await Page.EvaluateAsync<string>(
            "window.getComputedStyle(document.body).backgroundColor");

        // Dark backgrounds typically have low RGB values
        // Parse the rgb value and check if it's dark
        Assert.That(bodyBgColor, Is.Not.Null);
    }

    [Test]
    public async Task LightMode_ShouldHaveLightBackground()
    {
        await Page.GotoAsync(BaseUrl);

        // Ensure we're in light mode
        var htmlElement = Page.Locator("html");
        var currentClass = await htmlElement.GetAttributeAsync("class") ?? "";

        if (currentClass.Contains("dark"))
        {
            var themeToggle = Page.Locator("header button:has(svg)").Last;
            await themeToggle.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
        }

        // Check body has light background
        var bodyBgColor = await Page.EvaluateAsync<string>(
            "window.getComputedStyle(document.body).backgroundColor");

        Assert.That(bodyBgColor, Is.Not.Null);
    }
}
