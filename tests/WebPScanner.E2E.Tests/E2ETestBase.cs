using Microsoft.Playwright;

namespace WebPScanner.E2E.Tests;

/// <summary>
/// Base class for E2E tests that handles both local (WebApplicationFactory) and CI (Docker) modes.
/// Set E2E_BASE_URL environment variable to test against an external server (e.g., Docker container).
/// </summary>
public abstract class E2ETestBase : IAsyncDisposable
{
    private WebApplicationFixture? _appFixture;
    private IBrowserContext Context { get; set; } = null!;
    protected IPage Page { get; private set; } = null!;
    protected string BaseUrl { get; private set; } = null!;

    /// <summary>
    /// Returns true if running against an external server (CI mode with Docker).
    /// </summary>
    protected static bool IsExternalServer => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("E2E_BASE_URL"));

    [SetUp]
    public virtual async Task SetUp()
    {
        var externalUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL");

        if (!string.IsNullOrEmpty(externalUrl))
        {
            // CI mode: use external Docker container
            BaseUrl = externalUrl.TrimEnd('/');
        }
        else
        {
            // Local mode: use WebApplicationFactory
            _appFixture = new WebApplicationFixture();
            var client = _appFixture.CreateClient();
            BaseUrl = client.BaseAddress!.ToString().TrimEnd('/');
        }

        Context = await PlaywrightFixture.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        Page = await Context.NewPageAsync();
    }

    [TearDown]
    public virtual async Task TearDown()
    {
        await Page.CloseAsync();
        await Context.CloseAsync();

        if (_appFixture != null)
        {
            await _appFixture.DisposeAsync();
            _appFixture = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_appFixture != null)
        {
            await _appFixture.DisposeAsync();
            _appFixture = null;
        }

        GC.SuppressFinalize(this);
    }
}
