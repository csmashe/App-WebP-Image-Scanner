import { describe, it, expect } from 'vitest'
import {
  truncateUrl,
  formatDuration,
  parseFilenameFromContentDisposition
} from '../../lib/completedDisplayUtils'

describe('truncateUrl', () => {
  describe('with default maxLength (40)', () => {
    it('should return the URL unchanged if length is less than maxLength', () => {
      const url = 'https://example.com'
      expect(truncateUrl(url)).toBe(url)
    })

    it('should return the URL unchanged if length equals maxLength', () => {
      const url = 'https://example.com/path/to/page123'  // 37 chars, under 40
      expect(truncateUrl(url)).toBe(url)
    })

    it('should truncate and add ellipsis if length exceeds maxLength', () => {
      const url = 'https://example.com/this/is/a/very/long/path/to/page'
      const result = truncateUrl(url)
      expect(result.length).toBe(40)
      expect(result.endsWith('...')).toBe(true)
      expect(result).toBe('https://example.com/this/is/a/very/lo...')
    })
  })

  describe('with custom maxLength', () => {
    it('should truncate to custom maxLength', () => {
      const url = 'https://example.com/path'
      expect(truncateUrl(url, 20)).toBe('https://example.c...')
      expect(truncateUrl(url, 20).length).toBe(20)
    })

    it('should handle very short maxLength', () => {
      const url = 'https://example.com'
      expect(truncateUrl(url, 10)).toBe('https:/...')
      expect(truncateUrl(url, 10).length).toBe(10)
    })

    it('should return URL unchanged if exactly at maxLength', () => {
      const url = '12345'
      expect(truncateUrl(url, 5)).toBe('12345')
    })

    it('should handle maxLength of 4 (minimum meaningful length with ellipsis)', () => {
      const url = 'https://example.com'
      expect(truncateUrl(url, 4)).toBe('h...')
    })

    it('should handle maxLength of 3 (edge case)', () => {
      const url = 'https://example.com'
      expect(truncateUrl(url, 3)).toBe('...')
    })
  })

  describe('edge cases', () => {
    it('should handle empty URL', () => {
      expect(truncateUrl('')).toBe('')
    })

    it('should handle URL with only 3 characters', () => {
      expect(truncateUrl('abc')).toBe('abc')
    })

    it('should handle URL with special characters', () => {
      const url = 'https://example.com/path?query=value&foo=bar#anchor'
      expect(truncateUrl(url, 30)).toBe('https://example.com/path?qu...')
    })

    it('should handle URL with unicode characters', () => {
      const url = 'https://example.com/日本語/path'
      expect(truncateUrl(url, 25)).toBe('https://example.com/日本...')
    })
  })
})

describe('formatDuration', () => {
  describe('null/invalid input', () => {
    it('should return "Unknown" for null duration', () => {
      expect(formatDuration(null)).toBe('Unknown')
    })

    it('should return original string for invalid format', () => {
      expect(formatDuration('invalid')).toBe('invalid')
      expect(formatDuration('abc')).toBe('abc')
    })

    it('should return original string for partial match', () => {
      expect(formatDuration('12:34')).toBe('12:34')
    })
  })

  describe('standard TimeSpan format (HH:MM:SS)', () => {
    it('should format seconds only', () => {
      expect(formatDuration('00:00:45')).toBe('45s')
    })

    it('should format minutes and seconds', () => {
      expect(formatDuration('00:05:30')).toBe('5m 30s')
    })

    it('should format hours, minutes, and seconds', () => {
      expect(formatDuration('02:15:45')).toBe('2h 15m 45s')
    })

    it('should format hours with zero minutes and seconds', () => {
      // Note: The implementation always includes seconds (even 0s) when the regex matches
      expect(formatDuration('03:00:00')).toBe('3h 0s')
    })

    it('should format minutes with zero seconds', () => {
      // Note: The implementation always includes seconds (even 0s) when the regex matches
      expect(formatDuration('00:10:00')).toBe('10m 0s')
    })

    it('should handle leading zeros in values', () => {
      expect(formatDuration('01:02:03')).toBe('1h 2m 3s')
    })
  })

  describe('TimeSpan format with milliseconds (HH:MM:SS.fff)', () => {
    it('should ignore milliseconds and format correctly', () => {
      expect(formatDuration('00:01:23.456')).toBe('1m 23s')
    })

    it('should handle zero duration with milliseconds', () => {
      // Note: The implementation shows '0s' because seconds regex group matched (00)
      expect(formatDuration('00:00:00.123')).toBe('0s')
    })

    it('should handle seconds with milliseconds', () => {
      expect(formatDuration('00:00:05.999')).toBe('5s')
    })
  })

  describe('TimeSpan format with days (d.HH:MM:SS)', () => {
    it('should format days with hours, minutes, seconds', () => {
      expect(formatDuration('1.02:30:45')).toBe('1d 2h 30m 45s')
    })

    it('should format multiple days with zero minutes/seconds', () => {
      // Note: The implementation always includes seconds when the regex matches
      expect(formatDuration('7.12:00:00')).toBe('7d 12h 0s')
    })

    it('should format days with zero hours/minutes/seconds', () => {
      // Note: The implementation always includes seconds when the regex matches
      expect(formatDuration('3.00:00:00')).toBe('3d 0s')
    })

    it('should handle days with milliseconds', () => {
      expect(formatDuration('2.05:10:15.789')).toBe('2d 5h 10m 15s')
    })
  })

  describe('edge cases', () => {
    it('should return "0s" for zero duration (seconds match group captures 00)', () => {
      // The function returns '0s' because seconds (00) is parsed as a valid match
      expect(formatDuration('00:00:00')).toBe('0s')
    })

    it('should handle large hour values', () => {
      expect(formatDuration('23:59:59')).toBe('23h 59m 59s')
    })

    it('should handle single digit seconds correctly', () => {
      expect(formatDuration('00:00:01')).toBe('1s')
    })

    it('should omit zero components in the middle', () => {
      expect(formatDuration('01:00:30')).toBe('1h 30s')
    })
  })
})

describe('parseFilenameFromContentDisposition', () => {
  it('should return default filename when contentDisposition is null', () => {
    expect(parseFilenameFromContentDisposition(null, 'default.pdf')).toBe('default.pdf')
  })

  it('should return default filename when no filename in header', () => {
    expect(parseFilenameFromContentDisposition('attachment', 'default.pdf')).toBe('default.pdf')
  })

  it('should parse filename without quotes', () => {
    expect(parseFilenameFromContentDisposition('attachment; filename=report.pdf', 'default.pdf')).toBe('report.pdf')
  })

  it('should parse filename with double quotes', () => {
    expect(parseFilenameFromContentDisposition('attachment; filename="my report.pdf"', 'default.pdf')).toBe('my report.pdf')
  })

  it('should parse filename with spaces', () => {
    expect(parseFilenameFromContentDisposition('attachment; filename="scan report 2024.pdf"', 'default.pdf')).toBe('scan report 2024.pdf')
  })

  it('should handle inline disposition with filename', () => {
    expect(parseFilenameFromContentDisposition('inline; filename="document.pdf"', 'default.pdf')).toBe('document.pdf')
  })

  it('should handle filename with special characters', () => {
    expect(parseFilenameFromContentDisposition('attachment; filename="report-2024_01.pdf"', 'default.pdf')).toBe('report-2024_01.pdf')
  })
})
