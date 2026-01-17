# Product Requirements Document (PRD)
# WebP Image Scanner

**Version:** 1.0
**Last Updated:** January 17, 2026
**Status:** Draft

---

## Table of Contents
1. [Overview](#1-overview)
2. [Target Audience](#2-target-audience)
3. [Core Features](#3-core-features)
4. [Technical Architecture](#4-technical-architecture)
5. [Data Model](#5-data-model)
6. [User Interface](#6-user-interface)
7. [Queue & Fairness System](#7-queue--fairness-system)
8. [Email & PDF Reports](#8-email--pdf-reports)
9. [Security Considerations](#9-security-considerations)
10. [Development Phases](#10-development-phases)
11. [Technical Challenges & Mitigations](#11-technical-challenges--mitigations)
12. [Future Expansion](#12-future-expansion)
13. [Terms of Service Considerations](#13-terms-of-service-considerations)

---

## 1. Overview

### 1.1 Product Summary
WebP Image Scanner is a free, open-source web application that scans websites to identify images not served in WebP format. Users provide a URL and email address, and the system crawls all accessible pages, analyzes served image content-types via browser DevTools Protocol, and delivers an actionable PDF report with optimization recommendations.

### 1.2 Problem Statement
WebP images offer 25-35% better compression than PNG/JPEG while maintaining quality, directly impacting page load times and Core Web Vitals scores. Many website owners are unaware which images on their sites could benefit from WebP conversion. Manual auditing is time-consuming and error-prone.

### 1.3 Solution
An automated scanning tool that:
- Crawls all pages of a website
- Intercepts actual served images (not just `<img>` src attributes)
- Identifies non-WebP images with actionable data
- Provides estimated file size savings
- Delivers results via email as a downloadable PDF report

### 1.4 Key Objectives
- **Accessibility**: Free to use, no authentication required
- **Self-hostable**: Open source with Docker deployment
- **Fair usage**: Queue system with fairness algorithm
- **Actionable insights**: Not just identification, but optimization recommendations

---

## 2. Target Audience

### 2.1 Primary Users
| User Type | Use Case | Technical Level |
|-----------|----------|-----------------|
| Website Owners | Audit personal/business sites for performance optimization | Beginner to Intermediate |
| Web Developers | Audit client sites, pre-launch performance checks | Intermediate to Advanced |
| Digital Agencies | Batch audits for multiple client sites | Intermediate to Advanced |
| SEO Professionals | Performance audits as part of SEO analysis | Intermediate |

### 2.2 User Needs
- Quick, no-signup access to the tool
- Clear progress indication during scans
- Professional PDF reports for client deliverables
- Ability to self-host for privacy or custom configuration

---

## 3. Core Features

### 3.1 URL & Email Submission
**Description**: Simple form accepting a website URL and email address for result delivery.

**Acceptance Criteria**:
- URL field validates proper URL format (must include protocol)
- URL field normalizes input (trims whitespace, handles trailing slashes)
- Email field validates proper email format
- Form displays clear error messages for invalid input
- Submit button disabled until both fields pass validation
- User sees confirmation with queue position after submission

**Technical Considerations**:
- Client-side validation with Zod or similar schema validation
- Server-side re-validation before queue insertion
- Sanitize URL input to prevent injection attacks

---

### 3.2 Website Crawling Engine
**Description**: Puppeteer-based crawler that discovers and visits all pages within a domain.

**Acceptance Criteria**:
- Crawler starts from submitted URL and discovers all internal links
- Only crawls pages within the same domain (no external links)
- Respects `robots.txt` disallow rules
- Skips pages requiring authentication (detects login redirects, 401/403 responses)
- Handles client-rendered SPAs (waits for network idle)
- Implements retry mechanism for transient failures (max 3 retries with exponential backoff)
- Logs progress for real-time updates

**Technical Considerations**:
```
Crawler Configuration (configurable via appsettings.json):
- MaxConcurrentPages: int (default: 3) — parallel page processing within a single site
- PageTimeout: int (default: 30000ms) — max time to wait for page load
- NetworkIdleTimeout: int (default: 2000ms) — time to wait after last network request
- MaxRetries: int (default: 3) — retry attempts per page
- RetryDelayBase: int (default: 1000ms) — base delay for exponential backoff
- UserAgent: string — custom user agent identifying the scanner
- RespectRobotsTxt: bool (default: true)
```

**Page Discovery Algorithm**:
1. Load initial URL
2. Wait for `networkidle0` or timeout
3. Extract all `<a href>` links
4. Filter to same-domain, same-protocol links
5. Normalize URLs (remove fragments, deduplicate)
6. Add undiscovered URLs to crawl queue
7. Repeat until queue exhausted or limits reached

---

### 3.3 Image Detection via DevTools Protocol
**Description**: Intercept network requests to identify actual served image content-types, not just markup.

**Acceptance Criteria**:
- Captures all image requests (img, picture, background-image, etc.)
- Records actual Content-Type header from response
- Identifies images served as: JPEG, PNG, GIF, BMP, TIFF, SVG, AVIF, WebP
- Flags all non-WebP raster images (excluding SVG which is vector)
- Records image dimensions and file size from response

**Technical Considerations**:
```
Using Puppeteer CDP (Chrome DevTools Protocol):
- Enable Network domain
- Listen to Network.responseReceived events
- Filter responses where mimeType starts with "image/"
- Capture: url, mimeType, encodedDataLength, response headers
- For dimensions: decode image or use response headers if available
```

**Image Data Captured**:
```typescript
interface DetectedImage {
  imageUrl: string;           // Full URL of the image
  pageUrl: string;            // Page where image was found
  mimeType: string;           // e.g., "image/png", "image/jpeg"
  fileSize: number;           // Bytes
  width?: number;             // Pixels (if determinable)
  height?: number;            // Pixels (if determinable)
  estimatedWebPSize?: number; // Calculated estimate
  potentialSavings?: number;  // Percentage
}
```

---

### 3.4 Real-Time Progress Updates
**Description**: Users who keep their browser open receive live updates on queue position and scan progress.

**Acceptance Criteria**:
- Connection established via SignalR immediately after form submission
- User sees current queue position (updates when position changes)
- Once scan starts, user sees:
  - "Scanning page X of Y discovered" (Y increases as pages are discovered)
  - Current page URL being scanned
  - Running count of non-WebP images found
- Connection gracefully handles disconnection/reconnection
- If user closes browser and reopens, they cannot reconnect to the same scan (email-only at that point)

**Technical Considerations**:
```
SignalR Hub: ScanProgressHub

Client -> Server Methods:
- SubscribeToScan(scanId: string) — join scan-specific group

Server -> Client Methods:
- QueuePositionUpdate(position: int, totalInQueue: int)
- ScanStarted()
- PageProgress(currentPage: int, totalDiscovered: int, currentUrl: string)
- ImageFound(imageCount: int)
- ScanComplete(resultSummary: ScanSummary)
- ScanFailed(errorMessage: string)
```

---

### 3.5 PDF Report Generation
**Description**: Generate a professional PDF report summarizing scan results with actionable recommendations.

**Acceptance Criteria**:
- PDF includes:
  - Header with scan date, target URL, and scanner branding
  - Executive summary (total pages scanned, total images, non-WebP count, potential savings)
  - Detailed table of all non-WebP images with: page URL, image URL, current format, file size, estimated savings
  - Recommendations section with general WebP conversion guidance
  - Footer with link to the tool
- PDF is well-formatted and professional (suitable for client deliverables)
- File size reasonable (< 5MB for typical scans)

**Technical Considerations**:
```
Recommended Library: QuestPDF (MIT License, .NET native)
- Fluent API for document composition
- No external dependencies (no wkhtmltopdf)
- Good performance for server-side generation

Alternative: PuppeteerSharp for HTML-to-PDF (heavier, but allows HTML/CSS templates)
```

**PDF Structure**:
```
1. Cover Page
   - "WebP Image Scan Report"
   - Target URL
   - Scan Date
   - Generated by WebP Image Scanner

2. Executive Summary
   - Total pages scanned
   - Total images detected
   - Non-WebP images found
   - Total current size of non-WebP images
   - Estimated total savings (bytes and percentage)

3. Detailed Findings (table)
   | Page | Image URL | Format | Size | Est. WebP Size | Savings |

4. Recommendations
   - Brief explanation of WebP benefits
   - Conversion tools/services suggestions
   - Implementation approaches (picture element, server-side, CDN)

5. Footer
   - "Generated by WebP Image Scanner"
   - Link to tool
```

---

### 3.6 Email Delivery
**Description**: Send PDF report to user's email upon scan completion.

**Acceptance Criteria**:
- Email sent within 1 minute of scan completion
- Email includes:
  - Clear subject line: "Your WebP Image Scan Results for [domain]"
  - Brief summary in email body
  - PDF attached (or link if too large)
  - Link to the tool for future scans
- Handles delivery failures gracefully (retry logic)
- Works with SendGrid free tier limits

**Technical Considerations**:
```
SendGrid Integration:
- Use SendGrid .NET SDK (MIT License)
- Configure via environment variables:
  - SENDGRID_API_KEY
  - SENDGRID_FROM_EMAIL
  - SENDGRID_FROM_NAME

Email Template:
- Use SendGrid dynamic templates for maintainability
- Or inline HTML template in application

Attachment Handling:
- PDF < 10MB: attach directly
- PDF > 10MB: unlikely, but could temporarily host and link
```

---

## 4. Technical Architecture

### 4.1 System Overview
```
┌─────────────────────────────────────────────────────────────────┐
│                        Docker Container                          │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────┐ │
│  │   React/TS      │    │    .NET 9       │    │   SQLite    │ │
│  │   Frontend      │◄──►│    Backend      │◄──►│   Database  │ │
│  │   (Static)      │    │                 │    │             │ │
│  └─────────────────┘    └────────┬────────┘    └─────────────┘ │
│                                  │                              │
│                         ┌────────▼────────┐                     │
│                         │   Puppeteer     │                     │
│                         │   (Chromium)    │                     │
│                         └─────────────────┘                     │
└─────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
                         ┌─────────────────┐
                         │    SendGrid     │
                         │    (External)   │
                         └─────────────────┘
```

### 4.2 Technology Stack

| Layer | Technology | Justification |
|-------|------------|---------------|
| Frontend | React 18+ with TypeScript | Modern, type-safe, excellent ecosystem |
| Styling | Tailwind CSS | Utility-first, matches reference site aesthetics |
| Animations | Framer Motion | Smooth, performant animations for modern feel |
| State Management | Zustand or React Query | Lightweight, sufficient for this scope |
| Backend | .NET 9 (ASP.NET Core) | Specified requirement, excellent performance |
| Real-time | SignalR | Native .NET integration, WebSocket with fallbacks |
| Browser Automation | PuppeteerSharp | .NET port of Puppeteer, CDP access |
| Database | SQLite | Embedded, zero-config, container-friendly |
| ORM | Entity Framework Core | .NET standard, SQLite provider available |
| PDF Generation | QuestPDF | MIT license, native .NET, no external deps |
| Email | SendGrid SDK | Specified requirement, reliable delivery |
| Containerization | Docker | Specified requirement, easy self-hosting |

### 4.3 Project Structure
```
/
├── src/
│   ├── WebPScanner.Web/              # React Frontend
│   │   ├── src/
│   │   │   ├── components/
│   │   │   │   ├── ui/               # Reusable UI components
│   │   │   │   ├── forms/            # Form components
│   │   │   │   └── progress/         # Progress display components
│   │   │   ├── hooks/                # Custom React hooks
│   │   │   ├── services/             # API and SignalR clients
│   │   │   ├── stores/               # Zustand stores
│   │   │   ├── types/                # TypeScript types
│   │   │   └── pages/                # Page components
│   │   └── package.json
│   │
│   ├── WebPScanner.Api/              # .NET Backend API
│   │   ├── Controllers/
│   │   ├── Hubs/                     # SignalR hubs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   ├── WebPScanner.Core/             # Business Logic
│   │   ├── Services/
│   │   │   ├── CrawlerService.cs
│   │   │   ├── ImageAnalyzerService.cs
│   │   │   ├── QueueService.cs
│   │   │   ├── PdfReportService.cs
│   │   │   └── EmailService.cs
│   │   ├── Models/
│   │   └── Interfaces/
│   │
│   └── WebPScanner.Data/             # Data Access
│       ├── DbContext/
│       ├── Entities/
│       └── Repositories/
│
├── tests/
│   ├── WebPScanner.Core.Tests/
│   └── WebPScanner.Api.Tests/
│
├── docker/
│   ├── Dockerfile
│   └── docker-compose.yml
│
└── README.md
```

### 4.4 API Endpoints

```
POST /api/scan
  Request:  { url: string, email: string }
  Response: { scanId: string, queuePosition: int }

GET /api/scan/{scanId}/status
  Response: { status: "queued" | "processing" | "completed" | "failed",
              queuePosition?: int,
              progress?: { pagesScanned: int, pagesDiscovered: int } }

GET /api/health
  Response: { status: "healthy", queueLength: int, activeScans: int }
```

---

## 5. Data Model

### 5.1 Entity Relationship Diagram
```
┌─────────────────────┐       ┌─────────────────────┐
│      ScanJob        │       │    DiscoveredImage  │
├─────────────────────┤       ├─────────────────────┤
│ Id (PK)             │       │ Id (PK)             │
│ ScanId (GUID)       │───┐   │ ScanJobId (FK)      │
│ TargetUrl           │   │   │ PageUrl             │
│ Email               │   │   │ ImageUrl            │
│ Status              │   │   │ MimeType            │
│ QueuePosition       │   └──►│ FileSize            │
│ SubmitterIp         │       │ Width               │
│ SubmissionCount     │       │ Height              │
│ CreatedAt           │       │ EstimatedWebPSize   │
│ StartedAt           │       │ IsWebP              │
│ CompletedAt         │       │ CreatedAt           │
│ PagesScanned        │       └─────────────────────┘
│ PagesDiscovered     │
│ ErrorMessage        │
└─────────────────────┘
```

### 5.2 Entity Definitions

**ScanJob**
```csharp
public class ScanJob
{
    public int Id { get; set; }
    public Guid ScanId { get; set; }                    // Public identifier
    public string TargetUrl { get; set; }               // URL to scan
    public string Email { get; set; }                   // Result delivery email
    public ScanStatus Status { get; set; }              // Queued, Processing, Completed, Failed
    public int QueuePosition { get; set; }              // Current position (0 = processing)
    public string SubmitterIp { get; set; }             // For fairness algorithm
    public int SubmissionCount { get; set; }            // Number of submissions from this IP
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int PagesScanned { get; set; }
    public int PagesDiscovered { get; set; }
    public string? ErrorMessage { get; set; }

    public ICollection<DiscoveredImage> DiscoveredImages { get; set; }
}

public enum ScanStatus
{
    Queued,
    Processing,
    Completed,
    Failed
}
```

**DiscoveredImage**
```csharp
public class DiscoveredImage
{
    public int Id { get; set; }
    public int ScanJobId { get; set; }
    public string PageUrl { get; set; }                 // Page where image was found
    public string ImageUrl { get; set; }                // Full image URL
    public string MimeType { get; set; }                // image/png, image/jpeg, etc.
    public long FileSize { get; set; }                  // Bytes
    public int? Width { get; set; }                     // Pixels
    public int? Height { get; set; }                    // Pixels
    public long? EstimatedWebPSize { get; set; }        // Calculated estimate
    public bool IsWebP { get; set; }                    // Quick filter flag
    public DateTime CreatedAt { get; set; }

    public ScanJob ScanJob { get; set; }
}
```

### 5.3 Data Retention
- Scan jobs and images are stored only during processing
- Data is deleted immediately after email is successfully sent
- Failed email delivery: retry 3 times over 15 minutes, then delete with error log
- No long-term data retention

---

## 6. User Interface

### 6.1 Design Principles
Based on reference sites (t3.chat, pic.ping.gg, uploadthing.com):

- **Dark-first design** with optional light mode
- **Generous whitespace** and clean typography
- **Subtle gradients** and glassmorphism effects
- **Smooth micro-animations** on interactions
- **Clear visual hierarchy** guiding user to primary action
- **Responsive** across all device sizes

### 6.2 Color Palette (Dark Mode Primary)
```css
--background: #0a0a0b;
--foreground: #fafafa;
--card: #18181b;
--card-foreground: #fafafa;
--primary: #3b82f6;          /* Blue accent */
--primary-foreground: #ffffff;
--secondary: #27272a;
--muted: #71717a;
--border: #27272a;
--success: #22c55e;
--warning: #eab308;
--error: #ef4444;
```

### 6.3 Page Layout

```
┌────────────────────────────────────────────────────────────────┐
│  Logo                                          GitHub  Theme   │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│                    [Animated gradient orb]                     │
│                                                                │
│              Find images slowing down your site                │
│                                                                │
│         Scan your website for non-WebP images and get          │
│            actionable recommendations to optimize              │
│                                                                │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │  https://yourwebsite.com                                  │ │
│  └──────────────────────────────────────────────────────────┘ │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │  you@email.com                                            │ │
│  └──────────────────────────────────────────────────────────┘ │
│                                                                │
│                    [ Scan My Website ]                         │
│                                                                │
│         Results will be emailed as a PDF report                │
│                                                                │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│     ┌─────────┐    ┌─────────┐    ┌─────────┐                 │
│     │   01    │    │   02    │    │   03    │                 │
│     │  Enter  │───►│  We     │───►│  Get    │                 │
│     │  URL    │    │  Scan   │    │  Report │                 │
│     └─────────┘    └─────────┘    └─────────┘                 │
│                                                                │
│                    Why WebP?                                   │
│     25-35% smaller • Same quality • Browser support: 97%+      │
│                                                                │
├────────────────────────────────────────────────────────────────┤
│  Open Source • Self-Hostable              Built with ♥         │
└────────────────────────────────────────────────────────────────┘
```

### 6.4 Progress State (After Submission)

```
┌────────────────────────────────────────────────────────────────┐
│                                                                │
│                    Scanning yourwebsite.com                    │
│                                                                │
│              ┌────────────────────────────────┐                │
│              │     Queue Position: 3 of 7     │                │
│              │                                │                │
│              │    ○ ○ ● ○ ○ ○ ○               │                │
│              │        ▲                       │                │
│              │      You                       │                │
│              └────────────────────────────────┘                │
│                                                                │
│       We'll email your results to you@email.com                │
│            Feel free to close this tab                         │
│                                                                │
└────────────────────────────────────────────────────────────────┘

--- OR (when actively scanning) ---

┌────────────────────────────────────────────────────────────────┐
│                                                                │
│                    Scanning yourwebsite.com                    │
│                                                                │
│              ┌────────────────────────────────┐                │
│              │                                │                │
│              │     [Animated scanner icon]    │                │
│              │                                │                │
│              │   Page 24 of 47 discovered     │                │
│              │   ━━━━━━━━━━━━━━━░░░░░░░ 51%   │                │
│              │                                │                │
│              │   Currently: /products/shoes   │                │
│              │                                │                │
│              │   🖼 12 non-WebP images found  │                │
│              │                                │                │
│              └────────────────────────────────┘                │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

### 6.5 Component Library
Recommend using **shadcn/ui** components as a base:
- Pre-built, accessible components
- Tailwind-based styling
- Easy to customize to match design system
- MIT licensed

---

## 7. Queue & Fairness System

### 7.1 Queue Behavior
- Single-threaded scan processing (configurable via `MaxConcurrentScans` setting)
- FIFO ordering with fairness adjustments
- Queue position updates broadcast via SignalR

### 7.2 Fairness Algorithm

**Problem**: Prevent a single user from monopolizing the queue by submitting many sites.

**Solution**: IP-based fairness with priority aging.

```
When a new scan request arrives:
1. Count existing queued jobs from same IP
2. If count > 0:
   - New job priority = base_time + (count * fairness_penalty)
   - This effectively inserts the job further back
3. Jobs are processed in priority order (lowest first)
4. Priority ages over time (jobs waiting longer get boosted)

Example:
- User A submits site1 at T=0, priority = 0
- User B submits site2 at T=1, priority = 1
- User A submits site3 at T=2, priority = 2 + (1 * 60) = 62
- User C submits site4 at T=3, priority = 3

Processing order: site1, site2, site4, site3
```

**Configuration**:
```json
{
  "Queue": {
    "MaxConcurrentScans": 1,
    "FairnessPenaltySeconds": 60,
    "MaxQueuedJobsPerIp": 5,
    "PriorityAgingIntervalSeconds": 30,
    "PriorityAgingBoostSeconds": 10
  }
}
```

### 7.3 Terms of Service Requirements
The fairness system should be clearly communicated:
- "One scan processes at a time"
- "Submitting multiple sites? Others may be served first to ensure fair access"
- "Maximum 5 sites queued per IP address"

---

## 8. Email & PDF Reports

### 8.1 Email Content

**Subject**: `Your WebP Image Scan Results for {domain}`

**Body**:
```html
Hi there,

Your WebP image scan for {fullUrl} is complete!

Quick Summary:
• Pages scanned: {pageCount}
• Total images found: {totalImages}
• Non-WebP images: {nonWebPCount}
• Potential savings: {savingsPercent}% ({savingsMB} MB)

Your detailed PDF report is attached. It includes every non-WebP image
found, along with estimated savings if converted to WebP format.

Need to scan another site? Visit {toolUrl}

---
WebP Image Scanner
Free & Open Source
```

### 8.2 PDF Report Sections

**Page 1: Cover**
- Tool logo/name
- "WebP Image Scan Report"
- Target URL
- Scan date and time

**Page 2: Executive Summary**
- Scan statistics in visual cards
- Pie chart: WebP vs non-WebP distribution
- Potential total savings highlighted

**Page 3+: Detailed Findings**
- Sortable table (sorted by potential savings, descending)
- Columns: Page URL, Image URL (truncated), Format, Size, Est. WebP Size, Savings %
- Rows zebra-striped for readability

**Final Page: Recommendations**
- Brief WebP benefits explanation
- Conversion options:
  - Manual: Squoosh.app, ImageMagick
  - Build tools: imagemin, sharp
  - CDNs: Cloudflare, imgix
  - CMS plugins: relevant plugins for WordPress, etc.
- `<picture>` element usage example

---

## 9. Security Considerations

### 9.1 Input Validation
| Input | Validation |
|-------|------------|
| URL | Valid URL format, http/https only, no localhost/private IPs |
| Email | Valid email format, reasonable length limit |
| All inputs | Sanitized to prevent injection |

### 9.2 Scan Security
- Puppeteer runs in sandboxed mode
- Network requests limited to target domain
- No JavaScript execution of user-controlled code
- Resource limits (memory, CPU, time) per scan
- No storage of credentials or sensitive data

### 9.3 Rate Limiting
- IP-based rate limiting: max 5 submissions per IP in queue
- Global rate limiting: max queue size of 100 (configurable)
- Cooldown after scan completion: 1 minute before same IP can submit again

### 9.4 Infrastructure
- Container runs as non-root user
- No sensitive data persisted (SQLite contains only transient queue data)
- SendGrid API key via environment variable, not in code
- HTTPS enforced in production deployments

### 9.5 SSRF Prevention
Block scans of:
- localhost, 127.0.0.1, ::1
- Private IP ranges (10.x, 172.16-31.x, 192.168.x)
- Link-local addresses
- Internal hostnames

---

## 10. Development Phases

### Phase 1: Foundation (Core Infrastructure)
**Goal**: Basic working system end-to-end

- [ ] Project scaffolding (React + .NET + Docker)
- [ ] SQLite database setup with EF Core
- [ ] Basic API endpoint for scan submission
- [ ] Simple queue implementation (FIFO)
- [ ] Puppeteer integration with basic crawling
- [ ] Image detection via CDP
- [ ] Basic console logging of results

**Deliverable**: Can submit URL, crawl site, output image list to console

---

### Phase 2: Real-Time & Queue
**Goal**: SignalR integration and fairness algorithm

- [ ] SignalR hub implementation
- [ ] React SignalR client integration
- [ ] Queue position tracking and updates
- [ ] Scan progress broadcasting
- [ ] Fairness algorithm implementation
- [ ] IP-based tracking

**Deliverable**: Users see live queue position and scan progress

---

### Phase 3: Reports & Email
**Goal**: PDF generation and email delivery

- [ ] QuestPDF report template
- [ ] Savings estimation algorithm
- [ ] SendGrid integration
- [ ] Email template design
- [ ] Retry logic for failed emails
- [ ] Data cleanup after delivery

**Deliverable**: Complete scan results in PDF via email

---

### Phase 4: UI Polish
**Goal**: Production-ready frontend

- [ ] Landing page design implementation
- [ ] Dark/light mode toggle
- [ ] Animations and micro-interactions
- [ ] Mobile responsive design
- [ ] Loading states and error handling
- [ ] Accessibility audit (WCAG 2.1 AA)

**Deliverable**: Polished, production-ready UI

---

### Phase 5: Hardening & Deployment
**Goal**: Production-ready system

- [ ] Security audit and fixes
- [ ] Rate limiting implementation
- [ ] Docker optimization (multi-stage build, size reduction)
- [ ] docker-compose for easy deployment
- [ ] Environment variable documentation
- [ ] README with self-hosting instructions
- [ ] Terms of Service page
- [ ] Error monitoring setup

**Deliverable**: Deployable, documented, open-source release

---

## 11. Technical Challenges & Mitigations

### 11.1 Large Websites
**Challenge**: Sites with thousands of pages could take hours and consume resources.

**Mitigations**:
- Implement configurable page limit (default: unlimited, but admin can set)
- Progress checkpointing (resume capability if interrupted)
- Timeout per scan (configurable, default: 30 minutes)
- Memory management: process pages in batches, clear Puppeteer cache
- User warning for sites > 500 pages discovered

---

### 11.2 Single Page Applications (SPAs)
**Challenge**: SPAs render content via JavaScript; links may not be in initial HTML.

**Mitigations**:
- Wait for `networkidle0` state before extracting links
- Execute scroll events to trigger lazy-loaded content
- Check for common SPA frameworks and apply specific strategies
- Fallback: sitemap.xml parsing if available

---

### 11.3 Rate Limiting / Anti-Bot
**Challenge**: Target sites may rate-limit or block the scanner.

**Mitigations**:
- Configurable delay between page requests (default: 500ms)
- Respect `robots.txt` `Crawl-delay` directive
- Realistic User-Agent string
- Exponential backoff on 429/503 responses
- Circuit breaker: abort scan if too many failures

---

### 11.4 Puppeteer in Docker
**Challenge**: Chromium requires specific dependencies in Docker.

**Mitigations**:
- Use official Puppeteer Docker image as base
- Or install required dependencies explicitly
- Use `--no-sandbox` flag carefully (container is already sandboxed)
- Limit memory and CPU via Docker constraints

**Dockerfile snippet**:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# Install Chromium dependencies
RUN apt-get update && apt-get install -y \
    chromium \
    fonts-liberation \
    libasound2 \
    libatk-bridge2.0-0 \
    libatk1.0-0 \
    libcups2 \
    libdbus-1-3 \
    libdrm2 \
    libgbm1 \
    libgtk-3-0 \
    libnspr4 \
    libnss3 \
    libxcomposite1 \
    libxdamage1 \
    libxrandr2 \
    xdg-utils \
    --no-install-recommends \
    && rm -rf /var/lib/apt/lists/*

ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium
```

---

### 11.5 WebP Savings Estimation
**Challenge**: Accurately estimating WebP file sizes without actual conversion.

**Mitigation**:
- Use empirical averages based on source format:
  - PNG → WebP: ~26% of original size
  - JPEG → WebP: ~75% of original size (JPEG is already compressed)
  - GIF (static) → WebP: ~50% of original size
- Note in report that estimates are approximate
- Consider optional actual conversion for accurate sizing (v2 feature)

---

## 12. Future Expansion

The architecture should accommodate these potential future features:

### 12.1 Additional Format Detection
- AVIF support checking (newer, even better compression)
- Properly sized images (detect oversized images for viewport)
- Responsive image (`srcset`) analysis

### 12.2 Extended Reporting
- Performance scoring (custom Core Web Vitals estimate)
- Comparison with competitors
- Historical tracking (would require user accounts)

### 12.3 Automation
- API access for CI/CD integration
- Scheduled recurring scans
- Webhook notifications

### 12.4 Conversion Service
- Optional: actually convert images and provide download
- Integration with CDN providers

---

## 13. Terms of Service Considerations

The following should be clearly communicated to users:

### 13.1 Fair Use Policy
- Tool is provided free for reasonable use
- Queue system ensures fair access for all users
- Submitting multiple sites from same IP will result in lower priority for subsequent requests
- Maximum 5 sites can be queued per IP address simultaneously

### 13.2 Limitations
- Scans only publicly accessible pages
- Pages requiring authentication will be skipped
- Very large sites may take significant time
- Results are estimates; actual savings may vary

### 13.3 Data Handling
- Email addresses used only for result delivery
- No data retained after results are sent
- No tracking or analytics beyond operational logging
- IP addresses used only for fairness algorithm

### 13.4 Disclaimer
- Tool scans third-party websites; users responsible for having permission
- Results are informational; no guarantee of accuracy
- Not responsible for actions taken based on results

---

## Appendix A: Configuration Reference

```json
{
  "Crawler": {
    "MaxConcurrentPages": 3,
    "PageTimeoutMs": 30000,
    "NetworkIdleTimeoutMs": 2000,
    "MaxRetries": 3,
    "RetryDelayBaseMs": 1000,
    "DelayBetweenPagesMs": 500,
    "MaxScanDurationMinutes": 30,
    "RespectRobotsTxt": true,
    "UserAgent": "WebPImageScanner/1.0 (+https://your-domain.com)"
  },
  "Queue": {
    "MaxConcurrentScans": 1,
    "MaxQueueSize": 100,
    "FairnessPenaltySeconds": 60,
    "MaxQueuedJobsPerIp": 5,
    "CooldownAfterScanSeconds": 60
  },
  "Email": {
    "Provider": "SendGrid",
    "FromEmail": "scanner@your-domain.com",
    "FromName": "WebP Image Scanner",
    "RetryAttempts": 3,
    "RetryDelaySeconds": 300
  },
  "Security": {
    "BlockPrivateIps": true,
    "MaxUrlLength": 2000,
    "MaxEmailLength": 254
  }
}
```

---

## Appendix B: Glossary

| Term | Definition |
|------|------------|
| WebP | Image format developed by Google offering superior compression |
| CDP | Chrome DevTools Protocol - low-level interface to Chrome/Chromium |
| Puppeteer | Node.js library for controlling headless Chrome |
| PuppeteerSharp | .NET port of Puppeteer |
| SignalR | Microsoft library for real-time web functionality |
| SSRF | Server-Side Request Forgery - security vulnerability |
| SPA | Single Page Application - JavaScript-rendered websites |

---

*This PRD is a living document and should be updated as requirements evolve.*
