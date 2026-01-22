import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, act, renderHook } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import {
  getSystemTheme,
  getStoredTheme,
  resolveTheme,
  storeTheme,
} from '../../context/themeContextDef'
import { ThemeProvider } from '../../context/ThemeProvider'
import { useTheme } from '../../context/useTheme'

// Helper to mock matchMedia
function mockMatchMedia(prefersDark: boolean) {
  const listeners: ((e: MediaQueryListEvent) => void)[] = []

  const mediaQueryList = {
    matches: prefersDark,
    media: '(prefers-color-scheme: dark)',
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn((event: string, callback: (e: MediaQueryListEvent) => void) => {
      if (event === 'change') {
        listeners.push(callback)
      }
    }),
    removeEventListener: vi.fn((event: string, callback: (e: MediaQueryListEvent) => void) => {
      if (event === 'change') {
        const index = listeners.indexOf(callback)
        if (index !== -1) {
          listeners.splice(index, 1)
        }
      }
    }),
    dispatchEvent: vi.fn(),
  }

  // Function to simulate preference change
  const triggerChange = (newPrefersDark: boolean) => {
    mediaQueryList.matches = newPrefersDark
    listeners.forEach(listener => {
      listener({ matches: newPrefersDark } as MediaQueryListEvent)
    })
  }

  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    configurable: true,
    value: vi.fn().mockImplementation((query: string) => {
      if (query === '(prefers-color-scheme: dark)') {
        return mediaQueryList
      }
      return {
        matches: false,
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      }
    }),
  })

  return { mediaQueryList, triggerChange }
}

describe('themeContextDef utility functions', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    // Clear document classes
    document.documentElement.classList.remove('dark', 'light')
  })

  describe('getSystemTheme', () => {
    it('returns "dark" when prefers-color-scheme: dark matches', () => {
      mockMatchMedia(true)
      expect(getSystemTheme()).toBe('dark')
    })

    it('returns "light" when prefers-color-scheme: dark does not match', () => {
      mockMatchMedia(false)
      expect(getSystemTheme()).toBe('light')
    })
  })

  describe('getStoredTheme', () => {
    it('returns stored theme from localStorage when valid', () => {
      vi.mocked(localStorage.getItem).mockReturnValue('dark')
      expect(getStoredTheme()).toBe('dark')
      expect(localStorage.getItem).toHaveBeenCalledWith('webp-scanner-theme')
    })

    it('returns "light" when stored value is "light"', () => {
      vi.mocked(localStorage.getItem).mockReturnValue('light')
      expect(getStoredTheme()).toBe('light')
    })

    it('returns "system" when stored value is "system"', () => {
      vi.mocked(localStorage.getItem).mockReturnValue('system')
      expect(getStoredTheme()).toBe('system')
    })

    it('returns "system" when no value is stored', () => {
      vi.mocked(localStorage.getItem).mockReturnValue(null)
      expect(getStoredTheme()).toBe('system')
    })

    it('returns "system" when invalid value is stored', () => {
      vi.mocked(localStorage.getItem).mockReturnValue('invalid-theme')
      expect(getStoredTheme()).toBe('system')
    })
  })

  describe('resolveTheme', () => {
    it('returns "dark" when theme is "dark"', () => {
      expect(resolveTheme('dark')).toBe('dark')
    })

    it('returns "light" when theme is "light"', () => {
      expect(resolveTheme('light')).toBe('light')
    })

    it('resolves "system" to "dark" when system prefers dark', () => {
      mockMatchMedia(true)
      expect(resolveTheme('system')).toBe('dark')
    })

    it('resolves "system" to "light" when system prefers light', () => {
      mockMatchMedia(false)
      expect(resolveTheme('system')).toBe('light')
    })
  })

  describe('storeTheme', () => {
    it('stores theme in localStorage', () => {
      storeTheme('dark')
      expect(localStorage.setItem).toHaveBeenCalledWith('webp-scanner-theme', 'dark')
    })

    it('stores "light" theme', () => {
      storeTheme('light')
      expect(localStorage.setItem).toHaveBeenCalledWith('webp-scanner-theme', 'light')
    })

    it('stores "system" theme', () => {
      storeTheme('system')
      expect(localStorage.setItem).toHaveBeenCalledWith('webp-scanner-theme', 'system')
    })
  })
})

