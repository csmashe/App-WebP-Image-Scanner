using Microsoft.Extensions.Logging;
using Moq;
using WebPScanner.Core.Interfaces;
using WebPScanner.Core.Models;
using WebPScanner.Core.Services;

namespace WebPScanner.Core.Tests;

public class ImageAnalyzerServiceTests
{
    private Mock<ILogger<ImageAnalyzerService>> _loggerMock = null!;
    private ImageAnalyzerService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILogger<ImageAnalyzerService>>();
        _service = new ImageAnalyzerService(_loggerMock.Object);
    }

    #region IsNonWebPRasterImage Tests

    [TestCase("image/jpeg", true)]
    [TestCase("image/jpg", true)]
    [TestCase("image/png", true)]
    [TestCase("image/gif", true)]
    [TestCase("image/bmp", true)]
    [TestCase("image/tiff", true)]
    [TestCase("image/x-ms-bmp", true)]
    public void IsNonWebPRasterImage_ReturnsTrue_ForNonWebPRasterTypes(string mimeType, bool expected)
    {
        var result = _service.IsNonWebPRasterImage(mimeType);
        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("image/webp")]
    [TestCase("image/avif")]
    [TestCase("image/svg+xml")]
    [TestCase("image/x-icon")]
    public void IsNonWebPRasterImage_ReturnsFalse_ForOtherImageTypes(string mimeType)
    {
        var result = _service.IsNonWebPRasterImage(mimeType);
        Assert.That(result, Is.False);
    }

    [TestCase("text/html")]
    [TestCase("application/json")]
    [TestCase("text/css")]
    [TestCase("application/javascript")]
    public void IsNonWebPRasterImage_ReturnsFalse_ForNonImageTypes(string mimeType)
    {
        var result = _service.IsNonWebPRasterImage(mimeType);
        Assert.That(result, Is.False);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void IsNonWebPRasterImage_ReturnsFalse_ForNullOrEmptyMimeType(string? mimeType)
    {
        var result = _service.IsNonWebPRasterImage(mimeType!);
        Assert.That(result, Is.False);
    }

    [TestCase("image/jpeg; charset=utf-8", true)]
    [TestCase("image/png; boundary=something", true)]
    [TestCase("image/webp; charset=utf-8", false)]
    public void IsNonWebPRasterImage_HandlesParameterizedMimeTypes(string mimeType, bool expected)
    {
        var result = _service.IsNonWebPRasterImage(mimeType);
        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase("IMAGE/JPEG", true)]
    [TestCase("Image/Png", true)]
    [TestCase("IMAGE/WEBP", false)]
    public void IsNonWebPRasterImage_IsCaseInsensitive(string mimeType, bool expected)
    {
        var result = _service.IsNonWebPRasterImage(mimeType);
        Assert.That(result, Is.EqualTo(expected));
    }

    #endregion

    #region Service Interface Tests

    [Test]
    public void ImageAnalyzerService_ImplementsIImageAnalyzerService()
    {
        Assert.That(_service, Is.AssignableTo<IImageAnalyzerService>());
    }

    #endregion

    #region DetectedImage Model Tests

    [Test]
    public void DetectedImage_DefaultValues_AreCorrect()
    {
        var image = new DetectedImage();

        Assert.That(image.Url, Is.EqualTo(string.Empty));
        Assert.That(image.MimeType, Is.EqualTo(string.Empty));
        Assert.That(image.Size, Is.EqualTo(0));
        Assert.That(image.Width, Is.Null);
        Assert.That(image.Height, Is.Null);
    }

    [Test]
    public void DetectedImage_CanSetAllProperties()
    {
        var image = new DetectedImage
        {
            Url = "https://example.com/image.jpg",
            MimeType = "image/jpeg",
            Size = 12345,
            Width = 800,
            Height = 600
        };

        Assert.That(image.Url, Is.EqualTo("https://example.com/image.jpg"));
        Assert.That(image.MimeType, Is.EqualTo("image/jpeg"));
        Assert.That(image.Size, Is.EqualTo(12345));
        Assert.That(image.Width, Is.EqualTo(800));
        Assert.That(image.Height, Is.EqualTo(600));
    }

    #endregion

    #region MIME Type Edge Cases

    [TestCase("image/jpeg", true)]
    [TestCase("image/pjpeg", false)] // Progressive JPEG variant (not in our list)
    [TestCase("image/x-png", false)] // Non-standard PNG variant
    public void IsNonWebPRasterImage_HandlesMimeTypeVariants(string mimeType, bool expected)
    {
        var result = _service.IsNonWebPRasterImage(mimeType);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void IsNonWebPRasterImage_HandlesWhitespaceInMimeType()
    {
        // MIME type with leading/trailing whitespace before semicolon
        // After splitting by ';', the part before is trimmed
        var result = _service.IsNonWebPRasterImage("  image/jpeg  ; charset=utf-8");
        Assert.That(result, Is.True); // The Trim() handles leading/trailing spaces
    }

    #endregion
}
