using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SendGrid;
using SendGrid.Helpers.Mail;
using WebPScanner.Core.Configuration;
using WebPScanner.Core.Models;
using WebPScanner.Core.Services;

namespace WebPScanner.Core.Tests;

public class EmailServiceTests
{
    private Mock<ISendGridClient> _mockSendGridClient = null!;
    private Mock<ILogger<EmailService>> _mockLogger = null!;
    private EmailOptions _defaultOptions = null!;

    [SetUp]
    public void SetUp()
    {
        _mockSendGridClient = new Mock<ISendGridClient>();
        _mockLogger = new Mock<ILogger<EmailService>>();
        _defaultOptions = new EmailOptions
        {
            FromEmail = "test@example.com",
            FromName = "Test Sender",
            MaxRetries = 2,
            RetryDelayMinutes = 0, // No delay for tests
            Enabled = true,
            MaxAttachmentSizeMb = 10
        };
    }

    private EmailService CreateService(EmailOptions? options = null)
    {
        var opts = Options.Create(options ?? _defaultOptions);
        return new EmailService(opts, _mockLogger.Object, _mockSendGridClient.Object);
    }

    private static PdfReportData CreateTestReportData()
    {
        return new PdfReportData
        {
            ScanId = Guid.NewGuid(),
            TargetUrl = "https://example.com",
            ScanDate = DateTime.UtcNow,
            PagesScanned = 10,
            PagesDiscovered = 15,
            CrawlDuration = TimeSpan.FromSeconds(30),
            ReachedPageLimit = false,
            SavingsSummary = new SavingsSummary
            {
                TotalImages = 5,
                ConvertibleImages = 3,
                TotalOriginalSize = 500000,
                TotalEstimatedWebPSize = 300000,
                TotalSavingsBytes = 200000,
                TotalSavingsPercentage = 40
            }
        };
    }

    private static Response CreateSuccessResponse(string? messageId = "test-message-id")
    {
        var httpResponse = new HttpResponseMessage(HttpStatusCode.Accepted);
        if (messageId != null)
        {
            httpResponse.Headers.Add("X-Message-Id", messageId);
        }
        return new Response(HttpStatusCode.Accepted, null, httpResponse.Headers);
    }