describe('ThemeProvider', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    // Clear document classes
    document.documentElement.classList.remove('dark', 'light')
    // Default to light system theme
    mockMatchMedia(false)
    // Default localStorage to return null
    vi.mocked(localStorage.getItem).mockReturnValue(null)
  })

  afterEach(() => {
    document.documentElement.classList.remove('dark', 'light')
  })

  it('provides theme context to children', () => {
    render(
      <ThemeProvider>
        <TestComponent />
      </ThemeProvider>
    )

    expect(screen.getByTestId('theme')).toHaveTextContent('system')
  })

  it('uses defaultTheme when no stored theme', () => {
    vi.mocked(localStorage.getItem).mockReturnValue(null)

    render(
      <ThemeProvider defaultTheme="dark">
        <TestComponent />
      </ThemeProvider>
    )

    expect(screen.getByTestId('theme')).toHaveTextContent('dark')
  })

  it('uses stored theme over defaultTheme', () => {
    vi.mocked(localStorage.getItem).mockReturnValue('light')

    render(
      <ThemeProvider defaultTheme="dark">
        <TestComponent />
      </ThemeProvider>
    )

    expect(screen.getByTestId('theme')).toHaveTextContent('light')
  })

  it('applies dark class to document when theme is dark', () => {
    vi.mocked(localStorage.getItem).mockReturnValue('dark')

    render(
      <ThemeProvider>
        <TestComponent />
      </ThemeProvider>
    )

    expect(document.documentElement.classList.contains('dark')).toBe(true)
    expect(document.documentElement.classList.contains('light')).toBe(false)
  })

  it('applies light class to document when theme is light', () => {
    vi.mocked(localStorage.getItem).mockReturnValue('light')

    render(
      <ThemeProvider>
        <TestComponent />
      </ThemeProvider>
    )

    expect(document.documentElement.classList.contains('light')).toBe(true)
    expect(document.documentElement.classList.contains('dark')).toBe(false)
  })

  it('resolves system theme to dark when system prefers dark', () => {
    mockMatchMedia(true)
    vi.mocked(localStorage.getItem).mockReturnValue('system')

    render(
      <ThemeProvider>
        <TestComponent />
      </ThemeProvider>
    )

    expect(screen.getByTestId('resolvedTheme')).toHaveTextContent('dark')
    expect(screen.getByTestId('isDark')).toHaveTextContent('true')
  })

  it('resolves system theme to light when system prefers light', () => {
    mockMatchMedia(false)
    vi.mocked(localStorage.getItem).mockReturnValue('system')

    render(
      <ThemeProvider>
        <TestComponent />
      </ThemeProvider>
    )

    expect(screen.getByTestId('resolvedTheme')).toHaveTextContent('light')
    expect(screen.getByTestId('isDark')).toHaveTextContent('false')
  })

  it('setTheme updates theme and persists to localStorage', async () => {
    const user = userEvent.setup()
    vi.mocked(localStorage.getItem).mockReturnValue('light')

    render(
      <ThemeProvider>
        <TestComponent />
      </ThemeProvider>
    )

    expect(screen.getByTestId('theme')).toHaveTextContent('light')

    await user.click(screen.getByTestId('set-dark'))

    expect(screen.getByTestId('theme')).toHaveTextContent('dark')
    expect(localStorage.setItem).toHaveBeenCalledWith('webp-scanner-theme', 'dark')
  })

  it('toggleTheme switches from dark to light', async () => {
    const user = userEvent.setup()
    vi.mocked(localStorage.getItem).mockReturnValue('dark')

    render(
      <ThemeProvider>
        <TestComponent />
      </ThemeProvider>
    )

    expect(screen.getByTestId('resolvedTheme')).toHaveTextContent('dark')

    await user.click(screen.getByTestId('toggle'))

    expect(screen.getByTestId('theme')).toHaveTextContent('light')
    expect(screen.getByTestId('resolvedTheme')).toHaveTextContent('light')
  })

  it('toggleTheme switches from light to dark', async () => {
    const user = userEvent.setup()
    vi.mocked(localStorage.getItem).mockReturnValue('light')

    render(
      <ThemeProvider>
        <TestComponent />
      </ThemeProvider>
    )

    expect(screen.getByTestId('resolvedTheme')).toHaveTextContent('light')

    await user.click(screen.getByTestId('toggle'))

    expect(screen.getByTestId('theme')).toHaveTextContent('dark')
    expect(screen.getByTestId('resolvedTheme')).toHaveTextContent('dark')
  })

  it('toggleTheme resolves system theme before toggling', async () => {
    const user = userEvent.setup()
    mockMatchMedia(true) // System prefers dark
    vi.mocked(localStorage.getItem).mockReturnValue('system')

    render(
      <ThemeProvider>
        <TestComponent />
      </ThemeProvider>
    )

    expect(screen.getByTestId('theme')).toHaveTextContent('system')
    expect(screen.getByTestId('resolvedTheme')).toHaveTextContent('dark')

    await user.click(screen.getByTestId('toggle'))

    // After toggle, theme should be explicit 'light' (opposite of resolved dark)
    expect(screen.getByTestId('theme')).toHaveTextContent('light')
    expect(screen.getByTestId('resolvedTheme')).toHaveTextContent('light')
  })

  it('updates theme class when theme changes', async () => {
    const user = userEvent.setup()
    vi.mocked(localStorage.getItem).mockReturnValue('light')

    render(
      <ThemeProvider>
        <TestComponent />
      </ThemeProvider>
    )

    expect(document.documentElement.classList.contains('light')).toBe(true)
    expect(document.documentElement.classList.contains('dark')).toBe(false)

    await user.click(screen.getByTestId('set-dark'))

    expect(document.documentElement.classList.contains('dark')).toBe(true)
    expect(document.documentElement.classList.contains('light')).toBe(false)
  })

  it('responds to system theme changes when in system mode', () => {
    const { triggerChange } = mockMatchMedia(false) // Start with light
    vi.mocked(localStorage.getItem).mockReturnValue('system')

    render(
      <ThemeProvider>
        <TestComponent />
      </ThemeProvider>
    )

    expect(screen.getByTestId('resolvedTheme')).toHaveTextContent('light')

    // Simulate system preference change to dark
    act(() => {
      triggerChange(true)
    })

    expect(screen.getByTestId('resolvedTheme')).toHaveTextContent('dark')
  })

  it('isDark reflects resolved theme correctly', () => {
    vi.mocked(localStorage.getItem).mockReturnValue('dark')

    render(
      <ThemeProvider>
        <TestComponent />
      </ThemeProvider>
    )

    expect(screen.getByTestId('isDark')).toHaveTextContent('true')
  })
})

