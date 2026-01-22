# WebP Image Scanner

A free, open-source web application that scans websites to identify images not served in WebP format. Users provide a URL and email address, and the system crawls all accessible pages, analyzes served image content-types via browser DevTools Protocol, and delivers an actionable PDF report with optimization recommendations.

## Features

- **Automated Website Crawling**: Scans all accessible pages on a website using Puppeteer with full JavaScript support
- **Image Analysis**: Detects non-WebP images (JPEG, PNG, GIF, BMP, TIFF) via Chrome DevTools Protocol
- **Savings Estimation**: Calculates potential bandwidth savings from converting to WebP format
- **WebP Conversion**: Optionally convert discovered images to WebP format and download as a zip file
- **PDF Reports**: Generates professional PDF reports with executive summary, detailed findings, and recommendations
- **Email Delivery**: Sends reports directly to your email via SendGrid
- **Real-time Progress**: Live updates via SignalR showing scan progress, queue position, and discovered images
- **Fair Queue System**: Priority-based queue with fairness algorithm preventing abuse
- **Security First**: SSRF protection, rate limiting, input sanitization, and sandboxed browser execution

## Tech Stack

| Component | Technology |
|-----------|------------|
| Frontend | React 19, TypeScript, Tailwind CSS, Framer Motion, shadcn/ui |
| Backend | .NET 10, ASP.NET Core, FastEndpoints, SignalR |
| Database | SQLite with EF Core |
| Browser Automation | PuppeteerSharp (Chromium) |
| PDF Generation | QuestPDF |
| Email | SendGrid |
| Containerization | Docker |

## Quick Start

### Using Docker (Recommended)

1. Clone the repository:
   ```bash
   git clone https://github.com/csmashe/App-WebP-Image-Scanner.git
   cd App-WebP-Image-Scanner
   ```

2. Copy the environment file and configure:
   ```bash
   cp .env.example .env
   # Edit .env to enable email delivery:
   # - Set SENDGRID_API_KEY to your SendGrid API key
   # - Set EMAIL_ENABLED=true (email is disabled by default)
   ```

3. Start the application:
   ```bash
   docker-compose up -d
   ```

4. Open your browser to `http://localhost:5000`

### Local Development

See [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md) for detailed local development setup instructions.

## Configuration

All configuration options can be set via environment variables or `appsettings.json`. See [docs/CONFIGURATION.md](docs/CONFIGURATION.md) for a complete reference.

### Key Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `EMAIL_ENABLED` | Enable email delivery | `false` |
| `SENDGRID_API_KEY` | SendGrid API key (required if email enabled) | - |
| `FROM_EMAIL` | Email address for sending reports | `noreply@example.com` |
| `MAX_PAGES_PER_SCAN` | Maximum pages to scan per website | `2000` |
| `MAX_CONCURRENT_SCANS` | Number of simultaneous scans | `2` |
| `SENTRY_DSN` | Sentry DSN for error tracking (optional) | - |
| `VITE_GA_MEASUREMENT_ID` | Google Analytics measurement ID (optional) | - |

## How It Works

1. **Submit**: Enter a website URL and your email address
2. **Queue**: Your scan joins a fair FIFO queue with priority aging
3. **Scan**: Puppeteer crawls the website, respecting robots.txt
4. **Analyze**: Chrome DevTools Protocol captures all image requests
5. **Report**: A PDF report is generated with savings estimates
6. **Deliver**: Report is emailed to you directly

## Security Features

- **SSRF Prevention**: Blocks requests to private IPs, localhost, and internal networks
- **Rate Limiting**: IP-based limits on requests per minute/hour
- **Input Sanitization**: Validates and sanitizes all user inputs
- **robots.txt Compliance**: Respects website crawling preferences
- **Sandboxed Execution**: Browser runs in isolated environment
- **No Data Retention**: Scan data is deleted after email delivery

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/scan` | POST | Submit a new scan request |
| `/api/scan/{scanId}/status` | GET | Get scan status and progress |
| `/api/scan/{scanId}/report` | GET | Download PDF report (completed scans only) |
| `/api/scan/{scanId}/images` | GET | Download converted WebP images zip |
| `/api/images/{downloadId}` | GET | Download converted images by download ID |
| `/api/scan/stats` | GET | Get aggregate statistics |
| `/api/config` | GET | Get app configuration (email enabled, etc.) |
| `/api/health` | GET | Health check with queue statistics |

### SignalR Hub

Connect to `/hubs/scanprogress` for real-time updates:
- `QueuePositionUpdate` - Queue position changes
- `ScanStarted` - Scan begins processing
- `PageProgress` - Page scan progress
- `ImageFound` - Non-WebP image discovered
- `ScanComplete` - Scan finished successfully
- `ScanFailed` - Scan encountered an error
- `StatsUpdate` - Aggregate statistics updated

## Documentation

- [Deployment Guide](docs/DEPLOYMENT.md) - Self-hosting and Docker instructions
- [Configuration Reference](docs/CONFIGURATION.md) - All configuration options
- [Troubleshooting](docs/TROUBLESHOOTING.md) - Common issues and solutions
- [Contributing](docs/CONTRIBUTING.md) - How to contribute

## License

This project is open source and available under the [GNU Affero General Public License v3.0 (AGPL-3.0)](LICENSE).

## Contributing

Contributions are welcome! Please read our [Contributing Guidelines](docs/CONTRIBUTING.md) before submitting a pull request.

## Support

- [GitHub Issues](https://github.com/csmashe/App-WebP-Image-Scanner/issues) - Report bugs or request features
- [Discussions](https://github.com/csmashe/App-WebP-Image-Scanner/discussions) - Ask questions and share ideas

## Acknowledgments

- [PuppeteerSharp](https://github.com/hardkoded/puppeteer-sharp) - Headless Chrome .NET API
- [QuestPDF](https://github.com/QuestPDF/QuestPDF) - PDF generation library
- [shadcn/ui](https://ui.shadcn.com/) - UI components
- [SendGrid](https://sendgrid.com/) - Email delivery service
