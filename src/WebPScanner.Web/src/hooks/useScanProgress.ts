import { useEffect, useRef, useCallback } from 'react'
import * as signalR from '@microsoft/signalr'
import { useScanStore } from '../store/scanStore'
import type {
  QueuePositionUpdate,
  ScanStarted,
  PageProgress,
  ImageFound,
  ScanComplete,
  ScanFailed,
  ScanProgressSnapshot,
} from '../types/scan'

/** Hub URL for SignalR connection */
const HUB_URL = '/hubs/scanprogress'

/** Reconnect delays in milliseconds */
const RECONNECT_DELAYS = [0, 2000, 5000, 10000, 30000]

/**
 * Custom hook for managing SignalR connection and scan progress updates.
 * Automatically connects when a scanId is set in the store and disconnects
 * when the scan completes or fails.
 */
export function useScanProgress() {
  const connectionRef = useRef<signalR.HubConnection | null>(null)
  const isConnectingRef = useRef(false)
  const currentScanIdRef = useRef<string | null>(null)

  // Get state and actions from store
  const scanId = useScanStore((state) => state.scanId)
  const viewState = useScanStore((state) => state.viewState)

  const disconnectFromHub = useCallback(async () => {
    const connection = connectionRef.current
    if (!connection) return

    try {
      if (currentScanIdRef.current && connection.state === signalR.HubConnectionState.Connected) {
        await connection.invoke('UnsubscribeFromScan', currentScanIdRef.current)
      }
      await connection.stop()
    } catch (error) {
      console.error('Error disconnecting:', error)
    } finally {
      connectionRef.current = null
      currentScanIdRef.current = null
      isConnectingRef.current = false
      useScanStore.getState().setConnected(false)
    }
  }, [])

  const connectToHub = useCallback(async (targetScanId: string) => {
    // Prevent concurrent connection attempts
    if (isConnectingRef.current) {
      return
    }

    // Don't reconnect if already connected to the same scan
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected &&
        currentScanIdRef.current === targetScanId) {
      return
    }

    // Disconnect existing connection if connecting to different scan
    if (connectionRef.current) {
      await disconnectFromHub()
    }

    isConnectingRef.current = true
    currentScanIdRef.current = targetScanId

    // Clear error first, then set connecting (setConnectionError resets isConnecting to false)
    useScanStore.getState().setConnectionError(null)
    useScanStore.getState().setConnecting(true)

    // Declare connection outside try block so it's accessible in catch
    let connection: signalR.HubConnection | null = null
    let connectionStarted = false

    try {
      // Build connection with automatic reconnect
      connection = new signalR.HubConnectionBuilder()
        .withUrl(HUB_URL)
        .withAutomaticReconnect(RECONNECT_DELAYS)
        .configureLogging(signalR.LogLevel.Warning)
        .build()

      // Set up event handlers using store actions directly
      connection.on('QueuePositionUpdate', (data: QueuePositionUpdate) => {
        useScanStore.getState().handleQueuePositionUpdate(data)
      })

      connection.on('ScanStarted', (data: ScanStarted) => {
        useScanStore.getState().handleScanStarted(data)
      })

      connection.on('PageProgress', (data: PageProgress) => {
        useScanStore.getState().handlePageProgress(data)
      })

      connection.on('ImageFound', (data: ImageFound) => {
        useScanStore.getState().handleImageFound(data)
      })

      connection.on('ScanComplete', (data: ScanComplete) => {
        useScanStore.getState().handleScanComplete(data)
      })

      connection.on('ScanFailed', (data: ScanFailed) => {
        useScanStore.getState().handleScanFailed(data)
      })

      // Handle connection state changes
      connection.onreconnecting((error) => {
        console.warn('SignalR reconnecting:', error)
        useScanStore.getState().setConnected(false)
        useScanStore.getState().setConnecting(true)
      })

      connection.onreconnected(async () => {
        console.log('SignalR reconnected')
        useScanStore.getState().setConnected(true)
        useScanStore.getState().setConnecting(false)
        // Re-subscribe to the scan after reconnection
        if (currentScanIdRef.current && connection) {
          try {
            await connection.invoke('SubscribeToScan', currentScanIdRef.current)

            // Fetch current progress state to sync with server
            const snapshot = await connection.invoke<ScanProgressSnapshot | null>(
              'GetCurrentProgress',
              currentScanIdRef.current
            )
            if (snapshot) {
              console.log('Syncing state from server snapshot:', snapshot)
              useScanStore.getState().syncFromSnapshot(snapshot)
            }
          } catch (err) {
            console.error('Failed to re-subscribe/sync after reconnect:', err)
          }
        }
      })

      connection.onclose((error) => {
        console.log('SignalR connection closed:', error)
        useScanStore.getState().setConnected(false)
        useScanStore.getState().setConnecting(false)
        connectionRef.current = null
        isConnectingRef.current = false
      })

      // Start connection
      await connection.start()
      connectionStarted = true
      connectionRef.current = connection

      // Subscribe to scan updates
      await connection.invoke('SubscribeToScan', targetScanId)
      console.log(`Subscribed to scan: ${targetScanId}`)

      useScanStore.getState().setConnected(true)
      useScanStore.getState().setConnecting(false)
      isConnectingRef.current = false
    } catch (error) {
      console.error('SignalR connection error:', error)

      // Stop the connection if it was started to prevent orphaned connections
      if (connection && connectionStarted) {
        try {
          await connection.stop()
        } catch (stopError) {
          console.error('Error stopping orphaned connection:', stopError)
        }
      }

      useScanStore.getState().setConnected(false)
      useScanStore.getState().setConnecting(false)
      useScanStore.getState().setConnectionError(
        error instanceof Error ? error.message : 'Failed to connect to progress updates'
      )
      connectionRef.current = null
      currentScanIdRef.current = null
      isConnectingRef.current = false
    }
  }, [disconnectFromHub])

  // Connect when scanId is set and view state is active
  useEffect(() => {
    const shouldConnect = scanId && (viewState === 'queued' || viewState === 'scanning')
    const shouldDisconnect = viewState === 'completed' || viewState === 'failed' || viewState === 'idle'

    if (shouldConnect && scanId !== currentScanIdRef.current) {
      // Connect to new scan
      connectToHub(scanId)
    } else if (shouldDisconnect && connectionRef.current) {
      // Disconnect when scan ends (with delay to receive final messages)
      const timeout = setTimeout(() => {
        disconnectFromHub()
      }, 2000)
      return () => clearTimeout(timeout)
    }

    // Cleanup on unmount only
    return () => {
      // Only disconnect on actual unmount, not on dependency changes
    }
  }, [scanId, viewState, connectToHub, disconnectFromHub])

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      disconnectFromHub()
    }
  }, [disconnectFromHub])

  return {
    connect: connectToHub,
    disconnect: disconnectFromHub,
  }
}
