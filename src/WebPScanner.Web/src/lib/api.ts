/** API error response structure */
export interface ApiError {
  message: string
  code?: string
  details?: Record<string, string[]>
}

/** Custom error class for API errors */
export class ApiRequestError extends Error {
  public readonly status: number
  public readonly code?: string
  public readonly details?: Record<string, string[]>

  constructor(message: string, status: number, code?: string, details?: Record<string, string[]>) {
    super(message)
    this.name = 'ApiRequestError'
    this.status = status
    this.code = code
    this.details = details
  }

  /** Check if this is a network error */
  get isNetworkError(): boolean {
    return this.status === 0
  }

  /** Check if this is a rate limit error */
  get isRateLimited(): boolean {
    return this.status === 429
  }

  /** Check if this is a server error */
  get isServerError(): boolean {
    return this.status >= 500
  }

  /** Check if this is a validation error */
  get isValidationError(): boolean {
    return this.status === 400
  }
}

/** Map HTTP status codes to user-friendly messages */
export function getErrorMessage(status: number, serverMessage?: string): string {
  if (serverMessage && serverMessage.length > 0 && serverMessage.length < 200) {
    return serverMessage
  }

  switch (status) {
    case 0:
      return 'Unable to connect to the server. Please check your internet connection and try again.'
    case 400:
      return 'The request was invalid. Please check your input and try again.'
    case 401:
      return 'Authentication required. Please sign in and try again.'
    case 403:
      return 'You do not have permission to perform this action.'
    case 404:
      return 'The requested resource was not found.'
    case 429:
      return 'Too many requests. Please wait a moment before trying again.'
    case 500:
      return 'An internal server error occurred. Please try again later.'
    case 502:
      return 'The server is temporarily unavailable. Please try again later.'
    case 503:
      return 'The service is currently unavailable. Please try again later.'
    case 504:
      return 'The server took too long to respond. Please try again.'
    default:
      if (status >= 500) {
        return 'A server error occurred. Please try again later.'
      }
      return 'An unexpected error occurred. Please try again.'
  }
}

/** Parse error response from API */
async function parseErrorResponse(response: Response): Promise<ApiError> {
  try {
    const contentType = response.headers.get('content-type')
    if (contentType?.includes('application/json')) {
      const data = await response.json()
      return {
        message: data.message || data.error || data.title || getErrorMessage(response.status),
        code: data.code,
        details: data.errors || data.details,
      }
    }
    return { message: getErrorMessage(response.status) }
  } catch {
    return { message: getErrorMessage(response.status) }
  }
}

/** Generic fetch wrapper with error handling */
export async function apiFetch<T>(
  url: string,
  options: RequestInit = {}
): Promise<T> {
  try {
    const response = await fetch(url, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...options.headers,
      },
    })

    if (!response.ok) {
      const error = await parseErrorResponse(response)
      throw new ApiRequestError(error.message, response.status, error.code, error.details)
    }

    const contentType = response.headers.get('content-type')
    if (!contentType?.includes('application/json')) {
      return {} as T
    }

    return await response.json()
  } catch (error) {
    if (error instanceof ApiRequestError) {
      throw error
    }

    if (error instanceof TypeError && error.message.includes('fetch')) {
      throw new ApiRequestError(
        'Unable to connect to the server. Please check your internet connection and try again.',
        0
      )
    }

    throw new ApiRequestError(
      error instanceof Error ? error.message : 'An unexpected error occurred',
      0
    )
  }
}
