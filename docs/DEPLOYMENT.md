# Deployment Guide

This guide covers deploying WebP Scanner using Docker (recommended) and local development setup.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Docker Deployment](#docker-deployment)
- [Local Development](#local-development)
- [SendGrid Setup](#sendgrid-setup)
- [Production Considerations](#production-considerations)
- [Reverse Proxy Configuration](#reverse-proxy-configuration)

## Prerequisites

### For Docker Deployment
- Docker 20.10+
- Docker Compose 2.0+
- 2GB RAM minimum (4GB recommended)
- SendGrid account (free tier available)

### For Local Development
- .NET 10 SDK
- Node.js 22+
- Chromium browser (or let PuppeteerSharp download it)
- SendGrid account (optional for development)

## Docker Deployment

### Quick Start

1. **Clone the repository**:
   ```bash
   git clone https://github.com/csmashe/App-WebP-Image-Scanner.git
   cd App-WebP-Image-Scanner
   ```

2. **Configure environment variables**:
   ```bash
   cp .env.example .env
   ```

   Edit `.env` and set at minimum:
   ```bash
   SENDGRID_API_KEY=your-actual-api-key
   FROM_EMAIL=your-verified-sender@yourdomain.com
   ```

3. **Build and start**:
   ```bash
   docker-compose up -d
   ```

4. **Verify the application is running**:
   ```bash
   docker-compose ps
   docker-compose logs -f webpscanner
   ```

5. **Access the application** at `http://localhost:5000`

### Updating

To update to a new version:

```bash
git pull
docker-compose build
docker-compose up -d
```

### Stopping and Removing

```bash
# Stop the application
docker-compose down

# Stop and remove volumes (WARNING: deletes all data)
docker-compose down -v
```

### Container Management

```bash
# View logs
docker-compose logs -f webpscanner

# Restart the application
docker-compose restart webpscanner

# Check health status
docker inspect --format='{{json .State.Health}}' webpscanner

# Access container shell
docker exec -it webpscanner /bin/bash
```

## Local Development

### Backend Setup

1. **Install .NET 10 SDK** from [dotnet.microsoft.com](https://dotnet.microsoft.com/download)

2. **Restore dependencies**:
   ```bash
   cd src/WebPScanner.Api
   dotnet restore
   ```

3. **Configure settings** (optional - defaults work for development):
   ```bash
   # Copy and edit appsettings for development
   cp appsettings.json appsettings.Development.json
   ```

4. **Run the API**:
   ```bash
   dotnet run
   ```

   The API will start at `http://localhost:5000`

### Frontend Setup

1. **Install Node.js 22+** from [nodejs.org](https://nodejs.org)

2. **Install dependencies**:
   ```bash
   cd src/WebPScanner.Web
   npm install
   ```

3. **Start development server**:
   ```bash
   npm run dev
   ```

   The frontend will start at `http://localhost:5173` with hot reload.

   > Note: The Vite dev server proxies API requests to `http://localhost:5000`

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Run specific test project
dotnet test tests/WebPScanner.Core.Tests
```

## SendGrid Setup

WebP Scanner uses SendGrid to deliver PDF reports via email.

### Create a SendGrid Account

1. Sign up at [sendgrid.com](https://sendgrid.com) (free tier: 100 emails/day)

2. Complete email verification

### Create an API Key

1. Navigate to **Settings** > **API Keys**
2. Click **Create API Key**
3. Choose **Restricted Access**
4. Enable **Mail Send** > **Full Access**
5. Click **Create & View**
6. Copy the API key (you won't see it again!)

### Verify a Sender Identity

1. Navigate to **Settings** > **Sender Authentication**
2. Choose either:
   - **Domain Authentication** (recommended for production)
   - **Single Sender Verification** (quick for testing)
3. Complete the verification process

### Configure WebP Scanner

Set the following environment variables:

```bash
SENDGRID_API_KEY=SG.your-api-key-here
FROM_EMAIL=verified-sender@yourdomain.com
FROM_NAME=WebP Scanner
EMAIL_ENABLED=true
```

### Testing Email Delivery

You can test email delivery without sending real emails:

1. Set `EMAIL_ENABLED=false` in your configuration
2. Check the application logs for email content
3. Verify the PDF attachment is generated correctly

## Production Considerations

### Resource Requirements

| Metric | Minimum | Recommended |
|--------|---------|-------------|
| Memory | 1GB | 2-4GB |
| CPU | 1 core | 2+ cores |
| Disk | 1GB | 5GB |

Memory usage scales with concurrent scans. Each Chromium instance uses ~200-500MB.

### Security Checklist

- [ ] Use HTTPS (set `ENFORCE_HTTPS=true` behind a reverse proxy)
- [ ] Set strong rate limits for your traffic expectations
- [ ] Configure firewall rules to limit access if needed
- [ ] Review and customize Content Security Policy headers
- [ ] Use a dedicated SendGrid sender domain
- [ ] Monitor application logs for suspicious activity

### Database Backup

The SQLite database is stored in the `webpscanner-data` Docker volume:

```bash
# Backup
docker cp webpscanner:/app/data/webpscanner.db ./backup.db

# Restore
docker cp ./backup.db webpscanner:/app/data/webpscanner.db
docker-compose restart webpscanner
```

### Log Management

Logs are stored in the `webpscanner-logs` Docker volume. Configure log rotation in `docker-compose.yml`:

```yaml
logging:
  driver: "json-file"
  options:
    max-size: "10m"
    max-file: "3"
```

## Reverse Proxy Configuration

### Nginx

```nginx
server {
    listen 80;
    server_name scanner.yourdomain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name scanner.yourdomain.com;

    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;

        # WebSocket support for SignalR
        proxy_read_timeout 86400;
    }
}
```

### Caddy

```caddyfile
scanner.yourdomain.com {
    reverse_proxy localhost:5000
}
```

### Traefik (Docker Labels)

```yaml
services:
  webpscanner:
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.webpscanner.rule=Host(`scanner.yourdomain.com`)"
      - "traefik.http.routers.webpscanner.entrypoints=websecure"
      - "traefik.http.routers.webpscanner.tls.certresolver=letsencrypt"
      - "traefik.http.services.webpscanner.loadbalancer.server.port=5000"
```

### Important: SignalR Configuration

When using a reverse proxy, ensure WebSocket connections are properly forwarded:

1. Enable WebSocket support in your proxy
2. Set appropriate timeouts (SignalR uses long-polling as fallback)
3. Forward the necessary headers (`Upgrade`, `Connection`)

## Health Checks

The application exposes a health endpoint at `/api/health`:

```bash
curl http://localhost:5000/api/health
```

Response:
```json
{
  "status": "Healthy",
  "queuedJobs": 3,
  "processingJobs": 1,
  "timestamp": "2025-01-20T12:00:00Z"
}
```

Use this endpoint for:
- Load balancer health checks
- Container orchestration probes
- Monitoring systems

## Scaling Considerations

WebP Scanner is designed for single-instance deployment. For higher throughput:

1. **Increase concurrent scans**: Set `MAX_CONCURRENT_SCANS` higher (requires more memory)
2. **Deploy multiple instances**: Each instance needs its own database
3. **Use a task queue**: Consider migrating to Redis-based queue for distributed processing

> Note: Multi-instance deployment requires additional architecture changes not covered in this guide.
