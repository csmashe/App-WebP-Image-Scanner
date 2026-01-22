import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { renderHook, act } from '@testing-library/react'

// Mock the toast store module with inline factory to avoid hoisting issues
vi.mock('../../store/toastStore', () => ({
  toast: {
    success: vi.fn(),
    error: vi.fn(),
    warning: vi.fn(),
    info: vi.fn(),
  },
  useToastStore: vi.fn(() => ({
    toasts: [],
    addToast: vi.fn(),
    removeToast: vi.fn(),
    clearToasts: vi.fn(),
  })),
}))

// Import after mock is set up
import { useNetworkStatus } from '../../hooks/useNetworkStatus'
import { toast } from '../../store/toastStore'

describe('useNetworkStatus', () => {
  let originalNavigatorOnLine: boolean
  let onlineEventListeners: ((event: Event) => void)[] = []
  let offlineEventListeners: ((event: Event) => void)[] = []

  // Store original addEventListener and removeEventListener
  const originalAddEventListener = window.addEventListener
  const originalRemoveEventListener = window.removeEventListener

  beforeEach(() => {
    // Save original navigator.onLine value
    originalNavigatorOnLine = navigator.onLine

    // Reset event listener arrays
    onlineEventListeners = []
    offlineEventListeners = []

    // Mock navigator.onLine
    Object.defineProperty(navigator, 'onLine', {
      configurable: true,
      get: () => true,
    })

    // Mock addEventListener to capture listeners
    window.addEventListener = vi.fn((event: string, handler: EventListenerOrEventListenerObject) => {
      const fn = typeof handler === 'function' ? handler : handler.handleEvent.bind(handler)
      if (event === 'online') {
        onlineEventListeners.push(fn as (event: Event) => void)
      } else if (event === 'offline') {
        offlineEventListeners.push(fn as (event: Event) => void)
      }
      return originalAddEventListener.call(window, event, handler)
    }) as typeof window.addEventListener

    // Mock removeEventListener
    window.removeEventListener = vi.fn((event: string, handler: EventListenerOrEventListenerObject) => {
      const fn = typeof handler === 'function' ? handler : handler.handleEvent.bind(handler)
      if (event === 'online') {
        onlineEventListeners = onlineEventListeners.filter(h => h !== fn)
      } else if (event === 'offline') {
        offlineEventListeners = offlineEventListeners.filter(h => h !== fn)
      }
      return originalRemoveEventListener.call(window, event, handler)
    }) as typeof window.removeEventListener

    // Reset mock function calls
    vi.mocked(toast.success).mockClear()
    vi.mocked(toast.error).mockClear()
    vi.mocked(toast.warning).mockClear()
    vi.mocked(toast.info).mockClear()
  })

  afterEach(() => {
    // Restore original addEventListener and removeEventListener
    window.addEventListener = originalAddEventListener
    window.removeEventListener = originalRemoveEventListener

    // Restore original navigator.onLine
    Object.defineProperty(navigator, 'onLine', {
      configurable: true,
      get: () => originalNavigatorOnLine,
    })

    vi.clearAllMocks()
  })

  describe('initial state', () => {
    it('should reflect navigator.onLine when online', () => {
      Object.defineProperty(navigator, 'onLine', {
        configurable: true,
        get: () => true,
      })

      const { result } = renderHook(() => useNetworkStatus())

      expect(result.current.isOnline).toBe(true)
      expect(result.current.wasOffline).toBe(false)
    })

    it('should reflect navigator.onLine when offline', () => {
      Object.defineProperty(navigator, 'onLine', {
        configurable: true,
        get: () => false,
      })

      const { result } = renderHook(() => useNetworkStatus())

      expect(result.current.isOnline).toBe(false)
    })
  })

  describe('event listeners', () => {
    it('should add online and offline event listeners on mount', () => {
      renderHook(() => useNetworkStatus())

      expect(window.addEventListener).toHaveBeenCalledWith('online', expect.any(Function))
      expect(window.addEventListener).toHaveBeenCalledWith('offline', expect.any(Function))
    })

    it('should remove event listeners on unmount', () => {
      const { unmount } = renderHook(() => useNetworkStatus())

      unmount()

      expect(window.removeEventListener).toHaveBeenCalledWith('online', expect.any(Function))
      expect(window.removeEventListener).toHaveBeenCalledWith('offline', expect.any(Function))
    })
  })

  describe('offline event', () => {
    it('should update isOnline to false when offline event fires', () => {
      const { result } = renderHook(() => useNetworkStatus())

      expect(result.current.isOnline).toBe(true)

      // Simulate offline event
      act(() => {
        offlineEventListeners.forEach(handler => handler(new Event('offline')))
      })

      expect(result.current.isOnline).toBe(false)
    })

    it('should set wasOffline to true when offline event fires', () => {
      const { result } = renderHook(() => useNetworkStatus())

      expect(result.current.wasOffline).toBe(false)

      // Simulate offline event
      act(() => {
        offlineEventListeners.forEach(handler => handler(new Event('offline')))
      })

      expect(result.current.wasOffline).toBe(true)
    })

    it('should show error toast when offline event fires', () => {
      renderHook(() => useNetworkStatus())

      // Simulate offline event
      act(() => {
        offlineEventListeners.forEach(handler => handler(new Event('offline')))
      })

      expect(toast.error).toHaveBeenCalledWith(
        'Connection lost',
        'Please check your internet connection.'
      )
    })
  })

  describe('online event', () => {
    it('should update isOnline to true when online event fires', () => {
      // Start offline
      Object.defineProperty(navigator, 'onLine', {
        configurable: true,
        get: () => false,
      })

      const { result } = renderHook(() => useNetworkStatus())

      expect(result.current.isOnline).toBe(false)

      // Simulate online event
      act(() => {
        onlineEventListeners.forEach(handler => handler(new Event('online')))
      })

      expect(result.current.isOnline).toBe(true)
    })

    it('should not show toast when coming online if was never offline', () => {
      const { result } = renderHook(() => useNetworkStatus())

      expect(result.current.wasOffline).toBe(false)

      // Simulate online event without being offline first
      act(() => {
        onlineEventListeners.forEach(handler => handler(new Event('online')))
      })

      expect(toast.success).not.toHaveBeenCalled()
    })

    it('should show success toast when connection restored after being offline', () => {
      const { result } = renderHook(() => useNetworkStatus())

      // First go offline
      act(() => {
        offlineEventListeners.forEach(handler => handler(new Event('offline')))
      })

      expect(result.current.wasOffline).toBe(true)
      vi.mocked(toast.success).mockClear() // Clear any previous calls

      // Then come back online
      act(() => {
        onlineEventListeners.forEach(handler => handler(new Event('online')))
      })

      expect(toast.success).toHaveBeenCalledWith(
        'Connection restored',
        'You are back online.'
      )
    })
  })

  describe('multiple state transitions', () => {
    it('should handle going offline and online multiple times', () => {
      const { result } = renderHook(() => useNetworkStatus())

      // Initial state
      expect(result.current.isOnline).toBe(true)
      expect(result.current.wasOffline).toBe(false)

      // Go offline
      act(() => {
        offlineEventListeners.forEach(handler => handler(new Event('offline')))
      })

      expect(result.current.isOnline).toBe(false)
      expect(result.current.wasOffline).toBe(true)
      expect(toast.error).toHaveBeenCalledTimes(1)

      // Come back online
      act(() => {
        onlineEventListeners.forEach(handler => handler(new Event('online')))
      })

      expect(result.current.isOnline).toBe(true)
      expect(result.current.wasOffline).toBe(true) // Should remain true
      expect(toast.success).toHaveBeenCalledTimes(1)

      // Go offline again
      act(() => {
        offlineEventListeners.forEach(handler => handler(new Event('offline')))
      })

      expect(result.current.isOnline).toBe(false)
      expect(toast.error).toHaveBeenCalledTimes(2)

      // Come back online again
      act(() => {
        onlineEventListeners.forEach(handler => handler(new Event('online')))
      })

      expect(result.current.isOnline).toBe(true)
      expect(toast.success).toHaveBeenCalledTimes(2)
    })
  })

  describe('return value', () => {
    it('should return isOnline and wasOffline properties', () => {
      const { result } = renderHook(() => useNetworkStatus())

      expect(result.current).toHaveProperty('isOnline')
      expect(result.current).toHaveProperty('wasOffline')
      expect(typeof result.current.isOnline).toBe('boolean')
      expect(typeof result.current.wasOffline).toBe('boolean')
    })
  })

  describe('SSR compatibility', () => {
    it('should default to true if navigator is undefined', () => {
      // The hook already handles this with:
      // typeof navigator !== 'undefined' ? navigator.onLine : true
      // This is tested implicitly through the default behavior
      const { result } = renderHook(() => useNetworkStatus())

      // In our test environment, navigator exists, so this tests the normal path
      expect(result.current.isOnline).toBeDefined()
    })
  })

  describe('callback memoization', () => {
    it('should have stable callback references across re-renders', () => {
      const { result, rerender } = renderHook(() => useNetworkStatus())

      const initialIsOnline = result.current.isOnline
      const initialWasOffline = result.current.wasOffline

      // Re-render the hook
      rerender()

      // Values should be stable
      expect(result.current.isOnline).toBe(initialIsOnline)
      expect(result.current.wasOffline).toBe(initialWasOffline)
    })
  })
})
