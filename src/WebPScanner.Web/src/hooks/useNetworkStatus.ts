import { useState, useEffect, useCallback } from 'react'
import { toast } from '../store/toastStore'

interface NetworkStatus {
  isOnline: boolean
  wasOffline: boolean
}

/**
 * Custom hook for monitoring network connectivity.
 * Shows toast notifications when connection is lost or restored.
 */
export function useNetworkStatus(): NetworkStatus {
  const [isOnline, setIsOnline] = useState(
    typeof navigator !== 'undefined' ? navigator.onLine : true
  )
  const [wasOffline, setWasOffline] = useState(false)

  const handleOnline = useCallback(() => {
    setIsOnline(true)
    if (wasOffline) {
      toast.success('Connection restored', 'You are back online.')
    }
  }, [wasOffline])

  const handleOffline = useCallback(() => {
    setIsOnline(false)
    setWasOffline(true)
    toast.error('Connection lost', 'Please check your internet connection.')
  }, [])

  useEffect(() => {
    window.addEventListener('online', handleOnline)
    window.addEventListener('offline', handleOffline)

    return () => {
      window.removeEventListener('online', handleOnline)
      window.removeEventListener('offline', handleOffline)
    }
  }, [handleOnline, handleOffline])

  return { isOnline, wasOffline }
}
