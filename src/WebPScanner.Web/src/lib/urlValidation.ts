/**
 * URL and email validation utilities for the ScanForm component.
 * Extracted for testability.
 */

/**
 * Check if a string is a valid IPv4 address
 */
export function isIPv4Address(str: string): boolean {
  const parts = str.split('.')
  if (parts.length !== 4) return false
  return parts.every(part => {
    const num = parseInt(part, 10)
    return !isNaN(num) && num >= 0 && num <= 255 && part === num.toString()
  })
}

/**
 * Check if IPv4 address is in a blocked (private/reserved) range
 */
export function isBlockedIPv4(ip: string): boolean {
  const parts = ip.split('.').map(p => parseInt(p, 10))
  const [a, b] = parts

  // 127.0.0.0/8 - Loopback (any 127.x.x.x)
  if (a === 127) return true

  // 10.0.0.0/8 - Private
  if (a === 10) return true

  // 172.16.0.0/12 - Private (172.16.x.x - 172.31.x.x)
  if (a === 172 && b >= 16 && b <= 31) return true

  // 192.168.0.0/16 - Private
  if (a === 192 && b === 168) return true

  // 169.254.0.0/16 - Link-local
  if (a === 169 && b === 254) return true

  // 0.0.0.0/8 - Current network
  if (a === 0) return true

  return false
}

/**
 * Expand abbreviated IPv6 addresses to full form
 */
export function expandIPv6(ip: string): string | null {
  try {
    // Handle :: expansion
    let parts = ip.split(':')

    // Find the :: position
    const emptyIndex = parts.indexOf('')
    if (emptyIndex !== -1) {
      // Count non-empty parts
      const nonEmptyParts = parts.filter(p => p !== '')
      const missingCount = 8 - nonEmptyParts.length

      // Build expanded array
      const expanded: string[] = []
      for (const part of parts) {
        if (part === '' && missingCount > 0) {
          // First empty string we encounter after ::, fill with zeros
          if (expanded.length === 0 || expanded[expanded.length - 1] !== '') {
            for (let i = 0; i < missingCount; i++) {
              expanded.push('0')
            }
          }
        } else if (part !== '') {
          expanded.push(part)
        }
      }
      parts = expanded
    }

    // Pad each part to 4 characters and join
    if (parts.length !== 8) return null
    return parts.map(p => p.padStart(4, '0')).join(':')
  } catch {
    return null
  }
}

/**
 * Check if IPv6 address is in a blocked (private/reserved) range
 */
export function isBlockedIPv6(ip: string): boolean {
  const normalized = ip.toLowerCase()

  // Check for IPv4-mapped IPv6 addresses (::ffff:x.x.x.x or 0:0:0:0:0:ffff:x.x.x.x)
  // These embed an IPv4 address that should be checked with isBlockedIPv4
  const ipv4MappedPrefixes = ['::ffff:', '0:0:0:0:0:ffff:']
  for (const prefix of ipv4MappedPrefixes) {
    if (normalized.startsWith(prefix)) {
      const ipv4Part = normalized.slice(prefix.length)
      // Check if the remaining part is a valid dotted-decimal IPv4
      if (isIPv4Address(ipv4Part)) {
        return isBlockedIPv4(ipv4Part)
      }
    }
  }

  // Also check for fully expanded IPv4-mapped form (0000:0000:0000:0000:0000:ffff:x.x.x.x)
  const expandedMappedPrefix = '0000:0000:0000:0000:0000:ffff:'
  const expanded = expandIPv6(normalized)
  if (expanded && expanded.startsWith(expandedMappedPrefix)) {
    const ipv4Part = normalized.split(':').pop()
    if (ipv4Part && isIPv4Address(ipv4Part)) {
      return isBlockedIPv4(ipv4Part)
    }
  }

  // ::1 - Loopback
  if (normalized === '::1' || normalized === '0:0:0:0:0:0:0:1') return true

  // :: - Unspecified
  if (normalized === '::' || normalized === '0:0:0:0:0:0:0:0') return true

  // Expand the IPv6 address to check prefixes
  const expandedForPrefix = expandIPv6(normalized)
  if (!expandedForPrefix) return false

  const firstSegment = parseInt(expandedForPrefix.split(':')[0], 16)

  // fc00::/7 - Unique Local Address (ULA) - fc00:: to fdff::
  // First byte is 0xfc or 0xfd (binary 1111110x)
  if ((firstSegment & 0xfe00) === 0xfc00) return true

  // fe80::/10 - Link-local - fe80:: to febf::
  // First 10 bits are 1111111010
  if ((firstSegment & 0xffc0) === 0xfe80) return true

  return false
}

/**
 * Validate a URL for the scan form
 * Returns undefined if valid, error message string if invalid
 */
export function validateUrl(value: string): string | undefined {
  if (!value.trim()) {
    return 'URL is required'
  }
  try {
    const parsed = new URL(value)
    if (!['http:', 'https:'].includes(parsed.protocol)) {
      return 'URL must use http or https protocol'
    }

    const hostname = parsed.hostname.toLowerCase()

    // Block localhost hostname
    if (hostname === 'localhost') {
      return 'Cannot scan localhost or private IP addresses'
    }

    // Check if it's an IPv6 address (wrapped in brackets in URL)
    if (hostname.startsWith('[') && hostname.endsWith(']')) {
      const ipv6 = hostname.slice(1, -1)
      if (isBlockedIPv6(ipv6)) {
        return 'Cannot scan localhost or private IP addresses'
      }
    }
    // Check if it's an IPv4 address
    else if (isIPv4Address(hostname)) {
      if (isBlockedIPv4(hostname)) {
        return 'Cannot scan localhost or private IP addresses'
      }
    }
    // Check for IPv6 without brackets (shouldn't happen in valid URLs, but check anyway)
    else if (hostname.includes(':')) {
      if (isBlockedIPv6(hostname)) {
        return 'Cannot scan localhost or private IP addresses'
      }
    }

    return undefined
  } catch {
    return 'Please enter a valid URL (e.g., https://example.com)'
  }
}

/**
 * Validate an email address for the scan form
 * Returns undefined if valid (empty email is valid since it's optional), error message string if invalid
 */
export function validateEmail(value: string): string | undefined {
  // Email is optional - only validate format if provided
  if (!value.trim()) {
    return undefined
  }
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  if (!emailRegex.test(value)) {
    return 'Please enter a valid email address'
  }
  return undefined
}
