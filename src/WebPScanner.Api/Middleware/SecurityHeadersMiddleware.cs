namespace WebPScanner.Api.Middleware;

/// <summary>
/// Middleware to add security headers to all responses.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            if (!headers.ContainsKey("X-Frame-Options"))
            {
                headers.Append("X-Frame-Options", "DENY");
            }

            if (!headers.ContainsKey("X-Content-Type-Options"))
            {
                headers.Append("X-Content-Type-Options", "nosniff");
            }

            // Legacy browsers only
            if (!headers.ContainsKey("X-XSS-Protection"))
            {
                headers.Append("X-XSS-Protection", "1; mode=block");
            }

            if (!headers.ContainsKey("Referrer-Policy"))
            {
                headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            }

            // 'unsafe-inline' required for framer-motion inline styles
            // Google domains required for Analytics
            // Sentry domains required for error tracking
            // 'self' covers same-origin WebSocket for SignalR
            if (!headers.ContainsKey("Content-Security-Policy"))
            {
                headers.Append("Content-Security-Policy",
                    "default-src 'self'; " +
                    "script-src 'self' https://www.googletagmanager.com; " +
                    "style-src 'self' 'unsafe-inline'; " +
                    "img-src 'self' data: https:; " +
                    "font-src 'self'; " +
                    "worker-src 'self' blob:; " +
                    "connect-src 'self' https://www.google-analytics.com https://www.googletagmanager.com https://*.ingest.sentry.io https://*.ingest.us.sentry.io; " +
                    "frame-ancestors 'none'; " +
                    "base-uri 'self'; " +
                    "form-action 'self'");
            }

            // Formerly Feature-Policy
            if (!headers.ContainsKey("Permissions-Policy"))
            {
                headers.Append("Permissions-Policy",
                    "accelerometer=(), " +
                    "camera=(), " +
                    "geolocation=(), " +
                    "gyroscope=(), " +
                    "magnetometer=(), " +
                    "microphone=(), " +
                    "payment=(), " +
                    "usb=()");
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }
}

/// <summary>
/// Extension methods for security headers middleware.
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
