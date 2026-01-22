import { create } from 'zustand'

export type ToastType = 'success' | 'error' | 'warning' | 'info'

export interface Toast {
  id: string
  type: ToastType
  title: string
  message?: string
  duration?: number
}

interface ToastState {
  toasts: Toast[]
}

interface ToastActions {
  addToast: (toast: Omit<Toast, 'id'>) => void
  removeToast: (id: string) => void
  clearToasts: () => void
}

let toastId = 0

export const useToastStore = create<ToastState & ToastActions>((set) => ({
  toasts: [],

  addToast: (toast) => {
    const id = `toast-${++toastId}`
    const duration = toast.duration ?? (toast.type === 'error' ? 8000 : 5000)

    set((state) => ({
      toasts: [...state.toasts, { ...toast, id }],
    }))

    // Auto-remove toast after duration
    if (duration > 0) {
      setTimeout(() => {
        set((state) => ({
          toasts: state.toasts.filter((t) => t.id !== id),
        }))
      }, duration)
    }
  },

  removeToast: (id) => {
    set((state) => ({
      toasts: state.toasts.filter((t) => t.id !== id),
    }))
  },

  clearToasts: () => {
    set({ toasts: [] })
  },
}))

/** Helper functions for common toast types */
export const toast = {
  success: (title: string, message?: string) => {
    useToastStore.getState().addToast({ type: 'success', title, message })
  },
  error: (title: string, message?: string) => {
    useToastStore.getState().addToast({ type: 'error', title, message })
  },
  warning: (title: string, message?: string) => {
    useToastStore.getState().addToast({ type: 'warning', title, message })
  },
  info: (title: string, message?: string) => {
    useToastStore.getState().addToast({ type: 'info', title, message })
  },
}
