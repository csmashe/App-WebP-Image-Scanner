# Build stage for .NET API
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-api
WORKDIR /src

# Copy project files (not solution to avoid test project references)
COPY src/WebPScanner.Api/WebPScanner.Api.csproj src/WebPScanner.Api/
COPY src/WebPScanner.Core/WebPScanner.Core.csproj src/WebPScanner.Core/
COPY src/WebPScanner.Data/WebPScanner.Data.csproj src/WebPScanner.Data/

# Restore dependencies for API project (which pulls in Core and Data)
RUN dotnet restore src/WebPScanner.Api/WebPScanner.Api.csproj

# Copy remaining source code and build
COPY src/WebPScanner.Api/ src/WebPScanner.Api/
COPY src/WebPScanner.Core/ src/WebPScanner.Core/
COPY src/WebPScanner.Data/ src/WebPScanner.Data/

RUN dotnet publish src/WebPScanner.Api/WebPScanner.Api.csproj -c Release -o /app/publish

# Build stage for React frontend
FROM node:22-alpine AS build-web
WORKDIR /app

# Build args for frontend environment variables
ARG VITE_SENTRY_DSN
ARG VITE_GA_MEASUREMENT_ID

# Copy package files and install dependencies
COPY src/WebPScanner.Web/package*.json ./
RUN npm ci --ignore-scripts

# Copy source and build
COPY src/WebPScanner.Web/ ./
RUN npm run build

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install Chromium dependencies and tools
# Note: Ubuntu 24.04 chromium package is snap-based and does not work in Docker
RUN apt-get update && apt-get install -y \
    fonts-liberation \
    libasound2t64 \
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
    libxkbcommon0 \
    xdg-utils \
    wget \
    gosu \
    unzip \
    --no-install-recommends \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/* \
    && rm -rf /tmp/* \
    && rm -rf /var/tmp/*

# Install Chrome for Testing (Googles official headless Chrome for automation)
RUN wget -q https://storage.googleapis.com/chrome-for-testing-public/131.0.6778.204/linux64/chrome-linux64.zip \
    && unzip chrome-linux64.zip -d /opt \
    && rm chrome-linux64.zip \
    && ln -s /opt/chrome-linux64/chrome /usr/bin/chromium

# Set Puppeteer to use Chrome for Testing
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium
ENV PUPPETEER_SKIP_CHROMIUM_DOWNLOAD=true

# Security: Remove unnecessary setuid binaries
RUN find / -perm /6000 -type f -exec chmod a-s {} \; 2>/dev/null || true

# Create non-root user for security (UID 10000 to avoid conflicts with base image)
RUN groupadd -r -g 10000 webpscanner && \
    useradd -r -u 10000 -g webpscanner -d /app -s /sbin/nologin webpscanner

# Create necessary directories with proper permissions
# Note: /app/data and /app/converted-images will be overridden by volume mounts, so permissions are set in entrypoint
RUN mkdir -p /app/data /app/wwwroot /app/logs /app/converted-images && \
    chown -R webpscanner:webpscanner /app

# Copy published API with proper ownership
COPY --from=build-api --chown=webpscanner:webpscanner /app/publish .

# Copy built frontend to wwwroot with proper ownership
COPY --from=build-web --chown=webpscanner:webpscanner /app/dist ./wwwroot

# Copy entrypoint script
COPY --chown=webpscanner:webpscanner docker-entrypoint.sh /app/docker-entrypoint.sh

# Security: Set restrictive file permissions on application files
RUN chmod -R 755 /app && \
    chmod -R 644 /app/*.dll /app/*.json 2>/dev/null || true && \
    chmod 755 /app/WebPScanner.Api.dll && \
    chmod 755 /app/docker-entrypoint.sh

# Note: We run as root initially to fix volume permissions in entrypoint,
# then drop to non-root user. This is a common Docker pattern for named volumes.

# Expose port (non-privileged port)
EXPOSE 5000

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Crawler configuration
ENV Crawler__ChromiumPath=/usr/bin/chromium
# Sandbox disabled: Docker provides isolation. Enable requires SYS_ADMIN capability.
ENV Crawler__EnableSandbox=false
ENV Crawler__RestrictToTargetDomain=true
ENV Crawler__BlockTrackingDomains=true

# Health check using wget (more secure than curl)
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:5000/api/health || exit 1

ENTRYPOINT ["/app/docker-entrypoint.sh"]
