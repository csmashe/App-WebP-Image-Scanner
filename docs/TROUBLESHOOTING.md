# Troubleshooting Guide

This guide covers common issues and their solutions when running WebP Scanner.

## Table of Contents

- [Docker Issues](#docker-issues)
- [Chromium/Puppeteer Issues](#chromiumpuppeteer-issues)
- [Email Delivery Issues](#email-delivery-issues)
- [Scan Issues](#scan-issues)
- [Performance Issues](#performance-issues)
- [SignalR/Real-time Updates](#signalrreal-time-updates)
- [Database Issues](#database-issues)

## Docker Issues

### Container Fails to Start

**Symptom:** Container exits immediately or health check fails.

**Check the logs:**
```bash
docker-compose logs webpscanner
```

**Common causes:**

1. **Port conflict:**
   ```bash
   # Check if port 5000 is in use
   lsof -i :5000

   # Use a different port in docker-compose.yml
   ports:
     - "8080:5000"
   ```

2. **Missing environment variables:**
   - Ensure `.env` file exists
   - Check `SENDGRID_API_KEY` is set if email is enabled

3. **Insufficient memory:**
   ```bash
   # Check container resource usage
   docker stats webpscanner

   # Increase memory limit in docker-compose.yml
   deploy:
     resources:
       limits:
         memory: 4G
   ```

### Permission Denied Errors

**Symptom:** Errors about file permissions in `/app/data`.

**Solution:** The entrypoint script should fix permissions automatically. If issues persist:

```bash
# Stop container
docker-compose down

# Remove volumes and recreate
docker-compose down -v
docker-compose up -d
```

### Build Fails

**Symptom:** `docker-compose build` fails.

**Solutions:**

1. **Clear Docker cache:**
   ```bash
   docker-compose build --no-cache
   ```

2. **Ensure sufficient disk space:**
   ```bash
   docker system df
   docker system prune -a  # WARNING: removes unused images
   ```

3. **Check network connectivity** for package downloads

## Chromium/Puppeteer Issues

### "Failed to launch browser" Error

**Symptom:** Scans fail with browser launch errors.

**Solutions:**

1. **Check Chromium installation (Docker):**
   ```bash
   docker exec webpscanner which chromium
   docker exec webpscanner chromium --version
   ```

2. **Verify Chromium path configuration:**
   ```yaml
   environment:
     - Crawler__ChromiumPath=/usr/bin/chromium
   ```

3. **Sandbox issues:** In Docker, sandbox must be disabled:
   ```yaml
   environment:
     - Crawler__EnableSandbox=false
   ```

### "Navigation timeout" Errors

**Symptom:** Pages fail to load with timeout errors.

**Solutions:**

1. **Increase page timeout:**
   ```json
   "Crawler": {
     "PageTimeoutSeconds": 60
   }
   ```

2. **Increase network idle timeout for SPAs:**
   ```json
   "Crawler": {
     "NetworkIdleTimeoutMs": 5000
   }
   ```

3. **Check if the target website is accessible** from the container:
   ```bash
   docker exec webpscanner wget -q --spider https://example.com && echo "OK" || echo "FAIL"
   ```

### High Memory Usage

**Symptom:** Container uses excessive memory or gets OOM killed.

**Solutions:**

1. **Reduce concurrent scans:**
   ```json
   "Queue": {
     "MaxConcurrentScans": 1
   }
   ```

2. **Reduce max pages per scan:**
   ```json
   "Crawler": {
     "MaxPagesPerScan": 50
   }
   ```

3. **Increase container memory limit:**
   ```yaml
   deploy:
     resources:
       limits:
         memory: 4G
   ```

## Email Delivery Issues

### Emails Not Sending

**Symptom:** Scans complete but no email is received.

**Check email configuration:**
```bash
# Verify environment variable is set
docker exec webpscanner printenv | grep SENDGRID
```

**Solutions:**

1. **Verify API key is correct:** Test in SendGrid dashboard
2. **Verify sender identity:** SendGrid requires verified sender identities
3. **Check email is enabled:**
   ```yaml
   environment:
     - Email__Enabled=true
   ```

4. **Check application logs for errors:**
   ```bash
   docker-compose logs webpscanner | grep -i email
   ```

### "Forbidden" Error from SendGrid

**Symptom:** 403 error when sending emails.

**Causes:**
- API key doesn't have Mail Send permission
- Sender identity not verified
- Account suspended or restricted

**Solution:** Generate a new API key with "Mail Send" permission in SendGrid dashboard.

### Emails Going to Spam

**Solutions:**

1. **Use domain authentication** in SendGrid instead of single sender verification
2. **Configure SPF, DKIM, and DMARC** records for your domain
3. **Use a professional sender address** (not @gmail.com, etc.)

## Scan Issues

### "URL validation failed" Error

**Symptom:** Cannot submit certain URLs.

**Common causes:**

1. **Private IP addresses are blocked** (SSRF protection):
   - `localhost`, `127.0.0.1`
   - `10.x.x.x`, `172.16-31.x.x`, `192.168.x.x`
   - Internal hostnames

2. **Invalid URL format:**
   - Must start with `http://` or `https://`
   - Must be a valid domain

3. **URL points to blocked network:**
   - Link-local addresses
   - Multicast addresses

### "Rate limited" Error

**Symptom:** 429 Too Many Requests error.

**Solutions:**

1. **Wait and retry:** Rate limits reset after the configured window
2. **Reduce request frequency**
3. **For development:** Increase rate limits or add exempt IPs:
   ```json
   "Security": {
     "MaxRequestsPerMinute": 100,
     "RateLimitExemptIps": ["your-ip-here"]
   }
   ```

### Scan Stuck in "Queued" Status

**Symptom:** Scan doesn't start processing.

**Solutions:**

1. **Check queue processor is running:**
   ```bash
   docker-compose logs webpscanner | grep -i queue
   ```

2. **Check for stuck processing jobs:**
   ```bash
   curl http://localhost:5000/api/health
   ```

3. **Restart the container:**
   ```bash
   docker-compose restart webpscanner
   ```

### Pages Not Being Discovered

**Symptom:** Scan finds fewer pages than expected.

**Causes:**

1. **robots.txt blocking:** Check if the site's robots.txt disallows crawling
2. **JavaScript-rendered links:** May need longer network idle time
3. **Authentication required:** Login pages are detected and skipped
4. **External domains filtered:** Links to other domains are ignored

**Solutions:**
```json
"Crawler": {
  "NetworkIdleTimeoutMs": 5000,
  "RespectRobotsTxt": false  // Only if you own the site
}
```

## Performance Issues

### Slow Scan Speed

**Symptom:** Scans take too long.

**Solutions:**

1. **Reduce delay between pages:**
   ```json
   "Crawler": {
     "DelayBetweenPagesMs": 200
   }
   ```

2. **Reduce timeouts for fast sites:**
   ```json
   "Crawler": {
     "PageTimeoutSeconds": 15,
     "NetworkIdleTimeoutMs": 1000
   }
   ```

3. **Increase concurrent scans** (requires more memory):
   ```json
   "Queue": {
     "MaxConcurrentScans": 4
   }
   ```

### High CPU Usage

**Symptom:** Container uses 100% CPU.

**Solutions:**

1. **Limit concurrent scans:**
   ```json
   "Queue": {
     "MaxConcurrentScans": 1
   }
   ```

2. **Add delay between pages:**
   ```json
   "Crawler": {
     "DelayBetweenPagesMs": 1000
   }
   ```

3. **Set CPU limits:**
   ```yaml
   deploy:
     resources:
       limits:
         cpus: '1'
   ```

## SignalR/Real-time Updates

### Progress Updates Not Showing

**Symptom:** UI doesn't update during scan.

**Solutions:**

1. **Check WebSocket support:** If behind a proxy, ensure WebSocket upgrade is configured

2. **Check browser console** for connection errors

3. **Verify SignalR endpoint is accessible:**
   ```javascript
   // Browser console
   fetch('/hubs/scanprogress/negotiate?negotiateVersion=1', {method: 'POST'})
   ```

### Connection Keeps Dropping

**Symptom:** "Disconnected" messages appear frequently.

**Solutions:**

1. **Check proxy timeout settings** - SignalR needs long-lived connections

2. **Nginx configuration:**
   ```nginx
   proxy_read_timeout 86400;
   proxy_send_timeout 86400;
   ```

3. **Network stability** - Check for intermittent connectivity

## Database Issues

### "Database is locked" Error

**Symptom:** SQLite database lock errors.

**Causes:**
- Multiple concurrent write operations
- Container crashed while writing

**Solutions:**

1. **Restart the container:**
   ```bash
   docker-compose restart webpscanner
   ```

2. **Check for orphaned lock files:**
   ```bash
   docker exec webpscanner ls -la /app/data/
   ```

3. **If persistent, recreate the database** (loses data):
   ```bash
   docker-compose down
   docker volume rm webp_webpscanner-data
   docker-compose up -d
   ```

### Migration Errors on Startup

**Symptom:** Application fails with migration-related errors.

**Solutions:**

1. **Check logs for specific error:**
   ```bash
   docker-compose logs webpscanner | grep -i migration
   ```

2. **Remove database and let it recreate:**
   ```bash
   docker-compose down
   docker volume rm webp_webpscanner-data
   docker-compose up -d
   ```

## Getting Help

If you've tried the solutions above and still have issues:

1. **Check existing issues:** [GitHub Issues](https://github.com/csmashe/App-WebP-Image-Scanner/issues)

2. **Gather diagnostic information:**
   ```bash
   # Application logs
   docker-compose logs webpscanner > logs.txt

   # Container info
   docker inspect webpscanner > container.json

   # System info
   docker info > docker-info.txt
   ```

3. **Open a new issue** with:
   - Description of the problem
   - Steps to reproduce
   - Relevant logs (remove sensitive information)
   - Environment details (OS, Docker version)
