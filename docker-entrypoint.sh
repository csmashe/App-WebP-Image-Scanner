#!/bin/sh
set -e

# Fix ownership of mounted volumes if running as root
# This is necessary because Docker named volumes are created with root ownership
if [ "$(id -u)" = "0" ]; then
    echo "Fixing permissions on data, logs, and converted-images directories..."
    chown -R webpscanner:webpscanner /app/data /app/logs /app/converted-images 2>/dev/null || true

    # Drop to non-root user and exec the application using gosu
    echo "Starting application as webpscanner user..."
    exec gosu webpscanner dotnet WebPScanner.Api.dll "$@"
else
    # Already running as non-root user
    exec dotnet WebPScanner.Api.dll "$@"
fi
