import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor, act } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import App from '../App'
import { useScanStore } from '../store/scanStore'
import * as toastModule from '../store/toastStore'
import * as apiModule from '../lib/api'

// Mock the SignalR module
vi.mock('@microsoft/signalr', () => {
  const mockConnection = {
    start: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
    invoke: vi.fn().mockResolvedValue(undefined),
    on: vi.fn(),
    onreconnecting: vi.fn(),
    onreconnected: vi.fn(),
    onclose: vi.fn(),
    state: 'Disconnected',
  }

  class MockHubConnectionBuilder {
    withUrl() { return this }
    withAutomaticReconnect() { return this }
    configureLogging() { return this }
    build() { return mockConnection }
  }

  return {
    HubConnectionBuilder: MockHubConnectionBuilder,
    HubConnectionState: {
      Disconnected: 'Disconnected',
      Connecting: 'Connecting',
      Connected: 'Connected',
    },
    LogLevel: { Warning: 3 },
  }
})

// Mock toast module
vi.mock('../store/toastStore', async () => {
  const actual = await vi.importActual<typeof toastModule>('../store/toastStore')
  return {
    ...actual,
    toast: {
      success: vi.fn(),
      error: vi.fn(),
      warning: vi.fn(),
      info: vi.fn(),
    },
  }
})

// Mock framer-motion to avoid animation issues in tests
vi.mock('framer-motion', () => ({
  motion: {
    div: ({ children, ...props }: React.HTMLAttributes<HTMLDivElement>) => <div {...props}>{children}</div>,
    section: ({ children, ...props }: React.HTMLAttributes<HTMLElement>) => <section {...props}>{children}</section>,
    h1: ({ children, ...props }: React.HTMLAttributes<HTMLHeadingElement>) => <h1 {...props}>{children}</h1>,
    h2: ({ children, ...props }: React.HTMLAttributes<HTMLHeadingElement>) => <h2 {...props}>{children}</h2>,
    h3: ({ children, ...props }: React.HTMLAttributes<HTMLHeadingElement>) => <h3 {...props}>{children}</h3>,
    p: ({ children, ...props }: React.HTMLAttributes<HTMLParagraphElement>) => <p {...props}>{children}</p>,
    span: ({ children, ...props }: React.HTMLAttributes<HTMLSpanElement>) => <span {...props}>{children}</span>,
    button: ({ children, ...props }: React.ButtonHTMLAttributes<HTMLButtonElement>) => <button {...props}>{children}</button>,
    form: ({ children, ...props }: React.FormHTMLAttributes<HTMLFormElement>) => <form {...props}>{children}</form>,
    a: ({ children, ...props }: React.AnchorHTMLAttributes<HTMLAnchorElement>) => <a {...props}>{children}</a>,
    img: (props: React.ImgHTMLAttributes<HTMLImageElement>) => <img {...props} />,
    ul: ({ children, ...props }: React.HTMLAttributes<HTMLUListElement>) => <ul {...props}>{children}</ul>,
    li: ({ children, ...props }: React.LiHTMLAttributes<HTMLLIElement>) => <li {...props}>{children}</li>,
    nav: ({ children, ...props }: React.HTMLAttributes<HTMLElement>) => <nav {...props}>{children}</nav>,
    header: ({ children, ...props }: React.HTMLAttributes<HTMLElement>) => <header {...props}>{children}</header>,
    footer: ({ children, ...props }: React.HTMLAttributes<HTMLElement>) => <footer {...props}>{children}</footer>,
  },
  AnimatePresence: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  useAnimation: () => ({
    start: vi.fn(),
    stop: vi.fn(),
    set: vi.fn(),
  }),
  useInView: () => true,
}))

// Save original values
const originalNavigator = window.navigator
let originalFetch: typeof fetch

