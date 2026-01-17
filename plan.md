# Project Plan

## Overview
WebP Image Scanner - A free, open-source web application that scans websites to identify images not served in WebP format. Users provide a URL and email address, and the system crawls all accessible pages, analyzes served image content-types via browser DevTools Protocol, and delivers an actionable PDF report with optimization recommendations.

**Reference:** `PRD.md`

---

## Task List

```json
[
  {
    "category": "setup",
    "description": "Initialize project structure and core dependencies",
    "steps": [
      "Create solution structure with WebPScanner.Api, WebPScanner.Core, WebPScanner.Data, WebPScanner.Web projects",
      "Initialize .NET 9 backend projects with required NuGet packages (EF Core, SignalR, PuppeteerSharp)",
      "Initialize React 18+ frontend with TypeScript, Tailwind CSS, and Vite",
      "Configure SQLite database with EF Core migrations",
      "Set up Docker and docker-compose configuration",
      "Create basic appsettings.json with configurable crawler/queue/email settings",
      "Verify solution builds and all projects reference correctly"
    ],
    "passes": false
  },
  {
    "category": "feature",
    "description": "Implement data model and repository layer",
    "steps": [
      "Create ScanJob entity with all required properties (ScanId, TargetUrl, Email, Status, QueuePosition, etc.)",
      "Create DiscoveredImage entity with relationship to ScanJob",
      "Create ScanStatus enum (Queued, Processing, Completed, Failed)",
      "Implement WebPScannerDbContext with entity configurations",
      "Create IScanJobRepository and IDiscoveredImageRepository interfaces",
      "Implement repository classes with CRUD operations",
      "Generate and apply initial EF Core migration",
      "Verify database creates correctly with proper schema"
    ],
    "passes": false
  },
  {
    "category": "feature",
    "description": "Implement URL and email validation service",
    "steps": [
      "Create IValidationService interface",
      "Implement URL validation (proper format, http/https only, no localhost/private IPs)",
      "Implement SSRF prevention (block 10.x, 172.16-31.x, 192.168.x, link-local addresses)",
      "Implement email format validation",
      "Create validation DTOs with Zod-like schema validation",
      "Add server-side re-validation before queue insertion",
      "Write unit tests for all validation scenarios"
    ],
    "passes": false
  },
  {
    "category": "feature",
    "description": "Implement basic scan submission API endpoint",
    "steps": [
      "Create ScanController with POST /api/scan endpoint",
      "Implement request validation using validation service",
      "Create ScanRequestDto and ScanResponseDto",
      "Generate unique ScanId (GUID) for each submission",
      "Store scan job in database with Queued status",
      "Return scanId and queuePosition in response",
      "Add GET /api/scan/{scanId}/status endpoint",
      "Add GET /api/health endpoint with queue statistics",
      "Write integration tests for API endpoints"
    ],
    "passes": false
  },
  {
    "category": "feature",
    "description": "Implement simple FIFO queue service",
    "steps": [
      "Create IQueueService interface with Enqueue, Dequeue, GetPosition methods",
      "Implement basic FIFO queue using database ordering",
      "Create background service for queue processing (IHostedService)",
      "Implement queue position tracking and updates",
      "Add configurable MaxConcurrentScans setting",
      "Implement cooldown after scan completion per IP",
      "Write unit tests for queue operations"
    ],
    "passes": false
  },
  {
    "category": "feature",
    "description": "Implement Puppeteer-based website crawler",
    "steps": [
      "Create ICrawlerService interface",
      "Configure PuppeteerSharp with Chromium download/path settings",
      "Implement page discovery algorithm (extract <a href> links, filter same-domain)",
      "Implement URL normalization (remove fragments, deduplicate, handle trailing slashes)",
      "Add robots.txt parsing and respect for disallow rules",
      "Implement networkidle0 waiting for SPA support",
      "Add retry mechanism with exponential backoff (max 3 retries)",
      "Implement configurable page timeout and network idle timeout",
      "Add authentication detection (login redirects, 401/403 responses)",
      "Create configurable delay between page requests",
      "Write integration tests with mock websites"
    ],
    "passes": false
  },
  {
    "category": "feature",
    "description": "Implement image detection via Chrome DevTools Protocol",
    "steps": [
      "Create IImageAnalyzerService interface",
      "Enable Network domain in Puppeteer CDP session",
      "Listen to Network.responseReceived events",
      "Filter responses where mimeType starts with 'image/'",
      "Capture: url, mimeType, encodedDataLength, response headers",
      "Identify non-WebP raster images (JPEG, PNG, GIF, BMP, TIFF)",
      "Record image dimensions from response headers or decoding",
      "Create DetectedImage model with all captured properties",
      "Store discovered images in database linked to ScanJob",
      "Write unit tests for image detection logic"
    ],
    "passes": false
  },
  {
    "category": "feature",
    "description": "Implement SignalR hub for real-time progress updates",
    "steps": [
      "Create ScanProgressHub with client subscription methods",
      "Implement SubscribeToScan(scanId) for scan-specific groups",
      "Create server-to-client methods: QueuePositionUpdate, ScanStarted, PageProgress, ImageFound, ScanComplete, ScanFailed",
      "Integrate SignalR hub with queue service for position updates",
      "Integrate SignalR hub with crawler service for progress broadcasting",
      "Handle disconnection/reconnection gracefully",
      "Configure SignalR in Program.cs with appropriate settings",
      "Write integration tests for SignalR messaging"
    ],
    "passes": false
  },
  {
    "category": "feature",
    "description": "Implement fairness algorithm for queue prioritization",
    "steps": [
      "Add IP-based tracking to scan submissions (SubmitterIp, SubmissionCount)",
      "Implement priority calculation: base_time + (count * fairness_penalty)",
      "Modify queue processing to use priority ordering",
      "Implement priority aging (boost for longer-waiting jobs)",
      "Add MaxQueuedJobsPerIp limit with rejection for excess",
      "Create configurable FairnessPenaltySeconds and PriorityAgingBoostSeconds",
      "Update SignalR to broadcast updated queue positions after reordering",
      "Write unit tests for fairness algorithm scenarios"
    ],
    "passes": false
  },
  {
    "category": "feature",
    "description": "Implement WebP savings estimation algorithm",
    "steps": [
      "Create ISavingsEstimatorService interface",
      "Implement empirical conversion ratios: PNG→WebP ~26%, JPEG→WebP ~75%, GIF→WebP ~50%",
      "Calculate EstimatedWebPSize for each detected image",
      "Calculate PotentialSavings percentage per image",
      "Aggregate total savings statistics for scan summary",
      "Add disclaimer that estimates are approximate",
      "Write unit tests for savings calculations"
    ],
    "passes": false
  },
  {
    "category": "feature",
    "description": "Implement PDF report generation with QuestPDF",
    "steps": [
      "Install and configure QuestPDF library",
      "Create IPdfReportService interface",
      "Design cover page template (logo, title, URL, scan date)",
      "Design executive summary page (statistics cards, pie chart)",
      "Design detailed findings table (sorted by potential savings)",
      "Design recommendations page with WebP benefits and conversion guidance",
      "Add footer with tool branding and link",
      "Implement table pagination for large result sets",
      "Ensure PDF size < 5MB for typical scans",
      "Write unit tests for PDF generation"
    ],
    "passes": false
  },
  {
    "category": "feature",
    "description": "Implement email delivery with SendGrid",
    "steps": [
      "Install SendGrid .NET SDK",
      "Create IEmailService interface",
      "Configure SendGrid via environment variables (SENDGRID_API_KEY, FROM_EMAIL, FROM_NAME)",
      "Create email template with scan summary in body",
      "Implement PDF attachment (< 10MB direct attach)",
      "Implement retry logic (3 attempts over 15 minutes)",
      "Add delivery confirmation logging",
      "Implement data cleanup after successful email delivery",
      "Handle failed delivery gracefully with error logging",
      "Write integration tests with SendGrid sandbox"
    ],
    "passes": false
  },
  {
    "category": "feature",
    "description": "Implement React frontend landing page",
    "steps": [
      "Set up Tailwind CSS with dark-first color palette",
      "Install and configure shadcn/ui component library",
      "Create responsive header with logo, GitHub link, theme toggle",
      "Create hero section with animated gradient orb background",
      "Implement URL input field with validation feedback",
      "Implement email input field with validation feedback",
      "Create submit button with disabled state until validation passes",
      "Add 3-step process visualization (Enter URL → We Scan → Get Report)",
      "Add 'Why WebP?' statistics section",
      "Create footer with open source branding",
      "Ensure mobile responsive design"
    ],
    "passes": false
  },
  {
    "category": "feature",
    "description": "Implement React SignalR client for real-time updates",
    "steps": [
      "Install @microsoft/signalr package",
      "Create useScanProgress custom hook",
      "Implement connection establishment after form submission",
      "Handle QueuePositionUpdate with visual queue indicator",
      "Handle ScanStarted state transition",
      "Handle PageProgress with progress bar and current URL display",
      "Handle ImageFound with running count display",
      "Handle ScanComplete with success message",
      "Handle ScanFailed with error display",
      "Implement graceful disconnection handling",
      "Create Zustand store for scan state management"
    ],
    "passes": false
  },
  {
    "category": "feature",
    "description": "Implement progress display components",
    "steps": [
      "Create QueuePositionDisplay component with visual queue indicator",
      "Create ScanProgressDisplay component with animated scanner icon",
      "Create progress bar component showing pages scanned vs discovered",
      "Create current URL display with truncation for long URLs",
      "Create non-WebP image counter with icon",
      "Add smooth transitions between queue and scanning states",
      "Implement 'Feel free to close this tab' messaging",
      "Add Framer Motion animations for state transitions"
    ],
    "passes": false
  },
  {
    "category": "feature",
    "description": "Implement dark/light mode toggle",
    "steps": [
      "Create ThemeProvider context with localStorage persistence",
      "Implement useTheme custom hook",
      "Create theme toggle button component",
      "Define light mode color palette variables",
      "Update all components to use CSS variables for theming",
      "Add system preference detection as default",
      "Ensure smooth transition animations between themes"
    ],
    "passes": false
  },
  {
    "category": "feature",
    "description": "Implement error handling and loading states",
    "steps": [
      "Create error boundary component for React",
      "Implement API error handling with user-friendly messages",
      "Create loading spinner component",
      "Add skeleton loaders for async content",
      "Implement form submission loading state",
      "Add toast notifications for errors and success",
      "Handle network disconnection gracefully",
      "Create 'scan failed' state with retry option"
    ],
    "passes": false
  },
  {
    "category": "security",
    "description": "Implement rate limiting and security hardening",
    "steps": [
      "Implement IP-based rate limiting middleware",
      "Add global rate limiting (max queue size of 100)",
      "Configure Puppeteer sandbox mode appropriately for Docker",
      "Limit network requests to target domain only",
      "Implement resource limits (memory, CPU, time) per scan",
      "Add input sanitization for all user inputs",
      "Configure HTTPS enforcement for production",
      "Ensure container runs as non-root user",
      "Audit all dependencies for known vulnerabilities"
    ],
    "passes": false
  },
  {
    "category": "setup",
    "description": "Optimize Docker configuration for production",
    "steps": [
      "Create multi-stage Dockerfile for smaller image size",
      "Install Chromium dependencies in Docker image",
      "Configure PUPPETEER_EXECUTABLE_PATH environment variable",
      "Set up docker-compose.yml with environment variables",
      "Configure memory and CPU limits in docker-compose",
      "Add health check configuration",
      "Create .dockerignore for efficient builds",
      "Test container startup and Puppeteer functionality"
    ],
    "passes": false
  },
  {
    "category": "feature",
    "description": "Implement Terms of Service page",
    "steps": [
      "Create ToS React component/page",
      "Document fair use policy (queue system, IP limits)",
      "Document limitations (public pages only, auth pages skipped)",
      "Document data handling (no retention, email-only delivery)",
      "Add disclaimer about scanning third-party sites",
      "Link to ToS from footer and submission form",
      "Style consistently with rest of application"
    ],
    "passes": false
  },
  {
    "category": "testing",
    "description": "Implement end-to-end testing suite",
    "steps": [
      "Set up Playwright or Cypress for E2E testing",
      "Create test for complete scan flow: submit → queue → progress → email",
      "Test queue position updates via SignalR",
      "Test error handling scenarios (invalid URL, rate limit)",
      "Test mobile responsive layouts",
      "Test dark/light mode toggle",
      "Test PDF report generation output",
      "Create mock SMTP server for email testing",
      "Verify all console errors are addressed"
    ],
    "passes": false
  },
  {
    "category": "testing",
    "description": "Write unit tests for all core services",
    "steps": [
      "Test CrawlerService with mock Puppeteer",
      "Test ImageAnalyzerService detection logic",
      "Test QueueService fairness algorithm",
      "Test SavingsEstimatorService calculations",
      "Test PdfReportService output",
      "Test EmailService retry logic",
      "Test ValidationService all edge cases",
      "Achieve minimum 80% code coverage"
    ],
    "passes": false
  },
  {
    "category": "documentation",
    "description": "Create deployment documentation",
    "steps": [
      "Write README.md with project overview and features",
      "Document environment variables required",
      "Create self-hosting guide with Docker instructions",
      "Document SendGrid setup process",
      "Add configuration reference from appsettings.json",
      "Create troubleshooting guide for common issues",
      "Add contributing guidelines",
      "Include license information (open source)"
    ],
    "passes": false
  },
  {
    "category": "testing",
    "description": "Final integration testing and verification",
    "steps": [
      "Run full E2E test suite in Docker environment",
      "Test with various website types (static, SPA, large sites)",
      "Verify robots.txt compliance",
      "Test rate limiting under load",
      "Verify email delivery in production-like environment",
      "Test PDF readability and formatting",
      "Perform accessibility audit (WCAG 2.1 AA)",
      "Verify all links in UI and documentation work"
    ],
    "passes": false
  }
]
```

