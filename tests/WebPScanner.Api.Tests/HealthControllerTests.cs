using System.Net;
using System.Net.Http.Json;
using WebPScanner.Core.DTOs;

namespace WebPScanner.Api.Tests;

[TestFixture]
public class HealthControllerTests
{
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Test]
    public async Task GetHealth_ReturnsHealthyStatus()
    {
        // Use isolated factory
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/health");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var health = await response.Content.ReadFromJsonAsync<HealthResponseDto>();
        Assert.That(health, Is.Not.Null);
        Assert.That(health!.Status, Is.EqualTo("Healthy"));
        Assert.That(health.QueuedJobs, Is.EqualTo(0));
        Assert.That(health.ProcessingJobs, Is.EqualTo(0));
    }

    [Test]
    public async Task GetHealth_AfterSubmittingScans_ReturnsCorrectQueueCount()
    {
        // Use isolated factory
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        // Arrange - Submit some scan jobs and verify they are created
        // Use real domains that resolve (example.com, example.org are IANA reserved)
        var request1 = new ScanRequestDto { Url = "https://example.com/page1", Email = "test1@example.com" };
        var request2 = new ScanRequestDto { Url = "https://example.org/page2", Email = "test2@example.com" };

        var response1 = await client.PostAsJsonAsync("/api/scan", request1);
        var response2 = await client.PostAsJsonAsync("/api/scan", request2);

        // Ensure scans were created successfully
        Assert.That(response1.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        Assert.That(response2.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        // Act
        var response = await client.GetAsync("/api/health");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var health = await response.Content.ReadFromJsonAsync<HealthResponseDto>();
        Assert.That(health, Is.Not.Null);
        Assert.That(health!.Status, Is.EqualTo("Healthy"));
        Assert.That(health.QueuedJobs, Is.EqualTo(2));
        Assert.That(health.ProcessingJobs, Is.EqualTo(0));
    }

    [Test]
    public async Task GetHealth_ReturnsTimestamp()
    {
        // Act
        var beforeCall = DateTime.UtcNow.AddSeconds(-1);
        var response = await _client.GetAsync("/api/health");
        var afterCall = DateTime.UtcNow.AddSeconds(1);

        // Assert
        var health = await response.Content.ReadFromJsonAsync<HealthResponseDto>();
        Assert.That(health, Is.Not.Null);
        Assert.That(health!.Timestamp >= beforeCall && health.Timestamp <= afterCall, Is.True);
    }
}
