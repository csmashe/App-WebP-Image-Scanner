import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, act, cleanup } from '@testing-library/react'
import { AnimatedCounter, AnimatedFormattedValue } from '../../components/common/AnimatedCounter'

describe('AnimatedCounter', () => {
  let rafCallbacks: ((time: number) => void)[] = []
  let rafId = 0
  let mockNow = 0

  beforeEach(() => {
    rafCallbacks = []
    rafId = 0
    mockNow = 0

    // Mock performance.now
    vi.spyOn(performance, 'now').mockImplementation(() => mockNow)

    // Mock requestAnimationFrame
    vi.spyOn(window, 'requestAnimationFrame').mockImplementation((cb) => {
      rafCallbacks.push(cb)
      return ++rafId
    })

    // Mock cancelAnimationFrame
    vi.spyOn(window, 'cancelAnimationFrame').mockImplementation(() => {
      // For simplicity, just clear the last callback
    })
  })

  afterEach(() => {
    cleanup()
    vi.restoreAllMocks()
  })

  // Helper to advance animation by triggering RAF callbacks
  const advanceAnimation = (timeMs: number) => {
    mockNow += timeMs
    const callbacks = [...rafCallbacks]
    rafCallbacks = []
    callbacks.forEach(cb => cb(mockNow))
  }

  describe('easeOutQuart function', () => {
    // The easeOutQuart function is defined inside the component effect,
    // so we test it indirectly through animation behavior
    it('should produce smooth easing (start slow, end fast approach)', () => {
      // easeOutQuart: 1 - (1 - t)^4
      // At t=0: 1 - 1^4 = 0
      // At t=0.5: 1 - 0.5^4 = 1 - 0.0625 = 0.9375
      // At t=1: 1 - 0^4 = 1

      const easeOutQuart = (t: number): number => {
        return 1 - Math.pow(1 - t, 4)
      }

      expect(easeOutQuart(0)).toBe(0)
      expect(easeOutQuart(0.5)).toBe(0.9375)
      expect(easeOutQuart(1)).toBe(1)

      // Test intermediate values - easing should be fast at start, slow at end
      expect(easeOutQuart(0.25)).toBeCloseTo(0.6836, 4)
      expect(easeOutQuart(0.75)).toBeCloseTo(0.9961, 4)
    })
  })

  describe('AnimatedCounter component', () => {
    it('should render initial value', () => {
      render(<AnimatedCounter value={100} />)
      expect(screen.getByText('100')).toBeInTheDocument()
    })

    it('should render with custom formatter', () => {
      render(<AnimatedCounter value={1000} formatter={(v) => `$${v}`} />)
      // Custom formatter just adds $ prefix without locale formatting
      expect(screen.getByText('$1000')).toBeInTheDocument()
    })

    it('should render with default formatter (toLocaleString)', () => {
      render(<AnimatedCounter value={1000000} />)
      const expected = (1000000).toLocaleString()
      expect(screen.getByText(expected)).toBeInTheDocument()
    })

    it('should apply custom className', () => {
      render(<AnimatedCounter value={50} className="custom-class" />)
      const span = screen.getByText('50')
      expect(span).toHaveClass('custom-class')
    })

    it('should use requestAnimationFrame for animation', () => {
      const { rerender } = render(<AnimatedCounter value={0} />)

      // Change value to trigger animation
      rerender(<AnimatedCounter value={100} />)

      expect(window.requestAnimationFrame).toHaveBeenCalled()
    })

    it('should animate to new value when prop changes', () => {
      const { rerender, container } = render(<AnimatedCounter value={0} duration={1000} />)
      expect(screen.getByText('0')).toBeInTheDocument()

      // Change to new value
      act(() => {
        rerender(<AnimatedCounter value={100} duration={1000} />)
      })

      // Advance animation partway (500ms = 50% through)
      act(() => {
        advanceAnimation(500)
      })

      // With easeOutQuart at 50%, progress should be ~93.75%
      // So value should be around 94 (0 + 100 * 0.9375)
      const span = container.querySelector('span')
      const displayedValue = parseInt(span?.textContent || '0', 10)
      expect(displayedValue).toBeGreaterThan(50) // Should be more than linear 50%
      expect(displayedValue).toBeLessThan(100) // But not yet at 100

      // Complete animation
      act(() => {
        advanceAnimation(500)
      })

      expect(screen.getByText('100')).toBeInTheDocument()
    })

    it('should not animate when value stays the same', () => {
      const { rerender } = render(<AnimatedCounter value={100} />)

      const rafCallCount = (window.requestAnimationFrame as ReturnType<typeof vi.fn>).mock.calls.length

      rerender(<AnimatedCounter value={100} />)

      // RAF should not be called again for same value
      expect((window.requestAnimationFrame as ReturnType<typeof vi.fn>).mock.calls.length).toBe(rafCallCount)
    })

    it('should cancel existing animation when new value arrives', () => {
      const { rerender } = render(<AnimatedCounter value={0} duration={1000} />)

      // Start animation to 50
      act(() => {
        rerender(<AnimatedCounter value={50} duration={1000} />)
      })

      // Partway through, change to 100
      act(() => {
        advanceAnimation(250)
      })

      act(() => {
        rerender(<AnimatedCounter value={100} duration={1000} />)
      })

      expect(window.cancelAnimationFrame).toHaveBeenCalled()
    })

    it('should cancel animation on unmount', () => {
      const { rerender, unmount } = render(<AnimatedCounter value={0} duration={1000} />)

      // Start animation
      act(() => {
        rerender(<AnimatedCounter value={100} duration={1000} />)
      })

      // Unmount while animating
      unmount()

      expect(window.cancelAnimationFrame).toHaveBeenCalled()
    })

    it('should handle counting down', () => {
      const { rerender } = render(<AnimatedCounter value={100} duration={1000} />)
      expect(screen.getByText('100')).toBeInTheDocument()

      // Count down to 0
      act(() => {
        rerender(<AnimatedCounter value={0} duration={1000} />)
      })

      // Complete animation
      act(() => {
        advanceAnimation(1000)
      })

      expect(screen.getByText('0')).toBeInTheDocument()
    })

    it('should use custom duration', () => {
      const { rerender, container } = render(<AnimatedCounter value={0} duration={2000} />)

      act(() => {
        rerender(<AnimatedCounter value={100} duration={2000} />)
      })

      // At 1000ms (50% of 2000ms duration), animation should still be in progress
      act(() => {
        advanceAnimation(1000)
      })

      const span = container.querySelector('span')
      const displayedValue = parseInt(span?.textContent?.replace(/,/g, '') || '0', 10)
      expect(displayedValue).toBeLessThan(100)

      // Complete at 2000ms
      act(() => {
        advanceAnimation(1000)
      })

      expect(screen.getByText('100')).toBeInTheDocument()
    })
  })

  describe('AnimatedFormattedValue component', () => {
    it('should render value with unit', () => {
      render(<AnimatedFormattedValue value="100 KB" />)
      // Initial render shows the parsed value
      expect(screen.getByText(/KB/)).toBeInTheDocument()
    })

    it('should parse and display formatted value correctly', () => {
      const { container } = render(<AnimatedFormattedValue value="969.1 KB" />)
      const span = container.querySelector('span')
      expect(span?.textContent).toContain('KB')
    })

    it('should apply custom className', () => {
      const { container } = render(<AnimatedFormattedValue value="50 MB" className="formatted-class" />)
      const span = container.querySelector('span')
      expect(span).toHaveClass('formatted-class')
    })

    it('should handle values without units', () => {
      const { container } = render(<AnimatedFormattedValue value="123" />)
      const span = container.querySelector('span')
      expect(span?.textContent?.trim()).toContain('123')
    })

    it('should return raw value when pattern does not match', () => {
      render(<AnimatedFormattedValue value="invalid" />)
      expect(screen.getByText('invalid')).toBeInTheDocument()
    })

    it('should animate numeric portion when value changes', () => {
      const { rerender, container } = render(<AnimatedFormattedValue value="0 KB" />)

      act(() => {
        rerender(<AnimatedFormattedValue value="100 KB" />)
      })

      expect(window.requestAnimationFrame).toHaveBeenCalled()

      // Complete animation
      act(() => {
        advanceAnimation(1500)
      })

      const span = container.querySelector('span')
      expect(span?.textContent).toContain('100')
      expect(span?.textContent).toContain('KB')
    })

    it('should preserve unit during animation', () => {
      const { rerender, container } = render(<AnimatedFormattedValue value="0 MB" />)

      act(() => {
        rerender(<AnimatedFormattedValue value="500 MB" />)
      })

      // Partway through animation
      act(() => {
        advanceAnimation(750)
      })

      const span = container.querySelector('span')
      expect(span?.textContent).toContain('MB')
    })

    it('should format large numbers as integers', () => {
      // When animatedNum >= 100, it should show integer
      const { rerender, container } = render(<AnimatedFormattedValue value="0 GB" />)

      act(() => {
        rerender(<AnimatedFormattedValue value="200 GB" />)
      })

      // Complete animation
      act(() => {
        advanceAnimation(1500)
      })

      const span = container.querySelector('span')
      expect(span?.textContent).toContain('200')
      expect(span?.textContent).not.toContain('.')
    })

    it('should format small numbers with one decimal place', () => {
      const { rerender, container } = render(<AnimatedFormattedValue value="0 KB" />)

      act(() => {
        rerender(<AnimatedFormattedValue value="50.5 KB" />)
      })

      // Complete animation
      act(() => {
        advanceAnimation(1500)
      })

      const span = container.querySelector('span')
      expect(span?.textContent).toMatch(/50\.5/)
    })

    it('should not animate when numeric value stays the same', () => {
      // First render will trigger initial animation from 0 to 100
      const { rerender, container } = render(<AnimatedFormattedValue value="100 KB" />)

      // Complete the initial animation
      act(() => {
        advanceAnimation(1500)
      })

      // Now the value should be 100
      const span = container.querySelector('span')
      expect(span?.textContent).toContain('100')

      // Rerender with same value - due to how parsed is computed on each render,
      // the effect runs but the skip condition (startNum === endNum) should prevent animation
      rerender(<AnimatedFormattedValue value="100 KB" />)

      // Complete any potential animation frames
      act(() => {
        advanceAnimation(1500)
      })

      // The value should still be 100 (no change)
      expect(span?.textContent).toContain('100')
    })

    it('should cancel animation on unmount', () => {
      const { rerender, unmount } = render(<AnimatedFormattedValue value="0 KB" />)

      act(() => {
        rerender(<AnimatedFormattedValue value="100 KB" />)
      })

      unmount()

      expect(window.cancelAnimationFrame).toHaveBeenCalled()
    })

    it('should handle various unit formats', () => {
      const testCases = [
        { value: '1.5 GB', unit: 'GB' },
        { value: '256 bytes', unit: 'bytes' },
        { value: '99.9%', unit: '%' },
        { value: '42 items', unit: 'items' },
      ]

      testCases.forEach(({ value, unit }) => {
        cleanup()
        const { container } = render(<AnimatedFormattedValue value={value} />)
        const span = container.querySelector('span')
        expect(span?.textContent).toContain(unit)
      })
    })
  })

  describe('parseFormattedValue (tested through AnimatedFormattedValue)', () => {
    it('should parse integer values', () => {
      const { container } = render(<AnimatedFormattedValue value="100 KB" />)
      const span = container.querySelector('span')
      expect(span?.textContent).toContain('KB')
    })

    it('should parse decimal values', () => {
      const { container } = render(<AnimatedFormattedValue value="3.14 units" />)
      const span = container.querySelector('span')
      expect(span?.textContent).toContain('units')
    })

    it('should handle values with no space before unit', () => {
      const { container } = render(<AnimatedFormattedValue value="100%" />)
      const span = container.querySelector('span')
      expect(span?.textContent).toContain('%')
    })

    it('should return raw value for non-numeric strings', () => {
      render(<AnimatedFormattedValue value="hello world" />)
      expect(screen.getByText('hello world')).toBeInTheDocument()
    })
  })
})