---

## Session Startup Sequence

Following best practices for long-running agent sessions:

1. **Verify Environment**: Run `pwd` to confirm working directory is `/home/csmashe/RiderProjects/Prd`
2. **Read Activity Log**: Review `activity.md` for previous session work and decisions
3. **Check Git History**: Run `git log --oneline -10` to see recent commits
4. **Review Task List**: Check `plan.md` for next incomplete task (`"passes": false`)
5. **Read Requirements**: Reference `PRD.md` for detailed specifications if needed
6. **Run Baseline Tests**: Execute existing tests to ensure system is stable
7. **Update Activity Log**: Add session start entry to `activity.md`
8. **Begin Work**: Start on the highest-priority incomplete task

### Before Ending Session
1. Commit all changes with descriptive messages
2. Update task status in `plan.md` to `"passes": true` if completed
3. Append session summary to `activity.md`
4. Note any blockers or decisions for next session

---

## Development Notes

### Technology Stack Summary
| Component | Technology |
|-----------|------------|
| Frontend | React 18+, TypeScript, Tailwind CSS, Framer Motion, shadcn/ui |
| Backend | .NET 9, ASP.NET Core, SignalR |
| Database | SQLite with EF Core |
| Browser Automation | PuppeteerSharp |
| PDF Generation | QuestPDF |
| Email | SendGrid |
| Containerization | Docker |

### Key Configuration Files
- `appsettings.json` - Crawler, Queue, Email, Security settings
- `docker-compose.yml` - Container orchestration
- Environment variables for secrets (SENDGRID_API_KEY)

### Incremental Development Approach
- Complete one task fully before moving to next
- Commit changes with descriptive messages after each task
- Update task status to `"passes": true` after verification
- Run tests after each significant change

---

## Progress Tracking

Update this section as tasks are completed:

- [ ] Phase 1: Foundation (Core Infrastructure)
- [ ] Phase 2: Real-Time & Queue
- [ ] Phase 3: Reports & Email
- [ ] Phase 4: UI Polish
- [ ] Phase 5: Hardening & Deployment
