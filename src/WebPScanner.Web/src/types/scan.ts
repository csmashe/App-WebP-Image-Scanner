/**
 * TypeScript types matching the backend DTOs for SignalR communication.
 */

/** Status of a scan job */
export type ScanStatus = 'Queued' | 'Processing' | 'Completed' | 'Failed'

/** Response from POST /api/scan */
export interface ScanResponse {
  scanId: string
  queuePosition: number
  message: string
  convertToWebP: boolean
}

/** Queue position update from SignalR */
export interface QueuePositionUpdate {
  scanId: string
  position: number
  totalInQueue: number
}

/** Scan started notification from SignalR */
export interface ScanStarted {
  scanId: string
  targetUrl: string
  startedAt: string
}

/** Page progress notification from SignalR */
export interface PageProgress {
  scanId: string
  currentUrl: string
  pagesScanned: number
  pagesDiscovered: number
  progressPercent: number
}

/** Image found notification from SignalR */
export interface ImageFound {
  scanId: string
  imageUrl: string
  mimeType: string
  size: number
  isNonWebP: boolean
  totalNonWebPCount: number
  pageUrl: string
}

/** Scan complete notification from SignalR */
export interface ScanComplete {
  scanId: string
  totalPagesScanned: number
  totalImagesFound: number
  nonWebPImagesCount: number
  duration: string
  completedAt: string
  reachedPageLimit: boolean
}

/** Scan failed notification from SignalR */
export interface ScanFailed {
  scanId: string
  errorMessage: string
  failedAt: string
}

/** Category statistics */
export interface CategoryStat {
  category: string
  count: number
  totalSavingsBytes: number
  savingsPercent: number
}

/** Image type statistics */
export interface ImageTypeStat {
  mimeType: string
  displayName: string
  count: number
  totalSizeBytes: number
  potentialSavingsBytes: number
  savingsPercent: number
}

/** Aggregate statistics from all scans */
export interface AggregateStats {
  totalScans: number
  totalPagesCrawled: number
  totalImagesFound: number
  totalOriginalSizeBytes: number
  totalEstimatedWebPSizeBytes: number
  totalSavingsBytes: number
  totalSavingsFormatted: string
  averageSavingsPercent: number
  imageTypeBreakdown: ImageTypeStat[]
  topCategories: CategoryStat[]
}

/** Snapshot of current scan progress, returned by GetCurrentProgress on reconnect */
export interface ScanProgressSnapshot {
  status: string
  queuePosition: number
  pagesScanned: number
  pagesDiscovered: number
  nonWebPImagesCount: number
  progressPercent: number
  currentUrl: string | null
  errorMessage: string | null
}
