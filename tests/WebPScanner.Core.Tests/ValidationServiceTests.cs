using WebPScanner.Core.Services;

namespace WebPScanner.Core.Tests;

[TestFixture]
public class ValidationServiceTests
{
    private readonly ValidationService _validationService = new();

    #region URL Validation Tests

    [TestCase("https://example.com")]
    [TestCase("http://example.com")]
    [TestCase("https://www.example.com")]
    [TestCase("https://example.com/path")]
    [TestCase("https://example.com/path?query=value")]
    [TestCase("https://www.google.com")]
    public async Task ValidateScanRequest_ValidUrls_ReturnsSuccess(string url)
    {
        var result = await _validationService.ValidateScanRequestAsync(url, null);
        Assert.That(result.IsValid, Is.True, $"Expected valid for: {url}. Errors: {string.Join(", ", result.Errors)}");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task ValidateScanRequest_NullOrEmptyUrl_ReturnsFailure(string? url)
    {
        var result = await _validationService.ValidateScanRequestAsync(url, null);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors[0], Does.Contain("URL is required"));
    }

    [TestCase("not-a-url")]
    [TestCase("just some text")]
    [TestCase("ftp://example.com")]
    [TestCase("file:///etc/passwd")]
    [TestCase("javascript:alert(1)")]
    public async Task ValidateScanRequest_InvalidUrlFormat_ReturnsFailure(string url)
    {
        var result = await _validationService.ValidateScanRequestAsync(url, null);
        Assert.That(result.IsValid, Is.False);
    }

