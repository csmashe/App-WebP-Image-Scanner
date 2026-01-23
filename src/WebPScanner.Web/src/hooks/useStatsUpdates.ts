import { useEffect, useRef, useState, useCallback } from 'react'
import * as signalR from '@microsoft/signalr'
import type { AggregateStats } from '../types/scan'

/** Hub URL for SignalR connection */
const HUB_URL = '/hubs/scanprogress'

/** Reconnect delays in milliseconds */
const RECONNECT_DELAYS = [0, 2000, 5000, 10000, 30000]

/** Initial connection retry delays in milliseconds */
const INITIAL_RETRY_DELAYS = [2000, 5000, 10000, 30000]

/**
 * Custom hook for subscribing to real-time aggregate stats updates.
 * Automatically connects to SignalR and subscribes to stats updates.
 * Retries connection on initial failure.
 */
export function useStatsUpdates(onStatsUpdate: (stats: AggregateStats) => void) {
  const connectionRef = useRef<signalR.HubConnection | null>(null)
  const isConnectingRef = useRef(false)
  const retryTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const retryAttemptRef = useRef(0)
  const isMountedRef = useRef(true)
  const [connected, setConnected] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Use a ref to hold the latest callback to avoid stale closures
  const latestOnStatsUpdateRef = useRef(onStatsUpdate)

  // Keep the ref up to date with the latest callback
  useEffect(() => {
    latestOnStatsUpdateRef.current = onStatsUpdate
  }, [onStatsUpdate])

  // Use a ref to hold the connect function to break circular dependency
  const connectRef = useRef<(() => Promise<void>) | undefined>(undefined)

  const scheduleRetry = useCallback(() => {
    // Don't retry if unmounted or already connected
    if (!isMountedRef.current || connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      return
    }

    const retryIndex = Math.min(retryAttemptRef.current, INITIAL_RETRY_DELAYS.length - 1)
    const delay = INITIAL_RETRY_DELAYS[retryIndex]

    console.log(`Stats connection: scheduling retry ${retryAttemptRef.current + 1} in ${delay}ms`)

    retryTimeoutRef.current = setTimeout(() => {
      if (isMountedRef.current && connectRef.current) {
        retryAttemptRef.current++
        // Reset isConnectingRef so connect() will actually try
        isConnectingRef.current = false
        connectRef.current()
      }
    }, delay)
  }, [])

  const connect = useCallback(async () => {
    // Prevent concurrent connection attempts
    if (isConnectingRef.current || connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      return
    }

    isConnectingRef.current = true
    setError(null)

    try {
      // Build connection with automatic reconnect
      const connection = new signalR.HubConnectionBuilder()
        .withUrl(HUB_URL)
        .withAutomaticReconnect(RECONNECT_DELAYS)
        .configureLogging(signalR.LogLevel.Warning)
        .build()

      // Set up stats update handler (uses ref to avoid stale closure)
      connection.on('StatsUpdate', (data: AggregateStats) => {
        console.log('Received stats update:', data)
        latestOnStatsUpdateRef.current(data)
      })

      // Handle connection state changes
      connection.onreconnecting((err) => {
        console.warn('SignalR stats reconnecting:', err)
        setConnected(false)
      })

      connection.onreconnected(async () => {
        console.log('SignalR stats reconnected')
        setConnected(true)
        // Re-subscribe to stats after reconnection
        try {
          await connection.invoke('SubscribeToStats')
        } catch (err) {
          console.error('Failed to re-subscribe after reconnect:', err)
        }
      })

      connection.onclose((err) => {
        console.log('SignalR stats connection closed:', err)
        setConnected(false)
        connectionRef.current = null
        isConnectingRef.current = false
        // Schedule retry on unexpected close
        if (isMountedRef.current) {
          scheduleRetry()
        }
      })

      // Start connection
      await connection.start()
      connectionRef.current = connection

      // Subscribe to stats updates
      await connection.invoke('SubscribeToStats')
      console.log('Subscribed to stats updates')

      setConnected(true)
      isConnectingRef.current = false
      // Reset retry counter on successful connection
      retryAttemptRef.current = 0
    } catch (err) {
      console.error('SignalR stats connection error:', err)
      setConnected(false)
      setError(err instanceof Error ? err.message : 'Failed to connect')
      connectionRef.current = null
      isConnectingRef.current = false
      // Schedule retry on initial connection failure
      scheduleRetry()
    }
  }, [scheduleRetry])

  // Keep connectRef up to date
  useEffect(() => {
    connectRef.current = connect
  }, [connect])

  const disconnect = useCallback(async () => {
    // Clear any pending retry
    if (retryTimeoutRef.current) {
      clearTimeout(retryTimeoutRef.current)
      retryTimeoutRef.current = null
    }

    const connection = connectionRef.current
    if (!connection) return

    try {
      if (connection.state === signalR.HubConnectionState.Connected) {
        await connection.invoke('UnsubscribeFromStats')
      }
      await connection.stop()
    } catch (err) {
      console.error('Error disconnecting:', err)
    } finally {
      connectionRef.current = null
      isConnectingRef.current = false
      setConnected(false)
    }
  }, [])

  // Connect on mount, disconnect on unmount
  useEffect(() => {
    isMountedRef.current = true
    connect()
    return () => {
      isMountedRef.current = false
      // Clear any pending retry
      if (retryTimeoutRef.current) {
        clearTimeout(retryTimeoutRef.current)
        retryTimeoutRef.current = null
      }
      disconnect()
    }
  }, [connect, disconnect])

  return { connected, error, reconnect: connect }
}
