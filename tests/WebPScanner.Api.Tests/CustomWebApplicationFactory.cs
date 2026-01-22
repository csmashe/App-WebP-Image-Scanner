using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebPScanner.Data.Context;

namespace WebPScanner.Api.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<WebPScannerDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Create an in-memory SQLite connection that stays open
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Add DbContext using the in-memory SQLite connection
            services.AddDbContext<WebPScannerDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Remove all background hosted services to prevent flaky tests
            // These services can process queue items between test operations
            var hostedServiceDescriptors = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .ToList();
            foreach (var hostedService in hostedServiceDescriptors)
            {
                services.Remove(hostedService);
            }

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<WebPScannerDbContext>();

            // Ensure the database is created
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Testing");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
        {
            return;
        }

        _connection?.Close();
        _connection?.Dispose();
    }
}
