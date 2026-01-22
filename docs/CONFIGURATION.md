# Configuration Reference

WebP Scanner can be configured via environment variables or the `appsettings.json` file. Environment variables take precedence.

## Table of Contents

- [Environment Variables](#environment-variables)
- [appsettings.json Reference](#appsettingsjson-reference)
- [Configuration Sections](#configuration-sections)
  - [Email Settings](#email-settings)
  - [Crawler Settings](#crawler-settings)
  - [Queue Settings](#queue-settings)
  - [Security Settings](#security-settings)
- [Docker Environment Variables](#docker-environment-variables)

## Environment Variables

### Email Configuration

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `SENDGRID_API_KEY` | SendGrid API key for sending emails | - | Yes (for email delivery) |
| `FROM_EMAIL` | Sender email address (must be verified in SendGrid) | `noreply@example.com` | Recommended |
| `FROM_NAME` | Sender display name | `WebP Scanner` | No |
| `EMAIL_ENABLED` | Enable/disable email sending | `false` | No |

### Crawler Configuration

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `MAX_PAGES_PER_SCAN` | Maximum pages to crawl per website | `1000` (code) / `2000` (Docker) | No |
| `PAGE_TIMEOUT_SECONDS` | Timeout for loading a single page | `30` | No |

### Queue Configuration

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `MAX_CONCURRENT_SCANS` | Number of simultaneous scans | `2` | No |
| `MAX_QUEUE_SIZE` | Maximum jobs in the queue | `100` | No |
| `MAX_QUEUED_JOBS_PER_IP` | Maximum queued jobs per IP address | `20` | No |

### Security Configuration

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `MAX_REQUESTS_PER_MINUTE` | Rate limit: requests per minute per IP | `100` | No |
| `MAX_REQUESTS_PER_HOUR` | Rate limit: requests per hour per IP | `500` | No |
| `ENFORCE_HTTPS` | Redirect HTTP to HTTPS | `false` | No |

## appsettings.json Reference

The complete configuration file with all options:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=webpscanner.db"
  },
  "Crawler": {
    "MaxPagesPerScan": 1000,
    "PageTimeoutSeconds": 30,
    "NetworkIdleTimeoutMs": 2000,
    "DelayBetweenPagesMs": 500,
    "MaxRetries": 3,
    "RespectRobotsTxt": true,
    "EnableSandbox": true,
    "RestrictToTargetDomain": true,
    "MaxRequestSizeBytes": 52428800,
    "MaxRequestsPerPage": 500,
    "BlockTrackingDomains": true,
    "AllowedExternalDomains": []
  },
  "Queue": {
    "MaxConcurrentScans": 2,
    "MaxQueueSize": 100,
    "MaxQueuedJobsPerIp": 20,
    "FairnessSlotTicks": 36000000000,
    "PriorityAgingBoostSeconds": 30,
    "CooldownAfterScanSeconds": 0
  },
  "Email": {
    "FromEmail": "noreply@example.com",
    "FromName": "WebP Scanner",
    "MaxRetries": 3,
    "RetryDelayMinutes": 5
  },
  "Security": {
    "MaxRequestsPerMinute": 100,
    "EnforceHttps": true,
    "MaxScanDurationMinutes": 10,
    "MaxMemoryPerScanMb": 512,
    "RateLimitExemptIps": [],
    "EnableRequestSizeLimit": true,
    "MaxRequestBodySizeBytes": 102400
  }
}
```

## Configuration Sections

### Email Settings

Controls email delivery via SendGrid.

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `ApiKey` | string | - | SendGrid API key (prefer `SENDGRID_API_KEY` env var) |
| `FromEmail` | string | `noreply@example.com` | Sender email address |
| `FromName` | string | `WebP Scanner` | Sender display name |
| `Enabled` | bool | `false` | Enable/disable email sending |
| `MaxRetries` | int | `3` | Number of retry attempts for failed sends |
| `RetryDelayMinutes` | int | `5` | Delay between retry attempts |
| `MaxAttachmentSizeMb` | int | `10` | Maximum PDF attachment size |

**Example:**
```json
"Email": {
  "FromEmail": "reports@mycompany.com",
  "FromName": "WebP Image Scanner",
  "MaxRetries": 3,
  "RetryDelayMinutes": 5
}
```

### Crawler Settings

Controls the Puppeteer-based website crawler.

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `MaxPagesPerScan` | int | `1000` | Maximum pages to crawl per website |
| `PageTimeoutSeconds` | int | `30` | Timeout for loading a single page |
| `NetworkIdleTimeoutMs` | int | `2000` | Wait for network idle (SPA support) |
| `DelayBetweenPagesMs` | int | `500` | Delay between page requests (politeness) |
| `MaxRetries` | int | `3` | Retry attempts for failed page loads |
| `RespectRobotsTxt` | bool | `true` | Honor robots.txt directives |
| `ChromiumPath` | string | - | Path to Chromium executable (auto-detected) |
| `UserAgent` | string | - | Custom User-Agent string |
| `EnableSandbox` | bool | `true` | Enable Chromium sandbox (see security note below) |
| `RestrictToTargetDomain` | bool | `true` | Only crawl target domain |
| `MaxRequestSizeBytes` | int | `52428800` | Max size per network request (50MB) |
| `MaxRequestsPerPage` | int | `500` | Max requests per page load |
| `BlockTrackingDomains` | bool | `true` | Block analytics/tracking domains |
| `AllowedExternalDomains` | array | `[]` | Additional allowed domains (CDNs) |

**Chromium Sandbox Security Note:**

The `EnableSandbox` setting controls Chromium's internal sandbox. The default is `true` for maximum security in non-containerized environments.

| Environment | EnableSandbox | Reason |
|-------------|---------------|--------|
| Local development | `true` | Use Chromium's sandbox for browser isolation |
| Docker (production) | `false` | Docker provides container-level isolation; Chromium sandbox requires `SYS_ADMIN` capability or unprivileged user namespaces |
| Docker (with sandbox) | `true` | Requires adding `SYS_ADMIN` to `cap_add` in docker-compose.yml |

**WARNING:** If you enable the sandbox in Docker without proper capabilities, Chromium will fail to start. The `docker-compose.yml` explicitly sets `Crawler__EnableSandbox=false` for this reason.

To enable sandbox in Docker (advanced):
```yaml
cap_add:
  - SYS_ADMIN  # Required for Chromium sandbox
environment:
  - Crawler__EnableSandbox=true
```

**Example for large websites:**
```json
"Crawler": {
  "MaxPagesPerScan": 500,
  "PageTimeoutSeconds": 60,
  "NetworkIdleTimeoutMs": 5000,
  "DelayBetweenPagesMs": 1000
}
```

**Allowed External Domains Example:**
```json
"Crawler": {
  "AllowedExternalDomains": [
    "cdn.example.com",
    "images.example.com"
  ]
}
```

### Queue Settings

Controls the scan job queue and fairness algorithm.

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `MaxConcurrentScans` | int | `2` | Simultaneous scans (affects memory) |
| `MaxQueueSize` | int | `100` | Maximum jobs in queue |
| `MaxQueuedJobsPerIp` | int | `20` | Max pending jobs per IP address |
| `FairnessSlotTicks` | long | `36000000000` | Slot multiplier for fair-share ordering (1 hour in ticks) |
| `PriorityAgingBoostSeconds` | int | `30` | Boost interval for waiting jobs |
| `CooldownAfterScanSeconds` | int | `0` | Cooldown after scan completion (0 = disabled) |

**Fairness Algorithm:**
- Jobs are ordered by submission count (1st job, 2nd job, etc.), then by creation time within the same slot
- `FairnessSlotTicks` controls interleaving strength (higher = stronger interleaving)
- Jobs waiting longer than `PriorityAgingBoostSeconds` get priority boost
- After scan completion, IP must wait `CooldownAfterScanSeconds` (if > 0)

**Example for high-traffic deployment:**
```json
"Queue": {
  "MaxConcurrentScans": 4,
  "MaxQueueSize": 200,
  "MaxQueuedJobsPerIp": 2,
  "CooldownAfterScanSeconds": 600
}
```

### Security Settings

Controls rate limiting and security hardening.

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `MaxRequestsPerMinute` | int | `100` | Rate limit per IP per minute |
| `EnforceHttps` | bool | `true` | Redirect HTTP to HTTPS |
| `MaxScanDurationMinutes` | int | `10` | Maximum scan duration |
| `MaxMemoryPerScanMb` | int | `512` | Memory limit per scan |
| `RateLimitExemptIps` | array | `[]` | IPs exempt from rate limiting |
| `EnableRequestSizeLimit` | bool | `true` | Enforce request body size limits |
| `MaxRequestBodySizeBytes` | int | `102400` | Max request body size (100KB) |

**Rate Limit Exempt IPs (CIDR supported):**
```json
"Security": {
  "RateLimitExemptIps": [
    "10.0.0.0/8",
    "192.168.1.100"
  ]
}
```

**Production settings:**
```json
"Security": {
  "MaxRequestsPerMinute": 100,
  "EnforceHttps": true,
  "MaxScanDurationMinutes": 15
}
```

## Docker Environment Variables

When using Docker, override settings using environment variables in `docker-compose.yml` or `.env`:

```yaml
environment:
  # Email
  - SENDGRID_API_KEY=${SENDGRID_API_KEY}
  - Email__FromEmail=${FROM_EMAIL:-noreply@example.com}
  - Email__FromName=${FROM_NAME:-WebP Scanner}
  - Email__Enabled=${EMAIL_ENABLED:-false}

  # Crawler
  - Crawler__ChromiumPath=/usr/bin/chromium
  - Crawler__EnableSandbox=false
  - Crawler__MaxPagesPerScan=${MAX_PAGES_PER_SCAN:-2000}
  - Crawler__PageTimeoutSeconds=${PAGE_TIMEOUT_SECONDS:-30}

  # Queue
  - Queue__MaxConcurrentScans=${MAX_CONCURRENT_SCANS:-2}
  - Queue__MaxQueueSize=${MAX_QUEUE_SIZE:-100}
  - Queue__MaxQueuedJobsPerIp=${MAX_QUEUED_JOBS_PER_IP:-20}

  # Security
  - Security__MaxRequestsPerMinute=${MAX_REQUESTS_PER_MINUTE:-100}
  - Security__EnforceHttps=${ENFORCE_HTTPS:-false}
```

### Nested Configuration Pattern

For nested settings, use double underscores (`__`) as separators:

```bash
# appsettings.json:
# "Crawler": { "MaxPagesPerScan": 100 }

# Environment variable:
Crawler__MaxPagesPerScan=200
```

### Boolean Values

For boolean environment variables, use `true` or `false`:

```bash
EMAIL_ENABLED=true
ENFORCE_HTTPS=false
```
