using WebPScanner.Core.Configuration;

namespace WebPScanner.Core.Tests;

public class SecurityOptionsTests
{
    [Test]
    public void SecurityOptions_DefaultValues_AreCorrect()
    {
        var options = new SecurityOptions();

        Assert.That(options.MaxRequestsPerMinute, Is.EqualTo(100));
        Assert.That(options.EnforceHttps, Is.True);
        Assert.That(options.MaxScanDurationMinutes, Is.EqualTo(10));
        Assert.That(options.MaxMemoryPerScanMb, Is.EqualTo(512));
        Assert.That(options.RateLimitExemptIps, Is.Empty);
        Assert.That(options.EnableRequestSizeLimit, Is.True);
        Assert.That(options.MaxRequestBodySizeBytes, Is.EqualTo(1024 * 100)); // 100KB
    }

    [Test]
    public void SecurityOptions_SectionName_IsCorrect()
    {
        Assert.That(SecurityOptions.SectionName, Is.EqualTo("Security"));
    }

    [Test]
    public void SecurityOptions_CanSetRateLimitExemptIps()
    {
        var options = new SecurityOptions
        {
            RateLimitExemptIps = ["192.168.1.1", "10.0.0.0/8"]
        };

        Assert.That(options.RateLimitExemptIps.Length, Is.EqualTo(2));
        Assert.That(options.RateLimitExemptIps, Does.Contain("192.168.1.1"));
        Assert.That(options.RateLimitExemptIps, Does.Contain("10.0.0.0/8"));
    }

    [Test]
    public void SecurityOptions_CanSetCustomValues()
    {
        var options = new SecurityOptions
        {
            MaxRequestsPerMinute = 20,
            EnforceHttps = false,
            MaxScanDurationMinutes = 15,
            MaxMemoryPerScanMb = 1024,
            EnableRequestSizeLimit = false,
            MaxRequestBodySizeBytes = 1024 * 1024
        };

        Assert.That(options.MaxRequestsPerMinute, Is.EqualTo(20));
        Assert.That(options.EnforceHttps, Is.False);
        Assert.That(options.MaxScanDurationMinutes, Is.EqualTo(15));
        Assert.That(options.MaxMemoryPerScanMb, Is.EqualTo(1024));
        Assert.That(options.EnableRequestSizeLimit, Is.False);
        Assert.That(options.MaxRequestBodySizeBytes, Is.EqualTo(1024 * 1024));
    }
}
