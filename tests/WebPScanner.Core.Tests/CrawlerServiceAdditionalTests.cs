using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WebPScanner.Core.Configuration;
using WebPScanner.Core.Enums;
using WebPScanner.Core.Interfaces;
using WebPScanner.Core.Models;
using WebPScanner.Core.Services;

namespace WebPScanner.Core.Tests;

/// <summary>
/// Additional tests for CrawlerService to improve code coverage.
/// </summary>
[TestFixture]
public class CrawlerServiceAdditionalTests
{
    private Mock<IOptions<CrawlerOptions>> _optionsMock = null!;
    private Mock<IOptions<SecurityOptions>> _securityOptionsMock = null!;
    private Mock<ILogger<CrawlerService>> _loggerMock = null!;
    private Mock<IImageAnalyzerService> _imageAnalyzerMock = null!;
    private Mock<IValidationService> _validationServiceMock = null!;
    private CrawlerOptions _options = null!;
    private SecurityOptions _securityOptions = null!;

    [SetUp]
    public void SetUp()
    {
        _options = new CrawlerOptions
        {
            MaxPagesPerScan = 10,
            PageTimeoutSeconds = 30,
            NetworkIdleTimeoutMs = 2000,
            DelayBetweenPagesMs = 100,
            MaxRetries = 3,
            RespectRobotsTxt = true,
            UserAgent = "TestCrawler/1.0",
            RestrictToTargetDomain = true,
            BlockTrackingDomains = true,
            EnableSandbox = false,
            MaxRequestsPerPage = 100,
            MaxRequestSizeBytes = 10485760
        };

        _optionsMock = new Mock<IOptions<CrawlerOptions>>();
        _optionsMock.Setup(x => x.Value).Returns(_options);

        _securityOptions = new SecurityOptions
        {
            MaxScanDurationMinutes = 10,
            MaxMemoryPerScanMb = 512
        };
        _securityOptionsMock = new Mock<IOptions<SecurityOptions>>();
        _securityOptionsMock.Setup(x => x.Value).Returns(_securityOptions);

        _loggerMock = new Mock<ILogger<CrawlerService>>();

        _imageAnalyzerMock = new Mock<IImageAnalyzerService>();
        _imageAnalyzerMock.Setup(x => x.IsNonWebPRasterImage(It.IsAny<string>()))
            .Returns((string mimeType) =>
                !string.IsNullOrEmpty(mimeType) && new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/bmp", "image/tiff" }
                    .Contains(mimeType, StringComparer.OrdinalIgnoreCase));

        _validationServiceMock = new Mock<IValidationService>();
        _validationServiceMock.Setup(x => x.ValidateHostSsrfAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ValidationResult.Success());
        _validationServiceMock.Setup(x => x.IsPrivateOrReservedIp(It.IsAny<string>()))
            .Returns(false); // Default: treat all IPs as safe in tests
    }

    #region Tracking Domain Detection Tests

