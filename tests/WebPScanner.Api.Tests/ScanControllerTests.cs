using System.Net;
using System.Net.Http.Json;
using WebPScanner.Core.DTOs;
using WebPScanner.Core.Enums;

namespace WebPScanner.Api.Tests;

[TestFixture]
public class ScanControllerTests
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
    public async Task SubmitScan_ValidRequest_ReturnsCreatedWithScanId()
    {
        // Arrange
        var request = new ScanRequestDto
        {
            Url = "https://example.com",
            Email = "test@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/scan", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var result = await response.Content.ReadFromJsonAsync<ScanResponseDto>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ScanId, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result.QueuePosition >= 1, Is.True);
        Assert.That(result.Message, Does.Contain("queued successfully").IgnoreCase);
    }

    [Test]
    public async Task SubmitScan_MultipleRequests_IncrementsQueuePosition()
    {
        // Use an isolated factory for this test
        await using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        // Arrange - Use real domains that resolve
        var request1 = new ScanRequestDto { Url = "https://example.com/page1", Email = "test1@example.com" };
        var request2 = new ScanRequestDto { Url = "https://example.org/page2", Email = "test2@example.com" };
        var request3 = new ScanRequestDto { Url = "https://example.net/page3", Email = "test3@example.com" };

        // Act
        var response1 = await client.PostAsJsonAsync("/api/scan", request1);
        var response2 = await client.PostAsJsonAsync("/api/scan", request2);
        var response3 = await client.PostAsJsonAsync("/api/scan", request3);

        // Assert
        var result1 = await response1.Content.ReadFromJsonAsync<ScanResponseDto>();
        var result2 = await response2.Content.ReadFromJsonAsync<ScanResponseDto>();
        var result3 = await response3.Content.ReadFromJsonAsync<ScanResponseDto>();

        Assert.That(result1!.QueuePosition, Is.EqualTo(1));
        Assert.That(result2!.QueuePosition, Is.EqualTo(2));
        Assert.That(result3!.QueuePosition, Is.EqualTo(3));
    }

    [Test]
    public async Task SubmitScan_InvalidUrl_ReturnsBadRequest()
    {
        // Arrange
        var request = new ScanRequestDto
        {
            Url = "not-a-valid-url",
            Email = "test@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/scan", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task SubmitScan_InvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new ScanRequestDto
        {
            Url = "https://example.com",
            Email = "not-an-email"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/scan", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task SubmitScan_LocalhostUrl_ReturnsBadRequest()
    {
        // Arrange
        var request = new ScanRequestDto
        {
            Url = "http://localhost:8080",
            Email = "test@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/scan", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task SubmitScan_PrivateIpUrl_ReturnsBadRequest()
    {
        // Arrange
        var request = new ScanRequestDto
        {
            Url = "http://192.168.1.1",
            Email = "test@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/scan", request);

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task SubmitScan_EmptyBody_ReturnsBadRequest()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/scan", new { });

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetStatus_ExistingScan_ReturnsStatus()
    {
        // Arrange - First create a scan
        var request = new ScanRequestDto
        {
            Url = "https://example.com",
            Email = "test@example.com"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/scan", request);
        var createResult = await createResponse.Content.ReadFromJsonAsync<ScanResponseDto>();

        // Act
        var response = await _client.GetAsync($"/api/scan/{createResult!.ScanId}/status");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var status = await response.Content.ReadFromJsonAsync<ScanStatusDto>();
        Assert.That(status, Is.Not.Null);
        Assert.That(status!.ScanId, Is.EqualTo(createResult.ScanId));
        Assert.That(status.Status, Is.EqualTo(ScanStatus.Queued));
        Assert.That(status.TargetUrl, Is.EqualTo("https://example.com"));
        Assert.That(status.QueuePosition, Is.Not.Null);
    }

    [Test]
    public async Task GetStatus_NonExistentScan_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/scan/{nonExistentId}/status");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetStatus_InvalidGuid_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/scan/not-a-guid/status");

        // Assert
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }
}
