using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WebPScanner.Core.Configuration;
using WebPScanner.Core.Enums;
using WebPScanner.Core.Interfaces;
using WebPScanner.Core.Models;
using WebPScanner.Core.Services;

namespace WebPScanner.Core.Tests;

[TestFixture]
public class CrawlerServiceTests
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
            UserAgent = "TestCrawler/1.0"
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

    #region URL Normalization Tests

    [TestCase("https://example.com", "https://example.com/")]
    [TestCase("https://example.com/", "https://example.com/")]
    [TestCase("https://example.com/page", "https://example.com/page")]
    [TestCase("https://example.com/page/", "https://example.com/page")]
    [TestCase("https://example.com/page#section", "https://example.com/page")]
    [TestCase("https://example.com:443/page", "https://example.com/page")]
    [TestCase("http://example.com:80/page", "http://example.com/page")]
    [TestCase("HTTPS://EXAMPLE.COM/Page", "https://example.com/Page")]  // Path case preserved
    public void NormalizeUrl_ProducesConsistentResults(string input, string expected)
    {
        // Use reflection to access the private static method
        var method = typeof(CrawlerService).GetMethod("NormalizeUrl",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = method?.Invoke(null, [input]) as string;

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("not-a-url")]
    [TestCase("ftp://example.com")]
    [TestCase("javascript:void(0)")]
    [TestCase("mailto:test@example.com")]
    public void NormalizeUrl_ReturnsEmptyForInvalidUrls(string? input)
    {
        var method = typeof(CrawlerService).GetMethod("NormalizeUrl",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = method?.Invoke(null, [input]) as string;

        Assert.That(result, Is.EqualTo(string.Empty));
    }

    #endregion

    #region Authentication Detection Tests

    [TestCase("https://example.com/login", "<html></html>", true)]
    [TestCase("https://example.com/signin", "<html></html>", true)]
    [TestCase("https://example.com/auth/callback", "<html></html>", true)]
    [TestCase("https://example.com/account/sign-in", "<html></html>", true)]
    public void IsAuthenticationPage_DetectsAuthUrlPatterns(string url, string content, bool expected)
    {
        var method = typeof(CrawlerService).GetMethod("IsAuthenticationPage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (bool)method?.Invoke(null, [url.ToLowerInvariant(), content])!;

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("https://example.com/page", "<html><input type=\"password\"><span>Login</span></html>", true)]
    [TestCase("https://example.com/page", "<html><input type=\"password\"><span>Sign In</span></html>", true)]
    public void IsAuthenticationPage_DetectsLoginForms(string url, string content, bool expected)
    {
        var method = typeof(CrawlerService).GetMethod("IsAuthenticationPage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (bool)method?.Invoke(null, [url.ToLowerInvariant(), content])!;

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("https://example.com/products", "<html><h1>Products</h1></html>", false)]
    [TestCase("https://example.com/about", "<html><h1>About Us</h1></html>", false)]
    public void IsAuthenticationPage_ReturnsFalseForNormalPages(string url, string content, bool expected)
    {
        var method = typeof(CrawlerService).GetMethod("IsAuthenticationPage",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (bool)method?.Invoke(null, [url.ToLowerInvariant(), content])!;

        Assert.That(result, Is.EqualTo(expected));
    }

    #endregion

    #region Path Matching Tests

    [TestCase("/admin", "/admin", true)]
    [TestCase("/admin/page", "/admin", true)]
    [TestCase("/admin/page/deep", "/admin", true)]
    [TestCase("/public", "/admin", false)]
    [TestCase("/administration", "/admin", true)]
    public void GetMatchLength_HandlesSimplePaths(string path, string pattern, bool shouldMatch)
    {
        var method = typeof(CrawlerService).GetMethod("GetMatchLength",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (int)method?.Invoke(null, [path, pattern])!;

        Assert.That(result > 0, Is.EqualTo(shouldMatch));
    }

    [TestCase("/images/test.jpg", "/images/*", true)]
    [TestCase("/images/sub/test.jpg", "/images/*", true)]
    [TestCase("/other/test.jpg", "/images/*", false)]
    public void GetMatchLength_HandlesWildcardPatterns(string path, string pattern, bool shouldMatch)
    {
        var method = typeof(CrawlerService).GetMethod("GetMatchLength",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (int)method?.Invoke(null, [path, pattern])!;

        Assert.That(result > 0, Is.EqualTo(shouldMatch));
    }

    [TestCase("/exact", "/exact$", true)]
    [TestCase("/exactpath", "/exact$", false)]
    [TestCase("/exact/more", "/exact$", false)]
    public void GetMatchLength_HandlesExactMatchPatterns(string path, string pattern, bool shouldMatch)
    {
        var method = typeof(CrawlerService).GetMethod("GetMatchLength",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (int)method?.Invoke(null, [path, pattern])!;

        Assert.That(result > 0, Is.EqualTo(shouldMatch));
    }

    #endregion

    #region Robots Rules Tests

    [Test]
    public void IsUrlAllowed_AllowsWhenNoRules()
    {
        var rules = CreateRobotsRules([], []);
        var method = typeof(CrawlerService).GetMethod("IsUrlAllowed",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = (bool)method!.Invoke(null, ["https://example.com/page", rules])!;

        Assert.That(result, Is.True);
    }

    [Test]
    public void IsUrlAllowed_BlocksDisallowedPaths()
    {
        var rules = CreateRobotsRules(["/admin", "/private"], []);
        var method = typeof(CrawlerService).GetMethod("IsUrlAllowed",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var adminResult = (bool)method!.Invoke(null, ["https://example.com/admin/page", rules])!;
        var privateResult = (bool)method.Invoke(null, ["https://example.com/private/data", rules])!;
        var publicResult = (bool)method.Invoke(null, ["https://example.com/public", rules])!;

        Assert.That(adminResult, Is.False);
        Assert.That(privateResult, Is.False);
        Assert.That(publicResult, Is.True);
    }

    [Test]
    public void IsUrlAllowed_AllowOverridesDisallow()
    {
        var rules = CreateRobotsRules(
            ["/admin"],
            ["/admin/public"]
        );
        var method = typeof(CrawlerService).GetMethod("IsUrlAllowed",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var publicResult = (bool)method!.Invoke(null, ["https://example.com/admin/public/page", rules])!;
        var privateResult = (bool)method.Invoke(null, ["https://example.com/admin/settings", rules])!;

        Assert.That(publicResult, Is.True);
        Assert.That(privateResult, Is.False);
    }

    private static object CreateRobotsRules(List<string> disallowed, List<string> allowed)
    {
        var rulesType = typeof(CrawlerService).Assembly.GetType("WebPScanner.Core.Models.RobotsRules");
        var rules = Activator.CreateInstance(rulesType!);

        var disallowedProp = rulesType!.GetProperty("DisallowedPaths");
        var allowedProp = rulesType.GetProperty("AllowedPaths");

        var disallowedList = disallowedProp?.GetValue(rules) as List<string>;
        var allowedList = allowedProp?.GetValue(rules) as List<string>;

        disallowedList?.AddRange(disallowed);
        allowedList?.AddRange(allowed);

        return rules!;
    }

    #endregion

    #region CrawlerOptions Tests

    [Test]
    public void CrawlerOptions_HasCorrectDefaults()
    {
        var options = new CrawlerOptions();

        Assert.That(options.MaxPagesPerScan, Is.EqualTo(1000));
        Assert.That(options.PageTimeoutSeconds, Is.EqualTo(30));
        Assert.That(options.NetworkIdleTimeoutMs, Is.EqualTo(500));
        Assert.That(options.DelayBetweenPagesMs, Is.EqualTo(500));
        Assert.That(options.MaxRetries, Is.EqualTo(3));
        Assert.That(options.RespectRobotsTxt, Is.True);
        Assert.That(options.ChromiumPath, Is.Null);
        Assert.That(options.UserAgent, Is.EqualTo("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"));
    }

    [Test]
    public void CrawlerOptions_SectionNameIsCorrect()
    {
        Assert.That(CrawlerOptions.SectionName, Is.EqualTo("Crawler"));
    }

    #endregion

    #region CrawlResult Tests

    [Test]
    public void CrawlResult_InitializesWithDefaults()
    {
        var result = new CrawlResult();

        Assert.That(result.BaseUrl, Is.EqualTo(string.Empty));
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.Null);
        Assert.That(result.PagesScanned, Is.EqualTo(0));
        Assert.That(result.PagesDiscovered, Is.EqualTo(0));
        Assert.That(result.DetectedImages, Is.Empty);
        Assert.That(result.NonWebPImages, Is.Empty);
        Assert.That(result.TotalDuration, Is.EqualTo(TimeSpan.Zero));
        Assert.That(result.ReachedPageLimit, Is.False);
        Assert.That(result.ImageToPagesMap, Is.Empty);
    }

    [Test]
    public void PageCrawlResult_InitializesWithDefaults()
    {
        var result = new PageCrawlResult();

        Assert.That(result.Url, Is.EqualTo(string.Empty));
        Assert.That(result.Success, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(0));
        Assert.That(result.ErrorMessage, Is.Null);
        Assert.That(result.DiscoveredUrls, Is.Empty);
        Assert.That(result.DetectedImages, Is.Empty);
        Assert.That(result.IsAuthenticationPage, Is.False);
        Assert.That(result.CrawlDuration, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void DetectedImage_InitializesWithDefaults()
    {
        var image = new DetectedImage();

        Assert.That(image.Url, Is.EqualTo(string.Empty));
        Assert.That(image.MimeType, Is.EqualTo(string.Empty));
        Assert.That(image.Size, Is.EqualTo(0));
        Assert.That(image.Width, Is.Null);
        Assert.That(image.Height, Is.Null);
    }

    #endregion

    #region CrawlProgress Tests

    [Test]
    public void CrawlProgress_InitializesWithDefaults()
    {
        var progress = new CrawlProgress();

        Assert.That(progress.CurrentUrl, Is.EqualTo(string.Empty));
        Assert.That(progress.PagesScanned, Is.EqualTo(0));
        Assert.That(progress.PagesDiscovered, Is.EqualTo(0));
        Assert.That(progress.NonWebPImagesFound, Is.EqualTo(0));
        Assert.That(progress.Type, Is.EqualTo(CrawlProgressType.PageStarted));
    }

    [TestCase(CrawlProgressType.PageStarted)]
    [TestCase(CrawlProgressType.PageCompleted)]
    [TestCase(CrawlProgressType.ImageFound)]
    [TestCase(CrawlProgressType.CrawlCompleted)]
    [TestCase(CrawlProgressType.CrawlFailed)]
    public void CrawlProgressType_HasAllExpectedValues(CrawlProgressType progressType)
    {
        var progress = new CrawlProgress { Type = progressType };
        Assert.That(progress.Type, Is.EqualTo(progressType));
    }

    #endregion

    #region Interface Implementation Tests

    [Test]
    public void CrawlerService_ImplementsICrawlerService()
    {
        using var service = new CrawlerService(_optionsMock.Object, _securityOptionsMock.Object, _loggerMock.Object, _imageAnalyzerMock.Object, _validationServiceMock.Object);
        Assert.That(service, Is.AssignableTo<ICrawlerService>());
    }

    [Test]
    public void CrawlerService_ImplementsIDisposable()
    {
        using var service = new CrawlerService(_optionsMock.Object, _securityOptionsMock.Object, _loggerMock.Object, _imageAnalyzerMock.Object, _validationServiceMock.Object);
        Assert.That(service, Is.AssignableTo<IDisposable>());
    }

    #endregion
}