    [TestCase("https://google-analytics.com/collect", true)]
    [TestCase("https://www.google-analytics.com/analytics.js", true)]
    [TestCase("https://googletagmanager.com/gtm.js", true)]
    [TestCase("https://doubleclick.net/ad", true)]
    [TestCase("https://facebook.com/pixel", true)]
    [TestCase("https://connect.facebook.net/sdk.js", true)]
    [TestCase("https://twitter.com/tracking", true)]
    [TestCase("https://t.co/redirect", true)]
    [TestCase("https://linkedin.com/insight", true)]
    [TestCase("https://hotjar.com/script.js", true)]
    [TestCase("https://mouseflow.com/projects", true)]
    [TestCase("https://segment.io/api", true)]
    [TestCase("https://mixpanel.com/track", true)]
    [TestCase("https://amplitude.com/api", true)]
    [TestCase("https://heap.io/event", true)]
    [TestCase("https://intercom.io/messenger", true)]
    [TestCase("https://crisp.chat/widget", true)]
    [TestCase("https://tawk.to/chat", true)]
    [TestCase("https://ads.google.com/conversion", true)]
    [TestCase("https://adservice.google.com/pagead", true)]
    [TestCase("https://adroll.com/pixel", true)]
    [TestCase("https://example.com/page", false)]
    [TestCase("https://api.example.com/data", false)]
    [TestCase("https://cdn.example.com/script.js", false)]
    public void IsTrackingDomain_DetectsCorrectly(string url, bool isTracking)
    {
        var method = typeof(CrawlerService).GetMethod("IsTrackingDomain",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (bool)method?.Invoke(null, [url])!;

        Assert.That(result, Is.EqualTo(isTracking));
    }

    [TestCase("not-a-url", false)]
    [TestCase("", false)]
    public void IsTrackingDomain_HandlesInvalidUrls(string url, bool expected)
    {
        var method = typeof(CrawlerService).GetMethod("IsTrackingDomain",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (bool)method?.Invoke(null, [url])!;

        Assert.That(result, Is.EqualTo(expected));
    }

    #endregion

    #region Authentication Detection Extended Tests

    [TestCase("https://example.com/login", "<html></html>", true)]
    [TestCase("https://example.com/signin", "<html></html>", true)]
    [TestCase("https://example.com/sign-in", "<html></html>", true)]
    [TestCase("https://example.com/auth/callback", "<html></html>", true)]
    [TestCase("https://example.com/sso/login", "<html></html>", true)]
    public void IsAuthenticationPage_DetectsAuthPatterns(string url, string content, bool expected)
    {
        var method = typeof(CrawlerService).GetMethod("IsAuthenticationPage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (bool)method?.Invoke(null, [url.ToLowerInvariant(), content])!;

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("https://example.com/logout", "<html></html>")]
    public void IsAuthenticationPage_DoesNotDetectLogout(string url, string content)
    {
        var method = typeof(CrawlerService).GetMethod("IsAuthenticationPage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (bool)method?.Invoke(null, [url.ToLowerInvariant(), content])!;

        // Logout pages are NOT detected as auth pages
        Assert.That(result, Is.False);
    }

    [TestCase("https://example.com/forgot-password", "<html></html>")]
    [TestCase("https://example.com/reset-password", "<html></html>")]
    public void IsAuthenticationPage_DetectsPasswordInUrl(string url, string content)
    {
        var method = typeof(CrawlerService).GetMethod("IsAuthenticationPage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (bool)method?.Invoke(null, [url.ToLowerInvariant(), content])!;

        // Password reset pages ARE detected because "password" is in AuthIndicators
        Assert.That(result, Is.True);
    }

    [TestCase("https://example.com/page", "<html><form><input type='password' name='pwd'><button>Sign in</button></form></html>", true)]
    [TestCase("https://example.com/page", "<html><div class='login-form'><input type='password'></div></html>", true)]
    public void IsAuthenticationPage_DetectsPasswordFieldsWithLoginContext(string url, string content, bool expected)
    {
        var method = typeof(CrawlerService).GetMethod("IsAuthenticationPage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (bool)method?.Invoke(null, [url.ToLowerInvariant(), content])!;

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("https://example.com/page", "<html><input type='password'></html>", false)] // Password but no login context
    [TestCase("https://example.com/page", "<html>Login to continue</html>", false)] // Login text but no password field
    public void IsAuthenticationPage_RequiresBothPasswordAndLoginContext(string url, string content, bool expected)
    {
        var method = typeof(CrawlerService).GetMethod("IsAuthenticationPage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (bool)method?.Invoke(null, [url.ToLowerInvariant(), content])!;

        Assert.That(result, Is.EqualTo(expected));
    }

    #endregion

    #region URL Normalization Extended Tests

    [TestCase("https://example.com?query=1#fragment", "https://example.com/?query=1")]
    [TestCase("https://example.com/path?a=1&b=2#section", "https://example.com/path?a=1&b=2")]
    [TestCase("HTTPS://Example.COM/Path/To/Page", "https://example.com/Path/To/Page")]
    [TestCase("https://example.com:8080/page", "https://example.com:8080/page")]
    [TestCase("http://example.com:8080/page", "http://example.com:8080/page")]
    public void NormalizeUrl_AdditionalCases(string input, string expected)
    {
        var method = typeof(CrawlerService).GetMethod("NormalizeUrl",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = method?.Invoke(null, [input]) as string;

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("data:image/png;base64,ABC123")]
    [TestCase("blob:https://example.com/123")]
    [TestCase("tel:+1234567890")]
    [TestCase("about:blank")]
    public void NormalizeUrl_RejectsSpecialSchemes(string input)
    {
        var method = typeof(CrawlerService).GetMethod("NormalizeUrl",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = method?.Invoke(null, [input]) as string;

        Assert.That(result, Is.EqualTo(string.Empty));
    }

    #endregion

    #region Path Matching Extended Tests

    [TestCase("/", "/", true)]
    [TestCase("/page", "/", true)]
    [TestCase("/page/sub", "/page", true)]
    [TestCase("/different", "/page", false)]
    public void GetMatchLength_RootPathCases(string path, string pattern, bool shouldMatch)
    {
        var method = typeof(CrawlerService).GetMethod("GetMatchLength",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (int)method?.Invoke(null, [path, pattern])!;

        Assert.That(result > 0, Is.EqualTo(shouldMatch));
    }

    [TestCase("/api/v1/users", "/api/*", true)]
    [TestCase("/api/v2/users", "/api/*", true)]
    [TestCase("/different/path", "/api/*", false)]
    public void GetMatchLength_TrailingWildcard(string path, string pattern, bool shouldMatch)
    {
        // Implementation only supports trailing wildcard
        var method = typeof(CrawlerService).GetMethod("GetMatchLength",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (int)method?.Invoke(null, [path, pattern])!;

        Assert.That(result > 0, Is.EqualTo(shouldMatch));
    }

    [TestCase("/exact/path$", "/exact/path", true)]
    [TestCase("/exact/path$", "/exact/path/subpath", false)]
    public void GetMatchLength_ExactMatchWithDollar(string pattern, string path, bool shouldMatch)
    {
        // $ suffix requires exact match
        var method = typeof(CrawlerService).GetMethod("GetMatchLength",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (int)method?.Invoke(null, [path, pattern])!;

        Assert.That(result > 0, Is.EqualTo(shouldMatch));
    }

    [TestCase("", "/admin", false)]
    [TestCase("/admin", "", false)]
    [TestCase("", "", false)]
    public void GetMatchLength_EmptyStrings(string path, string pattern, bool shouldMatch)
    {
        var method = typeof(CrawlerService).GetMethod("GetMatchLength",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (int)method?.Invoke(null, [path, pattern])!;

        Assert.That(result > 0, Is.EqualTo(shouldMatch));
    }

    #endregion

    #region CrawlerOptions Extended Tests

    [Test]
    public void CrawlerOptions_SecuritySettingsHaveDefaults()
    {
        var options = new CrawlerOptions();

        Assert.That(options.EnableSandbox, Is.True); // Default true for security; Docker overrides to false
        Assert.That(options.RestrictToTargetDomain, Is.True);
        Assert.That(options.BlockTrackingDomains, Is.True);
        Assert.That(options.MaxRequestsPerPage, Is.EqualTo(500));
        Assert.That(options.MaxRequestSizeBytes, Is.EqualTo(50 * 1024 * 1024L)); // 50MB
        Assert.That(options.AllowedExternalDomains, Is.Not.Null);
        Assert.That(options.AllowedExternalDomains, Is.Empty);
    }

    [Test]
    public void CrawlerOptions_AllowedExternalDomainsCanBeSet()
    {
        var options = new CrawlerOptions
        {
            AllowedExternalDomains = ["cdn.example.com", "api.service.com"]
        };

        Assert.That(options.AllowedExternalDomains.Length, Is.EqualTo(2));
        Assert.That(options.AllowedExternalDomains, Does.Contain("cdn.example.com"));
        Assert.That(options.AllowedExternalDomains, Does.Contain("api.service.com"));
    }

    #endregion

    #region CrawlResult Extended Tests

    [Test]
    public void CrawlResult_CanSetAllProperties()
    {
        var result = new CrawlResult
        {
            BaseUrl = "https://example.com",
            Success = true,
            ErrorMessage = null,
            PagesScanned = 5,
            PagesDiscovered = 10,
            DetectedImages =
            [
                new DetectedImage { Url = "https://example.com/img1.jpg", MimeType = "image/jpeg", Size = 1000 },
                new DetectedImage { Url = "https://example.com/img2.png", MimeType = "image/png", Size = 2000 }
            ],
            TotalDuration = TimeSpan.FromSeconds(10),
            ReachedPageLimit = false,
            ImageToPagesMap = new Dictionary<string, List<string>>
            {
                { "https://example.com/img1.jpg", ["https://example.com/page1"] }
            }
        };

        Assert.That(result.BaseUrl, Is.EqualTo("https://example.com"));
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
        Assert.That(result.PagesScanned, Is.EqualTo(5));
        Assert.That(result.PagesDiscovered, Is.EqualTo(10));
        Assert.That(result.DetectedImages.Count, Is.EqualTo(2));
        Assert.That(result.TotalDuration, Is.EqualTo(TimeSpan.FromSeconds(10)));
        Assert.That(result.ReachedPageLimit, Is.False);
        Assert.That(result.ImageToPagesMap, Has.Exactly(1).Items);
    }

    [Test]
    public void CrawlResult_NonWebPImages_CanBeSet()
    {
        var result = new CrawlResult
        {
            NonWebPImages = [new DetectedImage { Url = "https://example.com/img1.jpg", MimeType = "image/jpeg", Size = 1000 }]
        };

        Assert.That(result.NonWebPImages, Has.Exactly(1).Items);
        Assert.That(result.NonWebPImages[0].MimeType, Is.EqualTo("image/jpeg"));
    }

    [Test]
    public void PageCrawlResult_CanSetAllProperties()
    {
        var result = new PageCrawlResult
        {
            Url = "https://example.com/page",
            Success = true,
            StatusCode = 200,
            ErrorMessage = null,
            DiscoveredUrls = ["https://example.com/link1", "https://example.com/link2"],
            DetectedImages = [new DetectedImage { Url = "https://example.com/img.jpg", MimeType = "image/jpeg", Size = 500 }],
            IsAuthenticationPage = false,
            CrawlDuration = TimeSpan.FromMilliseconds(500)
        };

        Assert.That(result.Url, Is.EqualTo("https://example.com/page"));
        Assert.That(result.Success, Is.True);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        Assert.That(result.ErrorMessage, Is.Null);
        Assert.That(result.DiscoveredUrls.Count, Is.EqualTo(2));
        Assert.That(result.DetectedImages, Has.Exactly(1).Items);
        Assert.That(result.IsAuthenticationPage, Is.False);
        Assert.That(result.CrawlDuration, Is.EqualTo(TimeSpan.FromMilliseconds(500)));
    }

    #endregion

    #region DetectedImage Extended Tests

    [Test]
    public void DetectedImage_CanSetDimensions()
    {
        var image = new DetectedImage
        {
            Url = "https://example.com/image.jpg",
            MimeType = "image/jpeg",
            Size = 50000,
            Width = 1920,
            Height = 1080
        };

        Assert.That(image.Width, Is.EqualTo(1920));
        Assert.That(image.Height, Is.EqualTo(1080));
    }

    [Test]
    public void DetectedImage_DimensionsNullByDefault()
    {
        var image = new DetectedImage();

        Assert.That(image.Width, Is.Null);
        Assert.That(image.Height, Is.Null);
    }

    #endregion

    #region CrawlProgress Extended Tests

    [Test]
    public void CrawlProgress_CanSetAllProperties()
    {
        var progress = new CrawlProgress
        {
            Type = CrawlProgressType.ImageFound,
            CurrentUrl = "https://example.com/image.jpg",
            PageUrl = "https://example.com/page1",
            PagesScanned = 5,
            PagesDiscovered = 10,
            NonWebPImagesFound = 3
        };

        Assert.That(progress.Type, Is.EqualTo(CrawlProgressType.ImageFound));
        Assert.That(progress.CurrentUrl, Is.EqualTo("https://example.com/image.jpg"));
        Assert.That(progress.PageUrl, Is.EqualTo("https://example.com/page1"));
        Assert.That(progress.PagesScanned, Is.EqualTo(5));
        Assert.That(progress.PagesDiscovered, Is.EqualTo(10));
        Assert.That(progress.NonWebPImagesFound, Is.EqualTo(3));
    }

    #endregion

    #region Service Lifecycle Tests

    [Test]
    public void CrawlerService_CanBeDisposedMultipleTimes()
    {
        var service = new CrawlerService(_optionsMock.Object, _securityOptionsMock.Object, _loggerMock.Object, _imageAnalyzerMock.Object, _validationServiceMock.Object);

        // Should not throw
        service.Dispose();
        service.Dispose();
    }

    [Test]
    public void CrawlerService_DisposesResources()
    {
        using var service = new CrawlerService(_optionsMock.Object, _securityOptionsMock.Object, _loggerMock.Object, _imageAnalyzerMock.Object, _validationServiceMock.Object);

        // Service should be disposable and not throw when disposed
        Assert.That(service, Is.AssignableTo<IDisposable>());
    }

    #endregion
}
