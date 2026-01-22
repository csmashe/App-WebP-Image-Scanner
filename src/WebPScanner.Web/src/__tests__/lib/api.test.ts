import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { ApiRequestError, getErrorMessage, apiFetch } from '../../lib/api'

describe('ApiRequestError', () => {
  describe('constructor', () => {
    it('should create an error with message and status', () => {
      const error = new ApiRequestError('Test error', 400)

      expect(error.message).toBe('Test error')
      expect(error.status).toBe(400)
      expect(error.name).toBe('ApiRequestError')
      expect(error.code).toBeUndefined()
      expect(error.details).toBeUndefined()
    })

    it('should create an error with all properties', () => {
      const details = { field: ['Error 1', 'Error 2'] }
      const error = new ApiRequestError('Validation failed', 400, 'VALIDATION_ERROR', details)

      expect(error.message).toBe('Validation failed')
      expect(error.status).toBe(400)
      expect(error.code).toBe('VALIDATION_ERROR')
      expect(error.details).toEqual(details)
    })

    it('should be an instance of Error', () => {
      const error = new ApiRequestError('Test', 500)
      expect(error).toBeInstanceOf(Error)
    })
  })

  describe('isNetworkError', () => {
    it('should return true for status 0', () => {
      const error = new ApiRequestError('Network error', 0)
      expect(error.isNetworkError).toBe(true)
    })

    it('should return false for other status codes', () => {
      expect(new ApiRequestError('Bad request', 400).isNetworkError).toBe(false)
      expect(new ApiRequestError('Server error', 500).isNetworkError).toBe(false)
      expect(new ApiRequestError('Not found', 404).isNetworkError).toBe(false)
    })
  })

  describe('isRateLimited', () => {
    it('should return true for status 429', () => {
      const error = new ApiRequestError('Rate limited', 429)
      expect(error.isRateLimited).toBe(true)
    })

    it('should return false for other status codes', () => {
      expect(new ApiRequestError('Bad request', 400).isRateLimited).toBe(false)
      expect(new ApiRequestError('Server error', 500).isRateLimited).toBe(false)
      expect(new ApiRequestError('Network error', 0).isRateLimited).toBe(false)
    })
  })

  describe('isServerError', () => {
    it('should return true for status >= 500', () => {
      expect(new ApiRequestError('Internal error', 500).isServerError).toBe(true)
      expect(new ApiRequestError('Bad gateway', 502).isServerError).toBe(true)
      expect(new ApiRequestError('Service unavailable', 503).isServerError).toBe(true)
      expect(new ApiRequestError('Gateway timeout', 504).isServerError).toBe(true)
    })

    it('should return false for status < 500', () => {
      expect(new ApiRequestError('Bad request', 400).isServerError).toBe(false)
      expect(new ApiRequestError('Not found', 404).isServerError).toBe(false)
      expect(new ApiRequestError('Rate limited', 429).isServerError).toBe(false)
      expect(new ApiRequestError('Network error', 0).isServerError).toBe(false)
    })
  })

  describe('isValidationError', () => {
    it('should return true for status 400', () => {
      const error = new ApiRequestError('Validation error', 400)
      expect(error.isValidationError).toBe(true)
    })

    it('should return false for other status codes', () => {
      expect(new ApiRequestError('Unauthorized', 401).isValidationError).toBe(false)
      expect(new ApiRequestError('Server error', 500).isValidationError).toBe(false)
      expect(new ApiRequestError('Network error', 0).isValidationError).toBe(false)
    })
  })
})

