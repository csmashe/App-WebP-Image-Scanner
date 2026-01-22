using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebPScanner.Data.Context;

namespace WebPScanner.E2E.Tests;

/// <summary>
/// Custom web application factory for E2E testing with an in-memory database.
/// </summary>
public class WebApplicationFixture : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove all Entity Framework related services to avoid provider conflicts
            var efServiceTypes = new[]
            {
                typeof(DbContextOptions<WebPScannerDbContext>),
                typeof(DbContextOptions),
                typeof(WebPScannerDbContext)
            };

            var descriptorsToRemove = services
                .Where(d => efServiceTypes.Contains(d.ServiceType) ||
                           (d.ServiceType.IsGenericType &&
                            d.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)) ||
                           d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true ||
                           d.ImplementationType?.FullName?.Contains("Sqlite") == true)
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add an in-memory database for testing with a unique name per instance
            services.AddDbContext<WebPScannerDbContext>(options =>
            {
                options.UseInMemoryDatabase($"E2ETestDb_{Guid.NewGuid()}");
            });
        });
    }
}
