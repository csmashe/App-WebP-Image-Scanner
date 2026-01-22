import { create } from 'zustand'
import type {
  QueuePositionUpdate,
  ScanStarted,
  PageProgress,
  ImageFound,
  ScanComplete,
  ScanFailed,
  ScanProgressSnapshot,
} from '../types/scan'

/** Current state of the scan progress UI */
export type ScanViewState =
  | 'idle'           // No scan in progress, show form
  | 'queued'         // Waiting in queue
  | 'scanning'       // Actively scanning
  | 'completed'      // Scan completed successfully
  | 'failed'         // Scan failed

/** Store state for scan progress tracking */
export interface ScanState {
  // Current view state
  viewState: ScanViewState

  // Scan identification
  scanId: string | null
  targetUrl: string | null
  email: string | null
  convertToWebP: boolean

  // Queue state
  queuePosition: number
  totalInQueue: number

  // Scanning progress
  currentUrl: string | null
  pagesScanned: number
  pagesDiscovered: number
  progressPercent: number

  // Image tracking
  totalImagesFound: number
  nonWebPImagesCount: number

  // Completion state
  duration: string | null
  completedAt: string | null
  reachedPageLimit: boolean

  // Error state
  errorMessage: string | null

  // Connection state
  isConnected: boolean
  isConnecting: boolean
  connectionError: string | null
}

/** Store actions for scan progress tracking */
export interface ScanActions {
  // Initialization
  startScan: (scanId: string, targetUrl: string, email: string, queuePosition: number, convertToWebP: boolean) => void
  reset: () => void

  // Connection state
  setConnected: (connected: boolean) => void
  setConnecting: (connecting: boolean) => void
  setConnectionError: (error: string | null) => void

  // SignalR event handlers
  handleQueuePositionUpdate: (data: QueuePositionUpdate) => void
  handleScanStarted: (data: ScanStarted) => void
  handlePageProgress: (data: PageProgress) => void
  handleImageFound: (data: ImageFound) => void
  handleScanComplete: (data: ScanComplete) => void
  handleScanFailed: (data: ScanFailed) => void

  // Sync from server state (used on reconnect)
  syncFromSnapshot: (snapshot: ScanProgressSnapshot) => void
}

const initialState: ScanState = {
  viewState: 'idle',
  scanId: null,
  targetUrl: null,
  email: null,
  convertToWebP: false,
  queuePosition: 0,
  totalInQueue: 0,
  currentUrl: null,
  pagesScanned: 0,
  pagesDiscovered: 0,
  progressPercent: 0,
  totalImagesFound: 0,
  nonWebPImagesCount: 0,
  duration: null,
  completedAt: null,
  reachedPageLimit: false,
  errorMessage: null,
  isConnected: false,
  isConnecting: false,
  connectionError: null,
}

export const useScanStore = create<ScanState & ScanActions>((set, get) => ({
  ...initialState,

  startScan: (scanId, targetUrl, email, queuePosition, convertToWebP) => {
    set({
      ...initialState,
      viewState: 'queued',
      scanId,
      targetUrl,
      email,
      convertToWebP,
      queuePosition,
      totalInQueue: queuePosition, // Initially assume we're at the end
    })
  },

  reset: () => {
    set(initialState)
  },

  setConnected: (connected) => {
    set({ isConnected: connected, connectionError: connected ? null : get().connectionError })
  },

  setConnecting: (connecting) => {
    set({ isConnecting: connecting })
  },

  setConnectionError: (error) => {
    set({ connectionError: error, isConnecting: false })
  },

  handleQueuePositionUpdate: (data) => {
    const { scanId } = get()
    if (data.scanId !== scanId) return

    set({
      queuePosition: data.position,
      totalInQueue: data.totalInQueue,
    })
  },

  handleScanStarted: (data) => {
    const { scanId } = get()
    if (data.scanId !== scanId) return

    set({
      viewState: 'scanning',
      targetUrl: data.targetUrl,
      queuePosition: 0,
    })
  },

  handlePageProgress: (data) => {
    const { scanId } = get()
    if (data.scanId !== scanId) return

    set({
      currentUrl: data.currentUrl,
      pagesScanned: data.pagesScanned,
      pagesDiscovered: data.pagesDiscovered,
      progressPercent: data.progressPercent,
    })
  },

  handleImageFound: (data) => {
    const { scanId } = get()
    if (data.scanId !== scanId) return

    set((state) => ({
      totalImagesFound: state.totalImagesFound + 1,
      nonWebPImagesCount: data.isNonWebP ? data.totalNonWebPCount : state.nonWebPImagesCount,
    }))
  },

  handleScanComplete: (data) => {
    const { scanId } = get()
    if (data.scanId !== scanId) return

    set({
      viewState: 'completed',
      pagesScanned: data.totalPagesScanned,
      totalImagesFound: data.totalImagesFound,
      nonWebPImagesCount: data.nonWebPImagesCount,
      duration: data.duration,
      completedAt: data.completedAt,
      reachedPageLimit: data.reachedPageLimit,
      currentUrl: null,
      progressPercent: 100,
    })
  },

  handleScanFailed: (data) => {
    const { scanId } = get()
    if (data.scanId !== scanId) return

    set({
      viewState: 'failed',
      errorMessage: data.errorMessage,
    })
  },

  syncFromSnapshot: (snapshot) => {
    // Map status string to view state
    const mapStatusToViewState = (status: string): ScanViewState => {
      switch (status) {
        case 'Queued':
          return 'queued'
        case 'Processing':
          return 'scanning'
        case 'Completed':
          return 'completed'
        case 'Failed':
          return 'failed'
        default:
          return 'idle'
      }
    }

    set({
      viewState: mapStatusToViewState(snapshot.status),
      queuePosition: snapshot.queuePosition,
      pagesScanned: snapshot.pagesScanned,
      pagesDiscovered: snapshot.pagesDiscovered,
      nonWebPImagesCount: snapshot.nonWebPImagesCount,
      progressPercent: snapshot.progressPercent,
      currentUrl: snapshot.currentUrl,
      errorMessage: snapshot.errorMessage,
    })
  },
}))
