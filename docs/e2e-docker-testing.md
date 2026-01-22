# E2E Testing with Docker for GitHub Actions

## Overview

This document outlines the steps needed to run E2E tests against a Docker container in CI/CD. This approach ensures tests run against the actual deployment artifact.

## Current State

- **Unit tests (Core + API)**: 545 tests, all passing - run with `dotnet test`
- **E2E tests**: 58 tests using Playwright
  - 27 pass (API/SignalR tests that don't need frontend)
  - 31 fail (UI tests that require the React frontend in `wwwroot`)

The E2E tests currently use `WebApplicationFactory<Program>` which spins up an in-process test server. This server doesn't have the React frontend built into `wwwroot`, causing UI-based tests to fail.

## Solution: Docker-Based E2E Testing

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│  GitHub Actions Runner                                  │
│                                                         │
│  1. Build Docker image (includes React + .NET)          │
│  2. Start container on port 5000                        │
│  3. Run E2E tests against http://localhost:5000         │
│  4. Stop container                                      │
│  5. If tests pass → push image to registry              │
└─────────────────────────────────────────────────────────┘
```

### Changes Required

#### 1. Create `docker-compose.test.yml`

```yaml
version: '3.8'

services:
  webpscanner-test:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Testing
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/webpscanner-test.db
      - Email__Enabled=false
    healthcheck:
      test: ["CMD", "wget", "--no-verbose", "--tries=1", "--spider", "http://localhost:5000/api/health"]
      interval: 5s
      timeout: 5s
      retries: 10
      start_period: 10s
```

#### 2. Update `WebApplicationFixture.cs`

Add support for connecting to an external URL instead of spinning up an in-process server:

```csharp
public class WebApplicationFixture : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program>? _factory;
    private readonly HttpClient _client;
    private readonly string _baseUrl;

    public WebApplicationFixture()
    {
        // Check if we should connect to external Docker container
        var externalUrl = Environment.GetEnvironmentVariable("E2E_TEST_BASE_URL");

        if (!string.IsNullOrEmpty(externalUrl))
        {
            // Connect to external Docker container
            _baseUrl = externalUrl.TrimEnd('/');
            _client = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            _factory = null;
        }
        else
        {
            // Fall back to in-process server (for local dev without frontend)
            _factory = new TestWebApplicationFactory();
            _client = _factory.CreateClient();
            _baseUrl = _client.BaseAddress!.ToString().TrimEnd('/');
        }
    }

    public HttpClient CreateClient() => _client;
    public string BaseUrl => _baseUrl;

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }
    }
}

// Separate class for in-process testing (keeps current DB replacement logic)
internal class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // ... existing DB replacement code ...
        });
    }
}
```

#### 3. Create GitHub Actions Workflow

Create `.github/workflows/ci.yml`:

```yaml
name: CI

on:
  push:
    branches: [main, development]
  pull_request:
    branches: [main]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Run Core Tests
        run: dotnet test tests/WebPScanner.Core.Tests --no-build --verbosity normal

      - name: Run API Tests
        run: dotnet test tests/WebPScanner.Api.Tests --no-build --verbosity normal

  e2e-tests:
    runs-on: ubuntu-latest
    needs: unit-tests
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Build Docker image
        run: docker compose -f docker-compose.test.yml build

      - name: Start container
        run: |
          docker compose -f docker-compose.test.yml up -d
          # Wait for health check to pass
          timeout 60 bash -c 'until docker compose -f docker-compose.test.yml ps | grep -q "healthy"; do sleep 2; done'

      - name: Install Playwright browsers
        run: |
          cd tests/WebPScanner.E2E.Tests
          dotnet build
          pwsh bin/Debug/net9.0/playwright.ps1 install chromium

      - name: Run E2E Tests
        env:
          E2E_TEST_BASE_URL: http://localhost:5000
        run: dotnet test tests/WebPScanner.E2E.Tests --verbosity normal

      - name: Stop container
        if: always()
        run: docker compose -f docker-compose.test.yml down

  deploy:
    runs-on: ubuntu-latest
    needs: [unit-tests, e2e-tests]
    if: github.ref == 'refs/heads/main'
    steps:
      - uses: actions/checkout@v4

      # Add deployment steps here
      # e.g., push to container registry, deploy to server
```

#### 4. Update Test Classes (Optional)

Some tests may need minor adjustments for timing when running against a real container vs in-process server. Consider adding:

```csharp
// In test base class or fixture
protected bool IsDockerMode =>
    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("E2E_TEST_BASE_URL"));

// Use longer timeouts for Docker mode if needed
protected int DefaultTimeout => IsDockerMode ? 30000 : 5000;
```

### Running Locally

#### Option A: Test against Docker (recommended)
```bash
# Build and start container
docker compose -f docker-compose.test.yml up -d --build

# Wait for healthy
docker compose -f docker-compose.test.yml ps

# Run E2E tests
E2E_TEST_BASE_URL=http://localhost:5000 dotnet test tests/WebPScanner.E2E.Tests

# Stop container
docker compose -f docker-compose.test.yml down
```

#### Option B: Test without frontend (API/SignalR tests only)
```bash
# Runs in-process, UI tests will fail but API tests pass
dotnet test tests/WebPScanner.E2E.Tests
```

### Test Categories (Future Enhancement)

Consider adding test categories to run subsets:

```csharp
[TestFixture]
[Category("UI")]  // Requires frontend
public class LandingPageTests { }

[TestFixture]
[Category("API")]  // Works without frontend
public class ScanFlowTests { }
```

Then in CI:
```bash
# Run only API tests (no Docker needed)
dotnet test --filter Category=API

# Run all tests (requires Docker)
dotnet test
```

## Summary

| Step | File | Description |
|------|------|-------------|
| 1 | `docker-compose.test.yml` | Test-specific Docker Compose config |
| 2 | `WebApplicationFixture.cs` | Support `E2E_TEST_BASE_URL` env var |
| 3 | `.github/workflows/ci.yml` | GitHub Actions workflow |
| 4 | Test classes (optional) | Add categories, adjust timeouts |

## Dependencies

- Docker and Docker Compose on CI runner
- Playwright browsers installed in CI
- .NET 9 SDK
