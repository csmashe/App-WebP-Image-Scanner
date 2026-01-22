import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor, cleanup, fireEvent } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ScanForm } from '../../components/landing/ScanForm'

// Mock framer-motion to avoid animation issues in tests
vi.mock('framer-motion', () => ({
  motion: {
    section: ({ children, ...props }: React.HTMLAttributes<HTMLElement>) => (
      <section {...props}>{children}</section>
    ),
  },
}))

// Mock analytics
vi.mock('../../components/analytics', () => ({
  trackScanSubmit: vi.fn(),
}))

describe('ScanForm', () => {
  const mockOnSubmit = vi.fn()
  const user = userEvent.setup()

  beforeEach(() => {
    vi.clearAllMocks()
    // Mock fetch for /api/config - default to email enabled
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
      json: () => Promise.resolve({ emailEnabled: true }),
    }))
  })

  afterEach(() => {
    cleanup()
    vi.restoreAllMocks()
  })

  describe('Rendering', () => {
    it('should render the URL input field', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByLabelText(/website url/i)).toBeInTheDocument()
      })
    })

    it('should render the email input field when email is enabled', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByLabelText(/email address/i)).toBeInTheDocument()
      })
    })

    it('should not render email input when email is disabled', async () => {
      vi.stubGlobal('fetch', vi.fn().mockResolvedValue({
        json: () => Promise.resolve({ emailEnabled: false }),
      }))

      render(<ScanForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.queryByLabelText(/email address/i)).not.toBeInTheDocument()
      })
    })

    it('should render the submit button', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      expect(screen.getByRole('button', { name: /start free scan/i })).toBeInTheDocument()
    })

    it('should render the convert to WebP checkbox', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      expect(screen.getByLabelText(/convert images to webp/i)).toBeInTheDocument()
    })
  })

  describe('URL Validation - UI Integration', () => {
    it('should show error when URL is empty on blur', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      const urlInput = screen.getByLabelText(/website url/i)
      await user.click(urlInput)
      await user.tab() // blur

      await waitFor(() => {
        expect(screen.getByText('URL is required')).toBeInTheDocument()
      })
    })

    it('should show error for invalid URL format', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      const urlInput = screen.getByLabelText(/website url/i)
      await user.type(urlInput, 'not-a-valid-url')
      await user.tab()

      await waitFor(() => {
        expect(screen.getByText('Please enter a valid URL (e.g., https://example.com)')).toBeInTheDocument()
      })
    })

    it('should show error for non-http/https protocol', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      const urlInput = screen.getByLabelText(/website url/i)
      await user.type(urlInput, 'ftp://example.com')
      await user.tab()

      await waitFor(() => {
        expect(screen.getByText('URL must use http or https protocol')).toBeInTheDocument()
      })
    })

    it('should show error for localhost URL', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      const urlInput = screen.getByLabelText(/website url/i)
      await user.type(urlInput, 'http://localhost')
      await user.tab()

      await waitFor(() => {
        expect(screen.getByText('Cannot scan localhost or private IP addresses')).toBeInTheDocument()
      })
    })

    it('should show error for private IPv4 addresses (127.0.0.1)', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      const urlInput = screen.getByLabelText(/website url/i)
      await user.type(urlInput, 'http://127.0.0.1')
      await user.tab()

      await waitFor(() => {
        expect(screen.getByText('Cannot scan localhost or private IP addresses')).toBeInTheDocument()
      })
    })

    it('should show error for private IPv4 addresses (192.168.x.x)', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      const urlInput = screen.getByLabelText(/website url/i)
      await user.type(urlInput, 'http://192.168.1.1')
      await user.tab()

      await waitFor(() => {
        expect(screen.getByText('Cannot scan localhost or private IP addresses')).toBeInTheDocument()
      })
    })

    it('should show error for private IPv4 addresses (10.x.x.x)', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      const urlInput = screen.getByLabelText(/website url/i)
      await user.type(urlInput, 'http://10.0.0.1')
      await user.tab()

      await waitFor(() => {
        expect(screen.getByText('Cannot scan localhost or private IP addresses')).toBeInTheDocument()
      })
    })

    it('should show error for IPv6 loopback addresses', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      const urlInput = screen.getByLabelText(/website url/i)
      // Use fireEvent.change instead of userEvent.type because brackets are special in userEvent
      fireEvent.change(urlInput, { target: { value: 'http://[::1]' } })
      fireEvent.blur(urlInput)

      await waitFor(() => {
        expect(screen.getByText('Cannot scan localhost or private IP addresses')).toBeInTheDocument()
      })
    })

    it('should not show error for valid public URL', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      const urlInput = screen.getByLabelText(/website url/i)
      await user.type(urlInput, 'https://example.com')
      await user.tab()

      await waitFor(() => {
        expect(screen.queryByText(/url is required/i)).not.toBeInTheDocument()
        expect(screen.queryByText(/please enter a valid url/i)).not.toBeInTheDocument()
        expect(screen.queryByText(/cannot scan localhost/i)).not.toBeInTheDocument()
      })
    })

    it('should clear error when valid URL is entered after error', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      const urlInput = screen.getByLabelText(/website url/i)

      // Trigger error
      await user.click(urlInput)
      await user.tab()
      await waitFor(() => {
        expect(screen.getByText('URL is required')).toBeInTheDocument()
      })

      // Fix the error
      await user.type(urlInput, 'https://example.com')

      await waitFor(() => {
        expect(screen.queryByText('URL is required')).not.toBeInTheDocument()
      })
    })
  })

  describe('Email Validation - UI Integration', () => {
    it('should not show error for empty email (optional field)', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByLabelText(/email address/i)).toBeInTheDocument()
      })

      const emailInput = screen.getByLabelText(/email address/i)
      await user.click(emailInput)
      await user.tab()

      await waitFor(() => {
        expect(screen.queryByText(/please enter a valid email/i)).not.toBeInTheDocument()
      })
    })

    it('should show error for invalid email format', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByLabelText(/email address/i)).toBeInTheDocument()
      })

      const emailInput = screen.getByLabelText(/email address/i)
      await user.type(emailInput, 'invalid-email')
      await user.tab()

      await waitFor(() => {
        expect(screen.getByText('Please enter a valid email address')).toBeInTheDocument()
      })
    })

    it('should not show error for valid email', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByLabelText(/email address/i)).toBeInTheDocument()
      })

      const emailInput = screen.getByLabelText(/email address/i)
      await user.type(emailInput, 'user@example.com')
      await user.tab()

      await waitFor(() => {
        expect(screen.queryByText(/please enter a valid email/i)).not.toBeInTheDocument()
      })
    })
  })

  describe('Form Submission', () => {
    it('should have submit button disabled with empty URL (validation prevents submission)', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      const submitButton = screen.getByRole('button', { name: /start free scan/i })

      // Button should be disabled when URL is empty
      expect(submitButton).toBeDisabled()

      // Validation error shown on blur
      const urlInput = screen.getByLabelText(/website url/i)
      await user.click(urlInput)
      await user.tab()

      await waitFor(() => {
        expect(screen.getByText('URL is required')).toBeInTheDocument()
      })
      expect(mockOnSubmit).not.toHaveBeenCalled()
    })

    it('should have submit button disabled with invalid URL (validation prevents submission)', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      const urlInput = screen.getByLabelText(/website url/i)
      await user.type(urlInput, 'http://localhost')
      await user.tab() // trigger validation on blur

      const submitButton = screen.getByRole('button', { name: /start free scan/i })

      // Button should be disabled when URL is invalid
      expect(submitButton).toBeDisabled()

      await waitFor(() => {
        expect(screen.getByText('Cannot scan localhost or private IP addresses')).toBeInTheDocument()
      })
      expect(mockOnSubmit).not.toHaveBeenCalled()
    })

    it('should call onSubmit with correct values when form is valid', async () => {
      mockOnSubmit.mockResolvedValue(undefined)
      render(<ScanForm onSubmit={mockOnSubmit} />)

      await waitFor(() => {
        expect(screen.getByLabelText(/email address/i)).toBeInTheDocument()
      })

      const urlInput = screen.getByLabelText(/website url/i)
      const emailInput = screen.getByLabelText(/email address/i)

      await user.type(urlInput, 'https://example.com')
      await user.type(emailInput, 'user@example.com')

      const submitButton = screen.getByRole('button', { name: /start free scan/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockOnSubmit).toHaveBeenCalledWith('https://example.com', 'user@example.com', false)
      })
    })

    it('should call onSubmit with convertToWebP flag when checkbox is checked', async () => {
      mockOnSubmit.mockResolvedValue(undefined)
      render(<ScanForm onSubmit={mockOnSubmit} />)

      const urlInput = screen.getByLabelText(/website url/i)
      const checkbox = screen.getByLabelText(/convert images to webp/i)

      await user.type(urlInput, 'https://example.com')
      await user.click(checkbox)

      const submitButton = screen.getByRole('button', { name: /start free scan/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(mockOnSubmit).toHaveBeenCalledWith('https://example.com', '', true)
      })
    })
  })

  describe('Submit Button State', () => {
    it('should disable submit button when URL is empty', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      const submitButton = screen.getByRole('button', { name: /start free scan/i })
      expect(submitButton).toBeDisabled()
    })

    it('should disable submit button when URL is invalid', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      const urlInput = screen.getByLabelText(/website url/i)
      await user.type(urlInput, 'invalid')
      await user.tab() // trigger validation

      const submitButton = screen.getByRole('button', { name: /start free scan/i })
      expect(submitButton).toBeDisabled()
    })

    it('should enable submit button when URL is valid', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      const urlInput = screen.getByLabelText(/website url/i)
      await user.type(urlInput, 'https://example.com')

      const submitButton = screen.getByRole('button', { name: /start free scan/i })

      await waitFor(() => {
        expect(submitButton).not.toBeDisabled()
      })
    })
  })

  describe('Loading State', () => {
    it('should show loading state when submitting', async () => {
      // Create a promise that we can control
      let resolveSubmit: () => void
      mockOnSubmit.mockImplementation(() => new Promise<void>((resolve) => {
        resolveSubmit = resolve
      }))

      render(<ScanForm onSubmit={mockOnSubmit} />)

      const urlInput = screen.getByLabelText(/website url/i)
      await user.type(urlInput, 'https://example.com')

      const submitButton = screen.getByRole('button', { name: /start free scan/i })
      await user.click(submitButton)

      await waitFor(() => {
        expect(screen.getByText(/starting scan/i)).toBeInTheDocument()
      })

      // Resolve the submission
      resolveSubmit!()

      await waitFor(() => {
        expect(screen.getByText(/start free scan/i)).toBeInTheDocument()
      })
    })

    it('should use external isSubmitting prop when provided', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} isSubmitting={true} />)

      expect(screen.getByText(/starting scan/i)).toBeInTheDocument()
    })

    it('should disable inputs during submission', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} isSubmitting={true} />)

      const urlInput = screen.getByLabelText(/website url/i)
      expect(urlInput).toBeDisabled()
    })
  })

  describe('Terms of Service Link', () => {
    it('should render terms of service link', async () => {
      render(<ScanForm onSubmit={mockOnSubmit} />)

      expect(screen.getByRole('link', { name: /terms of service/i })).toBeInTheDocument()
    })

    it('should call onNavigate when terms link is clicked with onNavigate prop', async () => {
      const mockNavigate = vi.fn()
      render(<ScanForm onSubmit={mockOnSubmit} onNavigate={mockNavigate} />)

      const termsLink = screen.getByRole('link', { name: /terms of service/i })
      await user.click(termsLink)

      expect(mockNavigate).toHaveBeenCalledWith('terms')
    })
  })
})