describe('App', () => {
  let mockNavigatorOnLine = true

  beforeEach(() => {
    // Save and mock fetch
    originalFetch = window.fetch
    window.fetch = vi.fn().mockImplementation((url: string) => {
      // Mock /api/config endpoint
      if (url === '/api/config') {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ emailEnabled: true }),
        })
      }
      // Mock /api/scan endpoint
      if (url === '/api/scan') {
        return Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ scanId: 'scan-123', queuePosition: 1 }),
        })
      }
      return Promise.resolve({
        ok: true,
        json: () => Promise.resolve({}),
      })
    })

    // Reset store state
    useScanStore.getState().reset()

    // Reset mocks
    vi.clearAllMocks()

    // Setup navigator.onLine mock
    mockNavigatorOnLine = true
    Object.defineProperty(window, 'navigator', {
      value: {
        ...originalNavigator,
        get onLine() {
          return mockNavigatorOnLine
        },
      },
      writable: true,
      configurable: true,
    })

    // Mock location
    Object.defineProperty(window, 'location', {
      value: {
        pathname: '/',
        href: 'http://localhost/',
        origin: 'http://localhost',
      },
      writable: true,
      configurable: true,
    })

    // Mock history
    window.history.pushState = vi.fn()

    // Mock scrollTo
    window.scrollTo = vi.fn()

    // Reset localStorage
    vi.mocked(localStorage.getItem).mockReturnValue(null)
  })

  afterEach(() => {
    // Restore fetch
    window.fetch = originalFetch

    // Restore original values
    Object.defineProperty(window, 'navigator', {
      value: originalNavigator,
      writable: true,
      configurable: true,
    })
  })

  describe('initial render', () => {
    it('should render landing page with all main sections', async () => {
      render(<App />)

      // Wait for config to load
      await waitFor(() => {
        expect(screen.getByRole('main')).toBeInTheDocument()
      })

      // Check for main sections (Hero, ScanForm visible in idle state)
      expect(screen.getByRole('banner')).toBeInTheDocument() // Header
    })

    it('should show scan form when viewState is idle', async () => {
      render(<App />)

      // Wait for config to load and email field to appear
      await waitFor(() => {
        expect(screen.getByLabelText(/website url/i)).toBeInTheDocument()
      })

      // ScanForm should have URL input
      expect(screen.getByLabelText(/website url/i)).toBeInTheDocument()
    })
  })

  describe('handleSubmit', () => {
    it('should call API and update store on successful submission', async () => {
      const user = userEvent.setup()
      const mockApiFetch = vi.spyOn(apiModule, 'apiFetch').mockResolvedValue({
        scanId: 'scan-123',
        queuePosition: 1,
      })

      render(<App />)

      // Wait for config to load
      await waitFor(() => {
        expect(screen.getByLabelText(/website url/i)).toBeInTheDocument()
      })

      // Fill in the form
      const urlInput = screen.getByLabelText(/website url/i)
      await user.type(urlInput, 'https://example.com')

      // Submit the form (button says "Start Free Scan")
      const submitButton = screen.getByRole('button', { name: /start free scan/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockApiFetch).toHaveBeenCalledWith('/api/scan', {
          method: 'POST',
          body: JSON.stringify({
            url: 'https://example.com',
            email: '',
            convertToWebP: false,
          }),
        })
      })

      // Check toast was shown
      await waitFor(() => {
        expect(toastModule.toast.success).toHaveBeenCalledWith('Scan submitted!', 'Your scan has been queued.')
      })

      // Check store was updated
      const state = useScanStore.getState()
      expect(state.scanId).toBe('scan-123')
      expect(state.viewState).toBe('queued')
    })

    it('should show error toast when offline', async () => {
      const user = userEvent.setup()
      mockNavigatorOnLine = false

      render(<App />)

      // Wait for config to load
      await waitFor(() => {
        expect(screen.getByLabelText(/website url/i)).toBeInTheDocument()
      })

      // Fill in the form
      const urlInput = screen.getByLabelText(/website url/i)
      await user.type(urlInput, 'https://example.com')

      // Submit the form
      const submitButton = screen.getByRole('button', { name: /start free scan/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(toastModule.toast.error).toHaveBeenCalledWith(
          'No internet connection',
          'Please check your connection and try again.'
        )
      })
    })

    it('should handle rate limit errors', async () => {
      const user = userEvent.setup()
      const rateLimitError = new apiModule.ApiRequestError('Too many requests', 429)
      vi.spyOn(apiModule, 'apiFetch').mockRejectedValue(rateLimitError)

      render(<App />)

      // Wait for config to load
      await waitFor(() => {
        expect(screen.getByLabelText(/website url/i)).toBeInTheDocument()
      })

      await user.type(screen.getByLabelText(/website url/i), 'https://example.com')
      await user.click(screen.getByRole('button', { name: /start free scan/i }))

      await waitFor(() => {
        expect(toastModule.toast.error).toHaveBeenCalledWith(
          'Rate limited',
          'Too many requests. Please wait a moment before trying again.'
        )
      })
    })

    it('should handle network errors', async () => {
      const user = userEvent.setup()
      const networkError = new apiModule.ApiRequestError('Connection failed', 0)
      vi.spyOn(apiModule, 'apiFetch').mockRejectedValue(networkError)

      render(<App />)

      await waitFor(() => {
        expect(screen.getByLabelText(/website url/i)).toBeInTheDocument()
      })

      await user.type(screen.getByLabelText(/website url/i), 'https://example.com')
      await user.click(screen.getByRole('button', { name: /start free scan/i }))

      await waitFor(() => {
        expect(toastModule.toast.error).toHaveBeenCalledWith('Connection error', 'Connection failed')
      })
    })

    it('should handle validation errors', async () => {
      const user = userEvent.setup()
      const validationError = new apiModule.ApiRequestError('Invalid URL', 400)
      vi.spyOn(apiModule, 'apiFetch').mockRejectedValue(validationError)

      render(<App />)

      await waitFor(() => {
        expect(screen.getByLabelText(/website url/i)).toBeInTheDocument()
      })

      await user.type(screen.getByLabelText(/website url/i), 'https://example.com')
      await user.click(screen.getByRole('button', { name: /start free scan/i }))

      await waitFor(() => {
        expect(toastModule.toast.error).toHaveBeenCalledWith('Invalid request', 'Invalid URL')
      })
    })

    it('should handle generic API errors', async () => {
      const user = userEvent.setup()
      const genericError = new apiModule.ApiRequestError('Server error', 500)
      vi.spyOn(apiModule, 'apiFetch').mockRejectedValue(genericError)

      render(<App />)

      await waitFor(() => {
        expect(screen.getByLabelText(/website url/i)).toBeInTheDocument()
      })

      await user.type(screen.getByLabelText(/website url/i), 'https://example.com')
      await user.click(screen.getByRole('button', { name: /start free scan/i }))

      await waitFor(() => {
        expect(toastModule.toast.error).toHaveBeenCalledWith('Submission failed', 'Server error')
      })
    })

    it('should handle non-ApiRequestError exceptions', async () => {
      const user = userEvent.setup()
      vi.spyOn(apiModule, 'apiFetch').mockRejectedValue(new Error('Unexpected error'))

      render(<App />)

      await waitFor(() => {
        expect(screen.getByLabelText(/website url/i)).toBeInTheDocument()
      })

      await user.type(screen.getByLabelText(/website url/i), 'https://example.com')
      await user.click(screen.getByRole('button', { name: /start free scan/i }))

      await waitFor(() => {
        expect(toastModule.toast.error).toHaveBeenCalledWith('Submission failed', 'Unexpected error')
      })
    })

    it('should set connectionError in store on API error', async () => {
      const user = userEvent.setup()
      const error = new apiModule.ApiRequestError('Test error', 500)
      vi.spyOn(apiModule, 'apiFetch').mockRejectedValue(error)

      render(<App />)

      await waitFor(() => {
        expect(screen.getByLabelText(/website url/i)).toBeInTheDocument()
      })

      await user.type(screen.getByLabelText(/website url/i), 'https://example.com')
      await user.click(screen.getByRole('button', { name: /start free scan/i }))

      await waitFor(() => {
        expect(useScanStore.getState().connectionError).toBe('Test error')
      })
    })
  })

  describe('handleReset', () => {
    it('should reset scan state from completed view', async () => {
      // Set up store with a completed scan
      useScanStore.getState().startScan('scan-123', 'https://example.com', 'test@test.com', 1, false)
      useScanStore.getState().handleScanComplete({
        scanId: 'scan-123',
        totalPagesScanned: 10,
        totalImagesFound: 50,
        nonWebPImagesCount: 25,
        duration: '00:01:00',
        completedAt: '2024-01-01T00:01:00Z',
        reachedPageLimit: false,
      })

      render(<App />)

      // The CompletedDisplay should be visible with "Scan Another Website" button
      await waitFor(() => {
        expect(screen.getByRole('button', { name: /scan another website/i })).toBeInTheDocument()
      })

      await userEvent.click(screen.getByRole('button', { name: /scan another website/i }))

      // Verify store is reset
      const state = useScanStore.getState()
      expect(state.viewState).toBe('idle')
      expect(state.scanId).toBeNull()
    })

    it('should reset scan state from failed view', async () => {
      // Set up store with a failed scan
      useScanStore.getState().startScan('scan-123', 'https://example.com', 'test@test.com', 1, false)
      useScanStore.getState().handleScanFailed({
        scanId: 'scan-123',
        errorMessage: 'Connection timeout',
        failedAt: '2024-01-01T00:01:00Z',
      })

      render(<App />)

      // The FailedDisplay should be visible with "Try Again" button
      await waitFor(() => {
        expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument()
      })

      await userEvent.click(screen.getByRole('button', { name: /try again/i }))

      // Verify store is reset
      const state = useScanStore.getState()
      expect(state.viewState).toBe('idle')
      expect(state.scanId).toBeNull()
    })
  })

  describe('navigation', () => {
    it('should navigate to terms page when clicking terms link', async () => {
      const user = userEvent.setup()
      render(<App />)

      // Wait for the form to load
      await waitFor(() => {
        expect(screen.getByLabelText(/website url/i)).toBeInTheDocument()
      })

      // Find all terms links and click the first one (in the form)
      const termsLinks = screen.getAllByRole('link', { name: /terms of service/i })
      await user.click(termsLinks[0])

      // Check history.pushState was called with /terms
      expect(window.history.pushState).toHaveBeenCalledWith({}, '', '/terms')

      // Check scrollTo was called
      expect(window.scrollTo).toHaveBeenCalledWith(0, 0)
    })

    it('should handle popstate event for browser back/forward', async () => {
      render(<App />)

      // Wait for initial render
      await waitFor(() => {
        expect(screen.getByRole('main')).toBeInTheDocument()
      })

      // Simulate browser navigation to /terms
      Object.defineProperty(window, 'location', {
        value: { pathname: '/terms', href: 'http://localhost/terms', origin: 'http://localhost' },
        writable: true,
        configurable: true,
      })

      // Dispatch popstate event
      act(() => {
        window.dispatchEvent(new PopStateEvent('popstate'))
      })

      // The terms page should be rendered
      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /terms of service/i })).toBeInTheDocument()
      })
    })

    it('should render home page on root path', async () => {
      Object.defineProperty(window, 'location', {
        value: { pathname: '/', href: 'http://localhost/', origin: 'http://localhost' },
        writable: true,
        configurable: true,
      })

      render(<App />)

      // Wait for config to load
      await waitFor(() => {
        expect(screen.getByLabelText(/website url/i)).toBeInTheDocument()
      })

      // Home page should show scan form
      expect(screen.getByLabelText(/website url/i)).toBeInTheDocument()
    })
  })

  describe('conditional rendering based on viewState', () => {
    it('should show ScanForm when viewState is idle', async () => {
      useScanStore.getState().reset()
      render(<App />)

      await waitFor(() => {
        expect(screen.getByLabelText(/website url/i)).toBeInTheDocument()
      })
    })

    it('should show ScanProgress when viewState is queued', async () => {
      useScanStore.getState().startScan('scan-123', 'https://example.com', 'test@test.com', 3, false)

      render(<App />)

      // ScanProgress should be visible, not ScanForm
      await waitFor(() => {
        expect(screen.queryByLabelText(/website url/i)).not.toBeInTheDocument()
      })

      // Check for queue-related content - QueueDisplay shows "Waiting in Queue"
      expect(screen.getByText(/waiting in queue/i)).toBeInTheDocument()
    })

    it('should show ScanProgress when viewState is scanning', async () => {
      useScanStore.getState().startScan('scan-123', 'https://example.com', 'test@test.com', 1, false)
      useScanStore.getState().handleScanStarted({
        scanId: 'scan-123',
        targetUrl: 'https://example.com',
        startedAt: '2024-01-01T00:00:00Z',
      })

      render(<App />)

      // ScanForm should not be visible
      await waitFor(() => {
        expect(screen.queryByLabelText(/website url/i)).not.toBeInTheDocument()
      })

      // ScanningDisplay shows "Scanning in Progress"
      expect(screen.getByText(/scanning in progress/i)).toBeInTheDocument()
    })

    it('should show completed view when viewState is completed', async () => {
      useScanStore.getState().startScan('scan-123', 'https://example.com', 'test@test.com', 1, false)
      useScanStore.getState().handleScanComplete({
        scanId: 'scan-123',
        totalPagesScanned: 10,
        totalImagesFound: 50,
        nonWebPImagesCount: 25,
        duration: '00:01:00',
        completedAt: '2024-01-01T00:01:00Z',
        reachedPageLimit: false,
      })

      render(<App />)

      // Completed view should show "Scan Complete!"
      await waitFor(() => {
        expect(screen.queryByLabelText(/website url/i)).not.toBeInTheDocument()
      })

      expect(screen.getByText(/scan complete!/i)).toBeInTheDocument()
    })

    it('should show failed view when viewState is failed', async () => {
      useScanStore.getState().startScan('scan-123', 'https://example.com', 'test@test.com', 1, false)
      useScanStore.getState().handleScanFailed({
        scanId: 'scan-123',
        errorMessage: 'Connection timeout',
        failedAt: '2024-01-01T00:01:00Z',
      })

      render(<App />)

      // Failed view should show "Scan Failed"
      await waitFor(() => {
        expect(screen.queryByLabelText(/website url/i)).not.toBeInTheDocument()
      })

      expect(screen.getByText(/scan failed/i)).toBeInTheDocument()
    })
  })

  describe('ThemeProvider integration', () => {
    it('should wrap app with ThemeProvider', async () => {
      render(<App />)

      // The app should render without errors, meaning ThemeProvider is working
      await waitFor(() => {
        expect(screen.getByRole('main')).toBeInTheDocument()
      })
    })
  })

  describe('ErrorBoundary integration', () => {
    it('should wrap app with ErrorBoundary', async () => {
      render(<App />)

      // The app should render without errors
      await waitFor(() => {
        expect(screen.getByRole('main')).toBeInTheDocument()
      })
    })
  })

  describe('form submission with email', () => {
    it('should include email in submission when provided', async () => {
      const user = userEvent.setup()
      const mockApiFetch = vi.spyOn(apiModule, 'apiFetch').mockResolvedValue({
        scanId: 'scan-123',
        queuePosition: 1,
      })

      render(<App />)

      // Wait for email field to appear (after config loads)
      await waitFor(() => {
        expect(screen.getByLabelText(/email address/i)).toBeInTheDocument()
      })

      await user.type(screen.getByLabelText(/website url/i), 'https://example.com')
      await user.type(screen.getByLabelText(/email address/i), 'test@example.com')
      await user.click(screen.getByRole('button', { name: /start free scan/i }))

      await waitFor(() => {
        expect(mockApiFetch).toHaveBeenCalledWith('/api/scan', {
          method: 'POST',
          body: JSON.stringify({
            url: 'https://example.com',
            email: 'test@example.com',
            convertToWebP: false,
          }),
        })
      })
    })
  })

  describe('form submission with convertToWebP option', () => {
    it('should include convertToWebP flag when checked', async () => {
      const user = userEvent.setup()
      const mockApiFetch = vi.spyOn(apiModule, 'apiFetch').mockResolvedValue({
        scanId: 'scan-123',
        queuePosition: 1,
      })

      render(<App />)

      // Wait for form to load
      await waitFor(() => {
        expect(screen.getByLabelText(/website url/i)).toBeInTheDocument()
      })

      await user.type(screen.getByLabelText(/website url/i), 'https://example.com')

      // Check the WebP conversion checkbox
      const convertCheckbox = screen.getByRole('checkbox', { name: /convert images to webp/i })
      await user.click(convertCheckbox)

      await user.click(screen.getByRole('button', { name: /start free scan/i }))

      await waitFor(() => {
        expect(mockApiFetch).toHaveBeenCalledWith('/api/scan', {
          method: 'POST',
          body: JSON.stringify({
            url: 'https://example.com',
            email: '',
            convertToWebP: true,
          }),
        })
      })
    })
  })
})
