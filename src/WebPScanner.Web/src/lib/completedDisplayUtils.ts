/** Truncate URL for display */
export function truncateUrl(url: string, maxLength: number = 40): string {
  if (url.length <= maxLength) return url
  return url.substring(0, maxLength - 3) + '...'
}

/** Format duration string from TimeSpan format */
export function formatDuration(duration: string | null): string {
  if (!duration) return 'Unknown'

  // Parse TimeSpan format: "00:01:23.456" or similar
  const match = duration.match(/(?:(\d+)\.)?(\d{2}):(\d{2}):(\d{2})(?:\.(\d+))?/)
  if (!match) return duration

  const [, days, hours, minutes, seconds] = match
  const parts: string[] = []

  if (days && parseInt(days) > 0) {
    parts.push(`${days}d`)
  }
  if (hours && parseInt(hours) > 0) {
    parts.push(`${parseInt(hours)}h`)
  }
  if (minutes && parseInt(minutes) > 0) {
    parts.push(`${parseInt(minutes)}m`)
  }
  if (seconds) {
    parts.push(`${parseInt(seconds)}s`)
  }

  return parts.length > 0 ? parts.join(' ') : '< 1s'
}

/**
 * Parse filename from Content-Disposition header.
 */
export function parseFilenameFromContentDisposition(
  contentDisposition: string | null,
  defaultFilename: string
): string {
  if (!contentDisposition) return defaultFilename
  const match = contentDisposition.match(/filename="?([^";\n]+)"?/)
  return match ? match[1] : defaultFilename
}