    private static Response CreateErrorResponse(HttpStatusCode statusCode, string body = "Error")
    {
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(body));
        return new Response(statusCode, new StreamContent(stream), null);
    }

    #region SendScanReportAsync Tests

    [Test]
    public async Task SendScanReportAsync_WhenEmailDisabled_ReturnsSuccessWithDisabledMessage()
    {
        // Arrange
        var options = new EmailOptions { Enabled = false };
        var service = CreateService(options);
        var reportData = CreateTestReportData();
        var pdfReport = new byte[] { 1, 2, 3 };

        // Act
        var result = await service.SendScanReportAsync("user@example.com", reportData, pdfReport);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.MessageId, Is.EqualTo("disabled"));
        _mockSendGridClient.Verify(
            c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task SendScanReportAsync_WhenSuccessful_ReturnsSuccessResult()
    {
        // Arrange
        var service = CreateService();
        var reportData = CreateTestReportData();
        var pdfReport = new byte[] { 1, 2, 3 };

        _mockSendGridClient
            .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResponse("msg-123"));

        // Act
        var result = await service.SendScanReportAsync("user@example.com", reportData, pdfReport);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.MessageId, Is.EqualTo("msg-123"));
        Assert.That(result.RetryAttempts, Is.EqualTo(0));
    }

    [Test]
    public async Task SendScanReportAsync_IncludesCorrectEmailContent()
    {
        // Arrange
        var service = CreateService();
        var reportData = CreateTestReportData();
        var pdfReport = new byte[] { 1, 2, 3 };
        SendGridMessage? capturedMessage = null;

        _mockSendGridClient
            .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SendGridMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .ReturnsAsync(CreateSuccessResponse());

        // Act
        await service.SendScanReportAsync("user@example.com", reportData, pdfReport);

        // Assert
        Assert.That(capturedMessage, Is.Not.Null);
        Assert.That(capturedMessage!.From.Email, Is.EqualTo("test@example.com"));
        Assert.That(capturedMessage.From.Name, Is.EqualTo("Test Sender"));
        Assert.That(capturedMessage.Subject, Does.Contain("WebP Scan Report"));
        Assert.That(capturedMessage.PlainTextContent, Is.Not.Null);
        Assert.That(capturedMessage.HtmlContent, Is.Not.Null);
        Assert.That(capturedMessage.Subject, Does.Contain("example.com"));
    }

    [Test]
    public async Task SendScanReportAsync_IncludesPdfAttachment()
    {
        // Arrange
        var service = CreateService();
        var reportData = CreateTestReportData();
        var pdfReport = new byte[] { 1, 2, 3, 4, 5 };
        SendGridMessage? capturedMessage = null;

        _mockSendGridClient
            .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SendGridMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .ReturnsAsync(CreateSuccessResponse());

        // Act
        await service.SendScanReportAsync("user@example.com", reportData, pdfReport);

        // Assert
        Assert.That(capturedMessage, Is.Not.Null);
        Assert.That(capturedMessage!.Attachments, Has.Count.EqualTo(1));
        var attachment = capturedMessage.Attachments[0];
        Assert.That(attachment.Type, Is.EqualTo("application/pdf"));
        Assert.That(attachment.Filename, Does.Contain(".pdf"));
        Assert.That(attachment.Content, Is.EqualTo(Convert.ToBase64String(pdfReport)));
    }

    [Test]
    public async Task SendScanReportAsync_WhenAttachmentTooLarge_ReturnsFailure()
    {
        // Arrange
        var options = new EmailOptions
        {
            Enabled = true,
            MaxAttachmentSizeMb = 1
        };
        var service = CreateService(options);
        var reportData = CreateTestReportData();
        // Create a 2MB file (larger than 1MB limit)
        var pdfReport = new byte[2 * 1024 * 1024];

        // Act
        var result = await service.SendScanReportAsync("user@example.com", reportData, pdfReport);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("exceeds maximum"));
        _mockSendGridClient.Verify(
            c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task SendScanReportAsync_WhenServerError_RetriesAndSucceeds()
    {
        // Arrange
        var service = CreateService();
        var reportData = CreateTestReportData();
        var pdfReport = new byte[] { 1, 2, 3 };
        var callCount = 0;

        _mockSendGridClient
            .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? CreateErrorResponse(HttpStatusCode.InternalServerError) : CreateSuccessResponse();
            });

        // Act
        var result = await service.SendScanReportAsync("user@example.com", reportData, pdfReport);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.RetryAttempts, Is.EqualTo(1));
        Assert.That(callCount, Is.EqualTo(2));
    }

    [Test]
    public async Task SendScanReportAsync_WhenClientError_DoesNotRetry()
    {
        // Arrange
        var service = CreateService();
        var reportData = CreateTestReportData();
        var pdfReport = new byte[] { 1, 2, 3 };

        _mockSendGridClient
            .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid email address"));

        // Act
        var result = await service.SendScanReportAsync("user@example.com", reportData, pdfReport);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("BadRequest"));
        _mockSendGridClient.Verify(
            c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task SendScanReportAsync_WhenRateLimited_Retries()
    {
        // Arrange
        var service = CreateService();
        var reportData = CreateTestReportData();
        var pdfReport = new byte[] { 1, 2, 3 };
        var callCount = 0;

        _mockSendGridClient
            .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? CreateErrorResponse(HttpStatusCode.TooManyRequests) : CreateSuccessResponse();
            });

        // Act
        var result = await service.SendScanReportAsync("user@example.com", reportData, pdfReport);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.RetryAttempts, Is.EqualTo(1));
    }

    [Test]
    public async Task SendScanReportAsync_WhenExceptionThrown_RetriesAndFails()
    {
        // Arrange
        var service = CreateService();
        var reportData = CreateTestReportData();
        var pdfReport = new byte[] { 1, 2, 3 };

        _mockSendGridClient
            .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await service.SendScanReportAsync("user@example.com", reportData, pdfReport);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Network error"));
        Assert.That(result.RetryAttempts, Is.EqualTo(2)); // MaxRetries = 2, so 3 total attempts, 2 retries
        _mockSendGridClient.Verify(
            c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Test]
    public async Task SendScanReportAsync_WhenAllRetriesFail_ReturnsFailure()
    {
        // Arrange
        var service = CreateService();
        var reportData = CreateTestReportData();
        var pdfReport = new byte[] { 1, 2, 3 };

        _mockSendGridClient
            .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateErrorResponse(HttpStatusCode.ServiceUnavailable));

        // Act
        var result = await service.SendScanReportAsync("user@example.com", reportData, pdfReport);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.RetryAttempts, Is.EqualTo(2));
        _mockSendGridClient.Verify(
            c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    #endregion

    #region SendScanFailedNotificationAsync Tests

    [Test]
    public async Task SendScanFailedNotificationAsync_WhenEmailDisabled_ReturnsSuccessWithDisabledMessage()
    {
        // Arrange
        var options = new EmailOptions { Enabled = false };
        var service = CreateService(options);

        // Act
        var result = await service.SendScanFailedNotificationAsync(
            "user@example.com",
            "https://example.com",
            Guid.NewGuid(),
            "Timeout error");

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.MessageId, Is.EqualTo("disabled"));
    }

    [Test]
    public async Task SendScanFailedNotificationAsync_WhenSuccessful_ReturnsSuccessResult()
    {
        // Arrange
        var service = CreateService();
        var scanId = Guid.NewGuid();

        _mockSendGridClient
            .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSuccessResponse("fail-msg-123"));

        // Act
        var result = await service.SendScanFailedNotificationAsync(
            "user@example.com",
            "https://example.com",
            scanId,
            "Connection timeout");

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.MessageId, Is.EqualTo("fail-msg-123"));
    }

    [Test]
    public async Task SendScanFailedNotificationAsync_IncludesErrorDetailsInEmail()
    {
        // Arrange
        var service = CreateService();
        var scanId = Guid.NewGuid();
        SendGridMessage? capturedMessage = null;

        _mockSendGridClient
            .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SendGridMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .ReturnsAsync(CreateSuccessResponse());

        // Act
        await service.SendScanFailedNotificationAsync(
            "user@example.com",
            "https://example.com",
            scanId,
            "The website blocked our crawler");

        // Assert
        Assert.That(capturedMessage, Is.Not.Null);
        Assert.That(capturedMessage!.Subject, Does.Contain("Failed"));
        Assert.That(capturedMessage.PlainTextContent, Does.Contain("The website blocked our crawler"));
        Assert.That(capturedMessage.HtmlContent, Does.Contain("The website blocked our crawler"));
        Assert.That(capturedMessage.PlainTextContent, Does.Contain(scanId.ToString()));
    }

    [Test]
    public async Task SendScanFailedNotificationAsync_DoesNotIncludeAttachment()
    {
        // Arrange
        var service = CreateService();
        SendGridMessage? capturedMessage = null;

        _mockSendGridClient
            .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SendGridMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .ReturnsAsync(CreateSuccessResponse());

        // Act
        await service.SendScanFailedNotificationAsync(
            "user@example.com",
            "https://example.com",
            Guid.NewGuid(),
            "Error");

        // Assert
        Assert.That(capturedMessage, Is.Not.Null);
        Assert.That(capturedMessage!.Attachments, Is.Null);
    }

    #endregion

    #region EmailResult Tests

    [Test]
    public void EmailResult_Succeeded_CreatesSuccessResult()
    {
        // Act
        var result = EmailResult.Succeeded("msg-id", 2);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.MessageId, Is.EqualTo("msg-id"));
        Assert.That(result.RetryAttempts, Is.EqualTo(2));
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void EmailResult_Failed_CreatesFailureResult()
    {
        // Act
        var result = EmailResult.Failed("Something went wrong", 3);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Something went wrong"));
        Assert.That(result.RetryAttempts, Is.EqualTo(3));
        Assert.That(result.MessageId, Is.Null);
    }

    #endregion

    #region EmailOptions Tests

    [Test]
    public void EmailOptions_HasCorrectDefaults()
    {
        // Act
        var options = new EmailOptions();

        // Assert
        Assert.That(EmailOptions.SectionName, Is.EqualTo("Email"));
        Assert.That(options.FromEmail, Is.EqualTo("noreply@example.com"));
        Assert.That(options.FromName, Is.EqualTo("WebP Scanner"));
        Assert.That(options.MaxRetries, Is.EqualTo(3));
        Assert.That(options.RetryDelayMinutes, Is.EqualTo(5));
        Assert.That(options.Enabled, Is.True);
        Assert.That(options.MaxAttachmentSizeMb, Is.EqualTo(10));
        Assert.That(options.ApiKey, Is.Null);
    }

    #endregion

    #region Long URL Truncation Tests

    [Test]
    public async Task SendScanReportAsync_TruncatesLongUrlInSubject()
    {
        // Arrange
        var service = CreateService();
        var reportData = CreateTestReportData();
        reportData.TargetUrl = "https://example.com/very/long/path/that/exceeds/fifty/characters/in/total";
        var pdfReport = new byte[] { 1, 2, 3 };
        SendGridMessage? capturedMessage = null;

        _mockSendGridClient
            .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SendGridMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .ReturnsAsync(CreateSuccessResponse());

        // Act
        await service.SendScanReportAsync("user@example.com", reportData, pdfReport);

        // Assert
        Assert.That(capturedMessage, Is.Not.Null);
        // The subject should contain a truncated URL with "..."
        Assert.That(capturedMessage!.Subject, Does.Contain("..."));
        // But the body should contain the full URL
        Assert.That(capturedMessage.HtmlContent, Does.Contain(reportData.TargetUrl));
    }

    #endregion

    #region HTML Escaping Tests

    [Test]
    public async Task SendScanReportAsync_EscapesHtmlInUrl()
    {
        // Arrange
        var service = CreateService();
        var reportData = CreateTestReportData();
        reportData.TargetUrl = "https://example.com/?query=<script>alert('xss')</script>";
        var pdfReport = new byte[] { 1, 2, 3 };
        SendGridMessage? capturedMessage = null;

        _mockSendGridClient
            .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SendGridMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .ReturnsAsync(CreateSuccessResponse());

        // Act
        await service.SendScanReportAsync("user@example.com", reportData, pdfReport);

        // Assert
        Assert.That(capturedMessage, Is.Not.Null);
        // HTML should not contain unescaped script tags
        Assert.That(capturedMessage!.HtmlContent, Does.Not.Contain("<script>"));
        Assert.That(capturedMessage.HtmlContent, Does.Contain("&lt;script&gt;"));
    }

    [Test]
    public async Task SendScanFailedNotificationAsync_EscapesHtmlInErrorMessage()
    {
        // Arrange
        var service = CreateService();
        SendGridMessage? capturedMessage = null;

        _mockSendGridClient
            .Setup(c => c.SendEmailAsync(It.IsAny<SendGridMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SendGridMessage, CancellationToken>((msg, _) => capturedMessage = msg)
            .ReturnsAsync(CreateSuccessResponse());

        // Act
        await service.SendScanFailedNotificationAsync(
            "user@example.com",
            "https://example.com",
            Guid.NewGuid(),
            "Error: <b>dangerous</b> HTML");

        // Assert
        Assert.That(capturedMessage, Is.Not.Null);
        Assert.That(capturedMessage!.HtmlContent, Does.Not.Contain("<b>dangerous</b>"));
        Assert.That(capturedMessage.HtmlContent, Does.Contain("&lt;b&gt;dangerous&lt;/b&gt;"));
    }

    #endregion

    #region Without SendGrid Client Tests

    [Test]
    public async Task SendScanReportAsync_WithoutApiKey_ReturnsFailure()
    {
        // Arrange - Create service without SendGrid client
        var options = Options.Create(new EmailOptions { Enabled = true, ApiKey = null });
        var service = new EmailService(options, _mockLogger.Object);
        var reportData = CreateTestReportData();
        var pdfReport = new byte[] { 1, 2, 3 };

        // Act
        var result = await service.SendScanReportAsync("user@example.com", reportData, pdfReport);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("API key not configured"));
    }

    [Test]
    public async Task SendScanFailedNotificationAsync_WithoutApiKey_ReturnsFailure()
    {
        // Arrange - Create service without SendGrid client
        var options = Options.Create(new EmailOptions { Enabled = true, ApiKey = null });
        var service = new EmailService(options, _mockLogger.Object);

        // Act
        var result = await service.SendScanFailedNotificationAsync(
            "user@example.com",
            "https://example.com",
            Guid.NewGuid(),
            "Error");

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("API key not configured"));
    }

    #endregion
}
