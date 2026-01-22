import { useEffect, useRef, useState, useCallback } from 'react'
import * as signalR from '@microsoft/signalr'
import type { AggregateStats } from '../types/scan'

/** Hub URL for SignalR connection */
const HUB_URL = '/hubs/scanprogress'

/** Reconnect delays in milliseconds */
const RECONNECT_DELAYS = [0, 2000, 5000, 10000, 30000]

/**
 * Custom hook for subscribing to real-time aggregate stats updates.
 * Automatically connects to SignalR and subscribes to stats updates.
 */
export function useStatsUpdates(onStatsUpdate: (stats: AggregateStats) => void) {
  const connectionRef = useRef<signalR.HubConnection | null>(null)
  const isConnectingRef = useRef(false)
  const [connected, setConnected] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Use a ref to hold the latest callback to avoid stale closures
  const latestOnStatsUpdateRef = useRef(onStatsUpdate)

  // Keep the ref up to date with the latest callback
  useEffect(() => {
    latestOnStatsUpdateRef.current = onStatsUpdate
  }, [onStatsUpdate])

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
        console.warn('SignalR reconnecting:', err)
        setConnected(false)
      })

      connection.onreconnected(async () => {
        console.log('SignalR reconnected')
        setConnected(true)
        // Re-subscribe to stats after reconnection
        try {
          await connection.invoke('SubscribeToStats')
        } catch (err) {
          console.error('Failed to re-subscribe after reconnect:', err)
        }
      })

      connection.onclose((err) => {
        console.log('SignalR connection closed:', err)
        setConnected(false)
        connectionRef.current = null
        isConnectingRef.current = false
      })

      // Start connection
      await connection.start()
      connectionRef.current = connection

      // Subscribe to stats updates
      await connection.invoke('SubscribeToStats')
      console.log('Subscribed to stats updates')

      setConnected(true)
      isConnectingRef.current = false
    } catch (err) {
      console.error('SignalR connection error:', err)
      setConnected(false)
      setError(err instanceof Error ? err.message : 'Failed to connect')
      connectionRef.current = null
      isConnectingRef.current = false
    }
  }, []) // No dependencies - uses refs for mutable state

  const disconnect = useCallback(async () => {
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
    connect()
    return () => {
      disconnect()
    }
  }, [connect, disconnect])

  return { connected, error, reconnect: connect }
}