    [TestCase("ftp://example.com")]
    [TestCase("file:///etc/passwd")]
    [TestCase("ssh://example.com")]
    [TestCase("mailto:test@example.com")]
    public async Task ValidateScanRequest_NonHttpSchemes_ReturnsFailure(string url)
    {
        var result = await _validationService.ValidateScanRequestAsync(url, null);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.Contains("HTTP") || e.Contains("Invalid")), Is.True);
    }

    [TestCase("http://localhost")]
    [TestCase("https://localhost")]
    [TestCase("http://localhost:8080")]
    [TestCase("http://127.0.0.1")]
    [TestCase("https://127.0.0.1")]
    [TestCase("http://127.0.0.1:3000")]
    [TestCase("http://[::1]")]
    public async Task ValidateScanRequest_Localhost_ReturnsFailure(string url)
    {
        var result = await _validationService.ValidateScanRequestAsync(url, null);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.Contains("Localhost") || e.Contains("localhost") || e.Contains("private") || e.Contains("internal")), Is.True);
    }

    #endregion

    #region SSRF Prevention Tests

    [TestCase("http://10.0.0.1")]
    [TestCase("http://10.255.255.255")]
    [TestCase("http://10.1.2.3")]
    public async Task ValidateScanRequest_PrivateIp10Range_ReturnsFailure(string url)
    {
        var result = await _validationService.ValidateScanRequestAsync(url, null);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.Contains("private") || e.Contains("internal") || e.Contains("Private")), Is.True);
    }

    [TestCase("http://172.16.0.1")]
    [TestCase("http://172.31.255.255")]
    [TestCase("http://172.20.10.5")]
    public async Task ValidateScanRequest_PrivateIp172Range_ReturnsFailure(string url)
    {
        var result = await _validationService.ValidateScanRequestAsync(url, null);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.Contains("private") || e.Contains("internal") || e.Contains("Private")), Is.True);
    }

    [TestCase("http://192.168.0.1")]
    [TestCase("http://192.168.1.1")]
    [TestCase("http://192.168.255.255")]
    public async Task ValidateScanRequest_PrivateIp192Range_ReturnsFailure(string url)
    {
        var result = await _validationService.ValidateScanRequestAsync(url, null);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.Contains("private") || e.Contains("internal") || e.Contains("Private")), Is.True);
    }

    [TestCase("http://169.254.0.1")]
    [TestCase("http://169.254.169.254")] // AWS metadata endpoint
    [TestCase("http://169.254.255.255")]
    public async Task ValidateScanRequest_LinkLocalIp_ReturnsFailure(string url)
    {
        var result = await _validationService.ValidateScanRequestAsync(url, null);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.Contains("private") || e.Contains("internal") || e.Contains("Private")), Is.True);
    }

    [TestCase("http://0.0.0.0")]
    [TestCase("http://0.0.0.1")]
    public async Task ValidateScanRequest_ZeroNetwork_ReturnsFailure(string url)
    {
        var result = await _validationService.ValidateScanRequestAsync(url, null);
        Assert.That(result.IsValid, Is.False);
    }

    [TestCase("http://224.0.0.1")] // Multicast
    [TestCase("http://239.255.255.255")]
    [TestCase("http://240.0.0.1")] // Reserved
    [TestCase("http://255.255.255.255")] // Broadcast
    public async Task ValidateScanRequest_SpecialIpRanges_ReturnsFailure(string url)
    {
        var result = await _validationService.ValidateScanRequestAsync(url, null);
        Assert.That(result.IsValid, Is.False);
    }

    #endregion

    #region Email Validation Tests

    [TestCase("test@example.com")]
    [TestCase("user.name@domain.com")]
    [TestCase("user+tag@example.org")]
    [TestCase("test@subdomain.example.com")]
    [TestCase("test123@example.co.uk")]
    public async Task ValidateScanRequest_ValidEmails_ReturnsSuccess(string email)
    {
        var result = await _validationService.ValidateScanRequestAsync("https://example.com", email);
        Assert.That(result.IsValid, Is.True, $"Expected valid for: {email}. Errors: {string.Join(", ", result.Errors)}");
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task ValidateScanRequest_NullOrEmptyEmail_ReturnsSuccess(string? email)
    {
        // Email is optional - null/empty is valid
        var result = await _validationService.ValidateScanRequestAsync("https://example.com", email);
        Assert.That(result.IsValid, Is.True);
    }

    [TestCase("notanemail")]
    [TestCase("missing@domain")]
    [TestCase("@nodomain.com")]
    [TestCase("double@@at.com")]
    [TestCase("spaces in@email.com")]
    public async Task ValidateScanRequest_InvalidEmailFormat_ReturnsFailure(string email)
    {
        var result = await _validationService.ValidateScanRequestAsync("https://example.com", email);
        Assert.That(result.IsValid, Is.False);
    }

    [Test]
    public async Task ValidateScanRequest_EmailTooLong_ReturnsFailure()
    {
        var longEmail = new string('a', 250) + "@example.com";
        var result = await _validationService.ValidateScanRequestAsync("https://example.com", longEmail);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Any(e => e.Contains("too long")), Is.True);
    }

    #endregion

    #region Combined Validation Tests

    [Test]
    public async Task ValidateScanRequest_ValidInputs_ReturnsSuccess()
    {
        var result = await _validationService.ValidateScanRequestAsync("https://example.com", "test@example.com");
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Errors, Is.Empty);
    }

    [Test]
    public async Task ValidateScanRequest_InvalidUrl_ReturnsFailure()
    {
        var result = await _validationService.ValidateScanRequestAsync("not-a-url", "test@example.com");
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Exactly(1).Items);
    }

    [Test]
    public async Task ValidateScanRequest_InvalidEmail_ReturnsFailure()
    {
        var result = await _validationService.ValidateScanRequestAsync("https://example.com", "not-an-email");
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Exactly(1).Items);
    }

    [Test]
    public async Task ValidateScanRequest_BothInvalid_ReturnsMultipleErrors()
    {
        var result = await _validationService.ValidateScanRequestAsync("not-a-url", "not-an-email");
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors.Count >= 2, Is.True);
    }

    [Test]
    public async Task ValidateScanRequest_NullInputs_ReturnsUrlError()
    {
        // Email is optional, so only URL error is returned
        var result = await _validationService.ValidateScanRequestAsync(null, null);
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Errors, Has.Exactly(1).Items);
        Assert.That(result.Errors[0], Does.Contain("URL"));
    }

    #endregion
}