describe('getErrorMessage', () => {
  describe('with server message', () => {
    it('should return server message if provided and valid', () => {
      expect(getErrorMessage(400, 'Invalid email format')).toBe('Invalid email format')
    })

    it('should return default message if server message is empty', () => {
      expect(getErrorMessage(400, '')).toBe('The request was invalid. Please check your input and try again.')
    })

    it('should return default message if server message is too long (>= 200 chars)', () => {
      const longMessage = 'a'.repeat(200)
      expect(getErrorMessage(400, longMessage)).toBe('The request was invalid. Please check your input and try again.')
    })

    it('should return server message if exactly at length limit (< 200 chars)', () => {
      const maxLengthMessage = 'a'.repeat(199)
      expect(getErrorMessage(400, maxLengthMessage)).toBe(maxLengthMessage)
    })
  })

  describe('network error (status 0)', () => {
    it('should return network error message', () => {
      expect(getErrorMessage(0)).toBe('Unable to connect to the server. Please check your internet connection and try again.')
    })
  })

  describe('client errors (4xx)', () => {
    it('should return message for 400 Bad Request', () => {
      expect(getErrorMessage(400)).toBe('The request was invalid. Please check your input and try again.')
    })

    it('should return message for 401 Unauthorized', () => {
      expect(getErrorMessage(401)).toBe('Authentication required. Please sign in and try again.')
    })

    it('should return message for 403 Forbidden', () => {
      expect(getErrorMessage(403)).toBe('You do not have permission to perform this action.')
    })

    it('should return message for 404 Not Found', () => {
      expect(getErrorMessage(404)).toBe('The requested resource was not found.')
    })

    it('should return message for 429 Too Many Requests', () => {
      expect(getErrorMessage(429)).toBe('Too many requests. Please wait a moment before trying again.')
    })
  })

  describe('server errors (5xx)', () => {
    it('should return message for 500 Internal Server Error', () => {
      expect(getErrorMessage(500)).toBe('An internal server error occurred. Please try again later.')
    })

    it('should return message for 502 Bad Gateway', () => {
      expect(getErrorMessage(502)).toBe('The server is temporarily unavailable. Please try again later.')
    })

    it('should return message for 503 Service Unavailable', () => {
      expect(getErrorMessage(503)).toBe('The service is currently unavailable. Please try again later.')
    })

    it('should return message for 504 Gateway Timeout', () => {
      expect(getErrorMessage(504)).toBe('The server took too long to respond. Please try again.')
    })

    it('should return generic server error for other 5xx codes', () => {
      expect(getErrorMessage(505)).toBe('A server error occurred. Please try again later.')
      expect(getErrorMessage(599)).toBe('A server error occurred. Please try again later.')
    })
  })

  describe('unknown status codes', () => {
    it('should return generic error for unknown client error codes', () => {
      expect(getErrorMessage(418)).toBe('An unexpected error occurred. Please try again.')
    })

    it('should return generic error for unusual status codes', () => {
      expect(getErrorMessage(299)).toBe('An unexpected error occurred. Please try again.')
    })
  })
})

