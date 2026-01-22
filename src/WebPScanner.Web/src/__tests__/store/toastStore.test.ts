import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest'
import { useToastStore, toast, type ToastType } from '../../store/toastStore'

describe('toastStore', () => {
  // Reset store state before each test
  beforeEach(() => {
    useToastStore.getState().clearToasts()
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  describe('initial state', () => {
    it('should have empty toasts array initially', () => {
      const state = useToastStore.getState()
      expect(state.toasts).toEqual([])
    })
  })

  describe('addToast()', () => {
    it('should create toast with unique ID', () => {
      const store = useToastStore.getState()

      store.addToast({ type: 'success', title: 'Test Toast' })

      const state = useToastStore.getState()
      expect(state.toasts).toHaveLength(1)
      expect(state.toasts[0].id).toMatch(/^toast-\d+$/)
      expect(state.toasts[0].title).toBe('Test Toast')
      expect(state.toasts[0].type).toBe('success')
    })

    it('should create toasts with incrementing IDs', () => {
      const store = useToastStore.getState()

      store.addToast({ type: 'success', title: 'Toast 1' })
      store.addToast({ type: 'error', title: 'Toast 2' })
      store.addToast({ type: 'info', title: 'Toast 3' })

      const state = useToastStore.getState()
      expect(state.toasts).toHaveLength(3)

      // IDs should be unique and incrementing
      const ids = state.toasts.map((t) => t.id)
      expect(new Set(ids).size).toBe(3) // All unique
    })

    it('should add toast with success type', () => {
      const store = useToastStore.getState()

      store.addToast({ type: 'success', title: 'Success!', message: 'Operation completed' })

      const state = useToastStore.getState()
      expect(state.toasts[0].type).toBe('success')
      expect(state.toasts[0].title).toBe('Success!')
      expect(state.toasts[0].message).toBe('Operation completed')
    })

    it('should add toast with error type', () => {
      const store = useToastStore.getState()

      store.addToast({ type: 'error', title: 'Error!', message: 'Something went wrong' })

      const state = useToastStore.getState()
      expect(state.toasts[0].type).toBe('error')
      expect(state.toasts[0].title).toBe('Error!')
      expect(state.toasts[0].message).toBe('Something went wrong')
    })

    it('should add toast with info type', () => {
      const store = useToastStore.getState()

      store.addToast({ type: 'info', title: 'Information' })

      const state = useToastStore.getState()
      expect(state.toasts[0].type).toBe('info')
    })

    it('should add toast with warning type', () => {
      const store = useToastStore.getState()

      store.addToast({ type: 'warning', title: 'Warning!' })

      const state = useToastStore.getState()
      expect(state.toasts[0].type).toBe('warning')
    })

    it('should add toast without optional message', () => {
      const store = useToastStore.getState()

      store.addToast({ type: 'success', title: 'Just a title' })

      const state = useToastStore.getState()
      expect(state.toasts[0].message).toBeUndefined()
    })
  })

  describe('removeToast()', () => {
    it('should remove correct toast by ID', () => {
      const store = useToastStore.getState()

      store.addToast({ type: 'success', title: 'Toast 1' })
      store.addToast({ type: 'error', title: 'Toast 2' })
      store.addToast({ type: 'info', title: 'Toast 3' })

      const toasts = useToastStore.getState().toasts
      const toastToRemove = toasts[1] // Remove the middle one

      store.removeToast(toastToRemove.id)

      const state = useToastStore.getState()
      expect(state.toasts).toHaveLength(2)
      expect(state.toasts.find((t) => t.id === toastToRemove.id)).toBeUndefined()
      expect(state.toasts[0].title).toBe('Toast 1')
      expect(state.toasts[1].title).toBe('Toast 3')
    })

    it('should not affect other toasts when removing by ID', () => {
      const store = useToastStore.getState()

      store.addToast({ type: 'success', title: 'First' })
      store.addToast({ type: 'error', title: 'Second' })

      const firstToast = useToastStore.getState().toasts[0]
      store.removeToast(firstToast.id)

      const state = useToastStore.getState()
      expect(state.toasts).toHaveLength(1)
      expect(state.toasts[0].title).toBe('Second')
    })

    it('should handle removing non-existent ID gracefully', () => {
      const store = useToastStore.getState()

      store.addToast({ type: 'success', title: 'Toast' })

      store.removeToast('non-existent-id')

      const state = useToastStore.getState()
      expect(state.toasts).toHaveLength(1)
    })
  })

  describe('clearToasts()', () => {
    it('should remove all toasts', () => {
      const store = useToastStore.getState()

      store.addToast({ type: 'success', title: 'Toast 1' })
      store.addToast({ type: 'error', title: 'Toast 2' })
      store.addToast({ type: 'info', title: 'Toast 3' })

      expect(useToastStore.getState().toasts).toHaveLength(3)

      store.clearToasts()

      expect(useToastStore.getState().toasts).toHaveLength(0)
    })

    it('should handle clearing when already empty', () => {
      const store = useToastStore.getState()

      expect(useToastStore.getState().toasts).toHaveLength(0)

      store.clearToasts()

      expect(useToastStore.getState().toasts).toHaveLength(0)
    })
  })

  describe('auto-removal timer', () => {
    it('should auto-remove error toast after 8 seconds', () => {
      const store = useToastStore.getState()

      store.addToast({ type: 'error', title: 'Error toast' })

      expect(useToastStore.getState().toasts).toHaveLength(1)

      // Advance time by 7.9 seconds - should still be there
      vi.advanceTimersByTime(7900)
      expect(useToastStore.getState().toasts).toHaveLength(1)

      // Advance time to 8 seconds - should be removed
      vi.advanceTimersByTime(100)
      expect(useToastStore.getState().toasts).toHaveLength(0)
    })

    it('should auto-remove success toast after 5 seconds', () => {
      const store = useToastStore.getState()

      store.addToast({ type: 'success', title: 'Success toast' })

      expect(useToastStore.getState().toasts).toHaveLength(1)

      // Advance time by 4.9 seconds - should still be there
      vi.advanceTimersByTime(4900)
      expect(useToastStore.getState().toasts).toHaveLength(1)

      // Advance time to 5 seconds - should be removed
      vi.advanceTimersByTime(100)
      expect(useToastStore.getState().toasts).toHaveLength(0)
    })

    it('should auto-remove info toast after 5 seconds', () => {
      const store = useToastStore.getState()

      store.addToast({ type: 'info', title: 'Info toast' })

      vi.advanceTimersByTime(5000)
      expect(useToastStore.getState().toasts).toHaveLength(0)
    })

    it('should auto-remove warning toast after 5 seconds', () => {
      const store = useToastStore.getState()

      store.addToast({ type: 'warning', title: 'Warning toast' })

      vi.advanceTimersByTime(5000)
      expect(useToastStore.getState().toasts).toHaveLength(0)
    })

    it('should use custom duration when provided', () => {
      const store = useToastStore.getState()

      store.addToast({ type: 'success', title: 'Custom duration', duration: 2000 })

      expect(useToastStore.getState().toasts).toHaveLength(1)

      vi.advanceTimersByTime(1900)
      expect(useToastStore.getState().toasts).toHaveLength(1)

      vi.advanceTimersByTime(100)
      expect(useToastStore.getState().toasts).toHaveLength(0)
    })

    it('should not auto-remove when duration is 0', () => {
      const store = useToastStore.getState()

      store.addToast({ type: 'success', title: 'Persistent toast', duration: 0 })

      vi.advanceTimersByTime(60000) // 1 minute
      expect(useToastStore.getState().toasts).toHaveLength(1)
    })

    it('should handle multiple toasts with different durations', () => {
      const store = useToastStore.getState()

      store.addToast({ type: 'success', title: 'Success (5s)' }) // 5s default
      store.addToast({ type: 'error', title: 'Error (8s)' }) // 8s default
      store.addToast({ type: 'info', title: 'Custom (2s)', duration: 2000 })

      expect(useToastStore.getState().toasts).toHaveLength(3)

      // After 2s: custom toast removed
      vi.advanceTimersByTime(2000)
      expect(useToastStore.getState().toasts).toHaveLength(2)
      expect(useToastStore.getState().toasts.find((t) => t.title === 'Custom (2s)')).toBeUndefined()

      // After 5s total: success toast removed
      vi.advanceTimersByTime(3000)
      expect(useToastStore.getState().toasts).toHaveLength(1)
      expect(useToastStore.getState().toasts.find((t) => t.title === 'Success (5s)')).toBeUndefined()

      // After 8s total: error toast removed
      vi.advanceTimersByTime(3000)
      expect(useToastStore.getState().toasts).toHaveLength(0)
    })
  })

  describe('multiple toasts', () => {
    it('should allow multiple toasts to exist simultaneously', () => {
      const store = useToastStore.getState()

      store.addToast({ type: 'success', title: 'Toast 1' })
      store.addToast({ type: 'error', title: 'Toast 2' })
      store.addToast({ type: 'info', title: 'Toast 3' })
      store.addToast({ type: 'warning', title: 'Toast 4' })

      const state = useToastStore.getState()
      expect(state.toasts).toHaveLength(4)
    })

    it('should maintain toast order (FIFO)', () => {
      const store = useToastStore.getState()

      store.addToast({ type: 'success', title: 'First' })
      store.addToast({ type: 'error', title: 'Second' })
      store.addToast({ type: 'info', title: 'Third' })

      const state = useToastStore.getState()
      expect(state.toasts[0].title).toBe('First')
      expect(state.toasts[1].title).toBe('Second')
      expect(state.toasts[2].title).toBe('Third')
    })
  })

  describe('toast helper functions', () => {
    it('toast.success should add success toast', () => {
      toast.success('Success Title', 'Success message')

      const state = useToastStore.getState()
      expect(state.toasts).toHaveLength(1)
      expect(state.toasts[0].type).toBe('success')
      expect(state.toasts[0].title).toBe('Success Title')
      expect(state.toasts[0].message).toBe('Success message')
    })

    it('toast.error should add error toast', () => {
      toast.error('Error Title', 'Error message')

      const state = useToastStore.getState()
      expect(state.toasts).toHaveLength(1)
      expect(state.toasts[0].type).toBe('error')
      expect(state.toasts[0].title).toBe('Error Title')
      expect(state.toasts[0].message).toBe('Error message')
    })

    it('toast.warning should add warning toast', () => {
      toast.warning('Warning Title', 'Warning message')

      const state = useToastStore.getState()
      expect(state.toasts).toHaveLength(1)
      expect(state.toasts[0].type).toBe('warning')
      expect(state.toasts[0].title).toBe('Warning Title')
      expect(state.toasts[0].message).toBe('Warning message')
    })

    it('toast.info should add info toast', () => {
      toast.info('Info Title', 'Info message')

      const state = useToastStore.getState()
      expect(state.toasts).toHaveLength(1)
      expect(state.toasts[0].type).toBe('info')
      expect(state.toasts[0].title).toBe('Info Title')
      expect(state.toasts[0].message).toBe('Info message')
    })

    it('toast helpers should work without message', () => {
      toast.success('Just title')

      const state = useToastStore.getState()
      expect(state.toasts[0].title).toBe('Just title')
      expect(state.toasts[0].message).toBeUndefined()
    })
  })

  describe('ToastType', () => {
    it('should support all defined toast types', () => {
      const types: ToastType[] = ['success', 'error', 'warning', 'info']
      const store = useToastStore.getState()

      types.forEach((type) => {
        store.addToast({ type, title: `${type} toast` })
      })

      const state = useToastStore.getState()
      expect(state.toasts).toHaveLength(4)
      types.forEach((type, index) => {
        expect(state.toasts[index].type).toBe(type)
      })
    })
  })
})