describe('useTheme hook', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockMatchMedia(false)
    vi.mocked(localStorage.getItem).mockReturnValue(null)
    document.documentElement.classList.remove('dark', 'light')
  })

  it('returns context values when used within ThemeProvider', () => {
    const { result } = renderHook(() => useTheme(), {
      wrapper: ({ children }) => <ThemeProvider>{children}</ThemeProvider>,
    })

    expect(result.current.theme).toBeDefined()
    expect(result.current.resolvedTheme).toBeDefined()
    expect(result.current.setTheme).toBeInstanceOf(Function)
    expect(result.current.toggleTheme).toBeInstanceOf(Function)
    expect(typeof result.current.isDark).toBe('boolean')
  })

  it('throws error when used outside ThemeProvider', () => {
    // Suppress console.error for this test
    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {})

    expect(() => {
      renderHook(() => useTheme())
    }).toThrow('useTheme must be used within a ThemeProvider')

    consoleSpy.mockRestore()
  })

  it('setTheme can be called from hook', () => {
    vi.mocked(localStorage.getItem).mockReturnValue('light')

    const { result } = renderHook(() => useTheme(), {
      wrapper: ({ children }) => <ThemeProvider>{children}</ThemeProvider>,
    })

    expect(result.current.theme).toBe('light')

    act(() => {
      result.current.setTheme('dark')
    })

    expect(result.current.theme).toBe('dark')
  })

  it('toggleTheme can be called from hook', () => {
    vi.mocked(localStorage.getItem).mockReturnValue('light')

    const { result } = renderHook(() => useTheme(), {
      wrapper: ({ children }) => <ThemeProvider>{children}</ThemeProvider>,
    })

    expect(result.current.resolvedTheme).toBe('light')

    act(() => {
      result.current.toggleTheme()
    })

    expect(result.current.resolvedTheme).toBe('dark')
  })
})

// Test component that uses the theme context
function TestComponent() {
  const { theme, resolvedTheme, setTheme, toggleTheme, isDark } = useTheme()

  return (
    <div>
      <span data-testid="theme">{theme}</span>
      <span data-testid="resolvedTheme">{resolvedTheme}</span>
      <span data-testid="isDark">{String(isDark)}</span>
      <button data-testid="set-dark" onClick={() => setTheme('dark')}>Set Dark</button>
      <button data-testid="set-light" onClick={() => setTheme('light')}>Set Light</button>
      <button data-testid="set-system" onClick={() => setTheme('system')}>Set System</button>
      <button data-testid="toggle" onClick={toggleTheme}>Toggle</button>
    </div>
  )
}