describe('apiFetch', () => {
  const mockFetch = vi.fn()

  beforeEach(() => {
    vi.stubGlobal('fetch', mockFetch)
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    vi.clearAllMocks()
  })

  describe('successful responses', () => {
    it('should return parsed JSON for successful response', async () => {
      const responseData = { id: 1, name: 'Test' }
      mockFetch.mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: vi.fn().mockResolvedValueOnce(responseData),
      })

      const result = await apiFetch<typeof responseData>('/api/test')

      expect(result).toEqual(responseData)
      expect(mockFetch).toHaveBeenCalledWith('/api/test', {
        headers: { 'Content-Type': 'application/json' },
      })
    })

    it('should return empty object for non-JSON response', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'text/plain' }),
      })

      const result = await apiFetch('/api/test')

      expect(result).toEqual({})
    })

    it('should merge custom headers with default headers', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: vi.fn().mockResolvedValueOnce({}),
      })

      await apiFetch('/api/test', {
        headers: { 'Authorization': 'Bearer token' },
      })

      expect(mockFetch).toHaveBeenCalledWith('/api/test', {
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer token',
        },
      })
    })

    it('should pass through request options', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: true,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: vi.fn().mockResolvedValueOnce({}),
      })

      await apiFetch('/api/test', {
        method: 'POST',
        body: JSON.stringify({ data: 'test' }),
      })

      expect(mockFetch).toHaveBeenCalledWith('/api/test', {
        method: 'POST',
        body: JSON.stringify({ data: 'test' }),
        headers: { 'Content-Type': 'application/json' },
      })
    })
  })

  describe('error responses', () => {
    it('should throw ApiRequestError for 400 Bad Request with JSON error', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 400,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: vi.fn().mockResolvedValueOnce({ message: 'Invalid URL format' }),
      })

      await expect(apiFetch('/api/test')).rejects.toThrow(ApiRequestError)

      // Re-mock and test error properties
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 400,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: vi.fn().mockResolvedValueOnce({ message: 'Invalid URL format' }),
      })

      try {
        await apiFetch('/api/test')
      } catch (e) {
        expect(e).toBeInstanceOf(ApiRequestError)
        expect((e as ApiRequestError).message).toBe('Invalid URL format')
        expect((e as ApiRequestError).status).toBe(400)
      }
    })

    it('should throw ApiRequestError for 401 Unauthorized', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 401,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: vi.fn().mockResolvedValueOnce({}),
      })

      try {
        await apiFetch('/api/test')
      } catch (error) {
        expect(error).toBeInstanceOf(ApiRequestError)
        expect((error as ApiRequestError).status).toBe(401)
        expect((error as ApiRequestError).message).toBe('Authentication required. Please sign in and try again.')
      }
    })

    it('should throw ApiRequestError for 403 Forbidden', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 403,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: vi.fn().mockResolvedValueOnce({}),
      })

      try {
        await apiFetch('/api/test')
      } catch (error) {
        expect(error).toBeInstanceOf(ApiRequestError)
        expect((error as ApiRequestError).status).toBe(403)
        expect((error as ApiRequestError).message).toBe('You do not have permission to perform this action.')
      }
    })

    it('should throw ApiRequestError for 404 Not Found', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 404,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: vi.fn().mockResolvedValueOnce({}),
      })

      try {
        await apiFetch('/api/test')
      } catch (error) {
        expect(error).toBeInstanceOf(ApiRequestError)
        expect((error as ApiRequestError).status).toBe(404)
        expect((error as ApiRequestError).message).toBe('The requested resource was not found.')
      }
    })

    it('should throw ApiRequestError for 429 Rate Limited', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 429,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: vi.fn().mockResolvedValueOnce({}),
      })

      try {
        await apiFetch('/api/test')
      } catch (error) {
        expect(error).toBeInstanceOf(ApiRequestError)
        expect((error as ApiRequestError).status).toBe(429)
        expect((error as ApiRequestError).isRateLimited).toBe(true)
      }
    })

    it('should throw ApiRequestError for 500 Internal Server Error', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: vi.fn().mockResolvedValueOnce({}),
      })

      try {
        await apiFetch('/api/test')
      } catch (error) {
        expect(error).toBeInstanceOf(ApiRequestError)
        expect((error as ApiRequestError).status).toBe(500)
        expect((error as ApiRequestError).isServerError).toBe(true)
      }
    })

    it('should throw ApiRequestError for 502 Bad Gateway', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 502,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: vi.fn().mockResolvedValueOnce({}),
      })

      try {
        await apiFetch('/api/test')
      } catch (error) {
        expect(error).toBeInstanceOf(ApiRequestError)
        expect((error as ApiRequestError).status).toBe(502)
        expect((error as ApiRequestError).isServerError).toBe(true)
      }
    })

    it('should throw ApiRequestError for 503 Service Unavailable', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 503,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: vi.fn().mockResolvedValueOnce({}),
      })

      try {
        await apiFetch('/api/test')
      } catch (error) {
        expect(error).toBeInstanceOf(ApiRequestError)
        expect((error as ApiRequestError).status).toBe(503)
        expect((error as ApiRequestError).isServerError).toBe(true)
      }
    })

    it('should throw ApiRequestError for 504 Gateway Timeout', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 504,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: vi.fn().mockResolvedValueOnce({}),
      })

      try {
        await apiFetch('/api/test')
      } catch (error) {
        expect(error).toBeInstanceOf(ApiRequestError)
        expect((error as ApiRequestError).status).toBe(504)
        expect((error as ApiRequestError).isServerError).toBe(true)
      }
    })
  })

  describe('parseErrorResponse', () => {
    it('should parse JSON error with message field', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 400,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: vi.fn().mockResolvedValueOnce({ message: 'Custom error message' }),
      })

      try {
        await apiFetch('/api/test')
      } catch (error) {
        expect((error as ApiRequestError).message).toBe('Custom error message')
      }
    })

    it('should parse JSON error with error field', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 400,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: vi.fn().mockResolvedValueOnce({ error: 'Error from error field' }),
      })

      try {
        await apiFetch('/api/test')
      } catch (error) {
        expect((error as ApiRequestError).message).toBe('Error from error field')
      }
    })

    it('should parse JSON error with title field', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 400,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: vi.fn().mockResolvedValueOnce({ title: 'Error from title field' }),
      })

      try {
        await apiFetch('/api/test')
      } catch (error) {
        expect((error as ApiRequestError).message).toBe('Error from title field')
      }
    })

    it('should parse JSON error with code and details', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 400,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: vi.fn().mockResolvedValueOnce({
          message: 'Validation failed',
          code: 'VALIDATION_ERROR',
          errors: { email: ['Invalid format'] },
        }),
      })

      try {
        await apiFetch('/api/test')
      } catch (error) {
        expect((error as ApiRequestError).message).toBe('Validation failed')
        expect((error as ApiRequestError).code).toBe('VALIDATION_ERROR')
        expect((error as ApiRequestError).details).toEqual({ email: ['Invalid format'] })
      }
    })

    it('should handle plain text error response', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
        headers: new Headers({ 'content-type': 'text/plain' }),
      })

      try {
        await apiFetch('/api/test')
      } catch (error) {
        expect((error as ApiRequestError).message).toBe('An internal server error occurred. Please try again later.')
      }
    })

    it('should handle empty response body', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 404,
        headers: new Headers({}),
      })

      try {
        await apiFetch('/api/test')
      } catch (error) {
        expect((error as ApiRequestError).message).toBe('The requested resource was not found.')
      }
    })

    it('should handle malformed JSON gracefully', async () => {
      mockFetch.mockResolvedValueOnce({
        ok: false,
        status: 500,
        headers: new Headers({ 'content-type': 'application/json' }),
        json: vi.fn().mockRejectedValueOnce(new Error('Invalid JSON')),
      })

      try {
        await apiFetch('/api/test')
      } catch (error) {
        expect((error as ApiRequestError).message).toBe('An internal server error occurred. Please try again later.')
      }
    })
  })

  describe('network failures', () => {
    it('should throw ApiRequestError with status 0 for fetch network error', async () => {
      mockFetch.mockRejectedValueOnce(new TypeError('Failed to fetch'))

      try {
        await apiFetch('/api/test')
      } catch (error) {
        expect(error).toBeInstanceOf(ApiRequestError)
        expect((error as ApiRequestError).status).toBe(0)
        expect((error as ApiRequestError).isNetworkError).toBe(true)
        expect((error as ApiRequestError).message).toBe(
          'Unable to connect to the server. Please check your internet connection and try again.'
        )
      }
    })

    it('should handle generic TypeError with fetch in message', async () => {
      mockFetch.mockRejectedValueOnce(new TypeError('fetch failed: network error'))

      try {
        await apiFetch('/api/test')
      } catch (error) {
        expect(error).toBeInstanceOf(ApiRequestError)
        expect((error as ApiRequestError).status).toBe(0)
        expect((error as ApiRequestError).isNetworkError).toBe(true)
      }
    })

    it('should handle non-TypeError errors', async () => {
      mockFetch.mockRejectedValueOnce(new Error('Some other error'))

      try {
        await apiFetch('/api/test')
      } catch (error) {
        expect(error).toBeInstanceOf(ApiRequestError)
        expect((error as ApiRequestError).status).toBe(0)
        expect((error as ApiRequestError).message).toBe('Some other error')
      }
    })

    it('should handle non-Error thrown values', async () => {
      mockFetch.mockRejectedValueOnce('string error')

      try {
        await apiFetch('/api/test')
      } catch (error) {
        expect(error).toBeInstanceOf(ApiRequestError)
        expect((error as ApiRequestError).status).toBe(0)
        expect((error as ApiRequestError).message).toBe('An unexpected error occurred')
      }
    })

    it('should re-throw ApiRequestError without wrapping', async () => {
      const originalError = new ApiRequestError('Original error', 400)
      mockFetch.mockImplementationOnce(() => Promise.reject(originalError))

      try {
        await apiFetch('/api/test')
      } catch (error) {
        expect(error).toBe(originalError)
        expect((error as ApiRequestError).message).toBe('Original error')
        expect((error as ApiRequestError).status).toBe(400)
      }
    })
  })
})
