import { describe, it, expect, beforeEach } from 'vitest'
import { useScanStore } from '../../store/scanStore'
import type {
  QueuePositionUpdate,
  ScanStarted,
  PageProgress,
  ImageFound,
  ScanComplete,
  ScanFailed,
  ScanProgressSnapshot,
} from '../../types/scan'

describe('scanStore', () => {
  // Reset store state before each test
  beforeEach(() => {
    useScanStore.getState().reset()
  })

  describe('initial state', () => {
    it('should have correct initial state values', () => {
      const state = useScanStore.getState()

      expect(state.viewState).toBe('idle')
      expect(state.scanId).toBeNull()
      expect(state.targetUrl).toBeNull()
      expect(state.email).toBeNull()
      expect(state.convertToWebP).toBe(false)
      expect(state.queuePosition).toBe(0)
      expect(state.totalInQueue).toBe(0)
      expect(state.currentUrl).toBeNull()
      expect(state.pagesScanned).toBe(0)
      expect(state.pagesDiscovered).toBe(0)
      expect(state.progressPercent).toBe(0)
      expect(state.totalImagesFound).toBe(0)
      expect(state.nonWebPImagesCount).toBe(0)
      expect(state.duration).toBeNull()
      expect(state.completedAt).toBeNull()
      expect(state.reachedPageLimit).toBe(false)
      expect(state.errorMessage).toBeNull()
      expect(state.isConnected).toBe(false)
      expect(state.isConnecting).toBe(false)
      expect(state.connectionError).toBeNull()
    })
  })

  describe('startScan()', () => {
    it('should set correct initial scanning state', () => {
      const store = useScanStore.getState()

      store.startScan('scan-123', 'https://example.com', 'test@example.com', 3, true)

      const state = useScanStore.getState()
      expect(state.viewState).toBe('queued')
      expect(state.scanId).toBe('scan-123')
      expect(state.targetUrl).toBe('https://example.com')
      expect(state.email).toBe('test@example.com')
      expect(state.queuePosition).toBe(3)
      expect(state.totalInQueue).toBe(3) // Initially assume at end of queue
      expect(state.convertToWebP).toBe(true)
    })

    it('should reset other state values when starting scan', () => {
      const store = useScanStore.getState()

      // Set some state values first
      store.startScan('old-scan', 'https://old.com', 'old@test.com', 1, false)
      store.handlePageProgress({
        scanId: 'old-scan',
        currentUrl: 'https://old.com/page',
        pagesScanned: 5,
        pagesDiscovered: 10,
        progressPercent: 50,
      })

      // Start a new scan
      store.startScan('new-scan', 'https://new.com', 'new@test.com', 2, true)

      const state = useScanStore.getState()
      expect(state.scanId).toBe('new-scan')
      expect(state.pagesScanned).toBe(0)
      expect(state.pagesDiscovered).toBe(0)
      expect(state.progressPercent).toBe(0)
      expect(state.currentUrl).toBeNull()
    })

    it('should set convertToWebP to false when not requested', () => {
      const store = useScanStore.getState()

      store.startScan('scan-456', 'https://example.com', 'test@example.com', 1, false)

      const state = useScanStore.getState()
      expect(state.convertToWebP).toBe(false)
    })
  })

  describe('reset()', () => {
    it('should return store to initial state', () => {
      const store = useScanStore.getState()

      // Set up some state
      store.startScan('scan-123', 'https://example.com', 'test@example.com', 3, true)
      store.handleScanStarted({
        scanId: 'scan-123',
        targetUrl: 'https://example.com',
        startedAt: '2024-01-01T00:00:00Z',
      })
      store.setConnected(true)
      store.setConnectionError('test error')

      // Reset
      store.reset()

      const state = useScanStore.getState()
      expect(state.viewState).toBe('idle')
      expect(state.scanId).toBeNull()
      expect(state.targetUrl).toBeNull()
      expect(state.email).toBeNull()
      expect(state.isConnected).toBe(false)
      expect(state.connectionError).toBeNull()
    })
  })

  describe('connection state actions', () => {
    it('setConnected(true) should update connection state and clear error', () => {
      const store = useScanStore.getState()
      store.setConnectionError('connection failed')

      store.setConnected(true)

      const state = useScanStore.getState()
      expect(state.isConnected).toBe(true)
      expect(state.connectionError).toBeNull()
    })

    it('setConnected(false) should preserve existing error', () => {
      const store = useScanStore.getState()
      store.setConnectionError('connection failed')

      store.setConnected(false)

      const state = useScanStore.getState()
      expect(state.isConnected).toBe(false)
      expect(state.connectionError).toBe('connection failed')
    })

    it('setConnecting() should update connecting state', () => {
      const store = useScanStore.getState()

      store.setConnecting(true)
      expect(useScanStore.getState().isConnecting).toBe(true)

      store.setConnecting(false)
      expect(useScanStore.getState().isConnecting).toBe(false)
    })

    it('setConnectionError() should update error and clear connecting state', () => {
      const store = useScanStore.getState()
      store.setConnecting(true)

      store.setConnectionError('Network error')

      const state = useScanStore.getState()
      expect(state.connectionError).toBe('Network error')
      expect(state.isConnecting).toBe(false)
    })

    it('setConnectionError(null) should clear the error', () => {
      const store = useScanStore.getState()
      store.setConnectionError('Previous error')

      store.setConnectionError(null)

      expect(useScanStore.getState().connectionError).toBeNull()
    })
  })

  describe('handleQueuePositionUpdate()', () => {
    it('should update queuePosition and totalInQueue', () => {
      const store = useScanStore.getState()
      store.startScan('scan-123', 'https://example.com', 'test@example.com', 5, false)

      const update: QueuePositionUpdate = {
        scanId: 'scan-123',
        position: 2,
        totalInQueue: 10,
      }
      store.handleQueuePositionUpdate(update)

      const state = useScanStore.getState()
      expect(state.queuePosition).toBe(2)
      expect(state.totalInQueue).toBe(10)
    })

    it('should keep viewState as queued', () => {
      const store = useScanStore.getState()
      store.startScan('scan-123', 'https://example.com', 'test@example.com', 5, false)

      store.handleQueuePositionUpdate({
        scanId: 'scan-123',
        position: 1,
        totalInQueue: 5,
      })

      expect(useScanStore.getState().viewState).toBe('queued')
    })

    it('should ignore updates for different scanId', () => {
      const store = useScanStore.getState()
      store.startScan('scan-123', 'https://example.com', 'test@example.com', 5, false)

      store.handleQueuePositionUpdate({
        scanId: 'different-scan',
        position: 1,
        totalInQueue: 10,
      })

      const state = useScanStore.getState()
      expect(state.queuePosition).toBe(5) // Unchanged
      expect(state.totalInQueue).toBe(5) // Unchanged (set by startScan)
    })
  })

  describe('handleScanStarted()', () => {
    it('should transition viewState to scanning', () => {
      const store = useScanStore.getState()
      store.startScan('scan-123', 'https://example.com', 'test@example.com', 3, false)

      const data: ScanStarted = {
        scanId: 'scan-123',
        targetUrl: 'https://example.com',
        startedAt: '2024-01-01T00:00:00Z',
      }
      store.handleScanStarted(data)

      expect(useScanStore.getState().viewState).toBe('scanning')
    })

    it('should update targetUrl and reset queuePosition', () => {
      const store = useScanStore.getState()
      store.startScan('scan-123', 'https://example.com', 'test@example.com', 3, false)

      store.handleScanStarted({
        scanId: 'scan-123',
        targetUrl: 'https://example.com/normalized',
        startedAt: '2024-01-01T00:00:00Z',
      })

      const state = useScanStore.getState()
      expect(state.targetUrl).toBe('https://example.com/normalized')
      expect(state.queuePosition).toBe(0)
    })

    it('should ignore updates for different scanId', () => {
      const store = useScanStore.getState()
      store.startScan('scan-123', 'https://example.com', 'test@example.com', 3, false)

      store.handleScanStarted({
        scanId: 'different-scan',
        targetUrl: 'https://other.com',
        startedAt: '2024-01-01T00:00:00Z',
      })

      const state = useScanStore.getState()
      expect(state.viewState).toBe('queued') // Still queued
      expect(state.targetUrl).toBe('https://example.com') // Unchanged
    })
  })

  describe('handlePageProgress()', () => {
    it('should update pagesScanned, pagesDiscovered, currentUrl, and progressPercent', () => {
      const store = useScanStore.getState()
      store.startScan('scan-123', 'https://example.com', 'test@example.com', 1, false)
      store.handleScanStarted({
        scanId: 'scan-123',
        targetUrl: 'https://example.com',
        startedAt: '2024-01-01T00:00:00Z',
      })

      const progress: PageProgress = {
        scanId: 'scan-123',
        currentUrl: 'https://example.com/page1',
        pagesScanned: 5,
        pagesDiscovered: 15,
        progressPercent: 33,
      }
      store.handlePageProgress(progress)

      const state = useScanStore.getState()
      expect(state.currentUrl).toBe('https://example.com/page1')
      expect(state.pagesScanned).toBe(5)
      expect(state.pagesDiscovered).toBe(15)
      expect(state.progressPercent).toBe(33)
    })

    it('should update progress multiple times', () => {
      const store = useScanStore.getState()
      store.startScan('scan-123', 'https://example.com', 'test@example.com', 1, false)

      store.handlePageProgress({
        scanId: 'scan-123',
        currentUrl: 'https://example.com/page1',
        pagesScanned: 1,
        pagesDiscovered: 5,
        progressPercent: 20,
      })

      store.handlePageProgress({
        scanId: 'scan-123',
        currentUrl: 'https://example.com/page2',
        pagesScanned: 2,
        pagesDiscovered: 8,
        progressPercent: 25,
      })

      const state = useScanStore.getState()
      expect(state.currentUrl).toBe('https://example.com/page2')
      expect(state.pagesScanned).toBe(2)
      expect(state.pagesDiscovered).toBe(8)
      expect(state.progressPercent).toBe(25)
    })

    it('should ignore updates for different scanId', () => {
      const store = useScanStore.getState()
      store.startScan('scan-123', 'https://example.com', 'test@example.com', 1, false)

      store.handlePageProgress({
        scanId: 'different-scan',
        currentUrl: 'https://other.com/page',
        pagesScanned: 10,
        pagesDiscovered: 20,
        progressPercent: 50,
      })

      const state = useScanStore.getState()
      expect(state.pagesScanned).toBe(0)
      expect(state.currentUrl).toBeNull()
    })
  })

  describe('handleImageFound()', () => {
    it('should increment totalImagesFound', () => {
      const store = useScanStore.getState()
      store.startScan('scan-123', 'https://example.com', 'test@example.com', 1, false)

      const imageData: ImageFound = {
        scanId: 'scan-123',
        imageUrl: 'https://example.com/image.png',
        mimeType: 'image/png',
        size: 1024,
        isNonWebP: true,
        totalNonWebPCount: 1,
        pageUrl: 'https://example.com/page1',
      }
      store.handleImageFound(imageData)

      expect(useScanStore.getState().totalImagesFound).toBe(1)
    })

    it('should update nonWebPImagesCount when isNonWebP is true', () => {
      const store = useScanStore.getState()
      store.startScan('scan-123', 'https://example.com', 'test@example.com', 1, false)

      store.handleImageFound({
        scanId: 'scan-123',
        imageUrl: 'https://example.com/image.png',
        mimeType: 'image/png',
        size: 1024,
        isNonWebP: true,
        totalNonWebPCount: 3,
        pageUrl: 'https://example.com/page1',
      })

      expect(useScanStore.getState().nonWebPImagesCount).toBe(3)
    })

    it('should not update nonWebPImagesCount when isNonWebP is false', () => {
      const store = useScanStore.getState()
      store.startScan('scan-123', 'https://example.com', 'test@example.com', 1, false)

      // First add a non-WebP image
      store.handleImageFound({
        scanId: 'scan-123',
        imageUrl: 'https://example.com/image.png',
        mimeType: 'image/png',
        size: 1024,
        isNonWebP: true,
        totalNonWebPCount: 1,
        pageUrl: 'https://example.com/page1',
      })

      // Then add a WebP image
      store.handleImageFound({
        scanId: 'scan-123',
        imageUrl: 'https://example.com/image.webp',
        mimeType: 'image/webp',
        size: 512,
        isNonWebP: false,
        totalNonWebPCount: 1, // Still 1 (the count doesn't change for WebP)
        pageUrl: 'https://example.com/page1',
      })

      const state = useScanStore.getState()
      expect(state.totalImagesFound).toBe(2)
      expect(state.nonWebPImagesCount).toBe(1) // Unchanged by the WebP image
    })

    it('should increment totalImagesFound for multiple images', () => {
      const store = useScanStore.getState()
      store.startScan('scan-123', 'https://example.com', 'test@example.com', 1, false)

      for (let i = 1; i <= 5; i++) {
        store.handleImageFound({
          scanId: 'scan-123',
          imageUrl: `https://example.com/image${i}.png`,
          mimeType: 'image/png',
          size: 1024,
          isNonWebP: true,
          totalNonWebPCount: i,
          pageUrl: 'https://example.com/page1',
        })
      }

      expect(useScanStore.getState().totalImagesFound).toBe(5)
      expect(useScanStore.getState().nonWebPImagesCount).toBe(5)
    })

    it('should ignore updates for different scanId', () => {
      const store = useScanStore.getState()
      store.startScan('scan-123', 'https://example.com', 'test@example.com', 1, false)

      store.handleImageFound({
        scanId: 'different-scan',
        imageUrl: 'https://other.com/image.png',
        mimeType: 'image/png',
        size: 1024,
        isNonWebP: true,
        totalNonWebPCount: 5,
        pageUrl: 'https://other.com/page1',
      })

      expect(useScanStore.getState().totalImagesFound).toBe(0)
    })
  })

  describe('handleScanComplete()', () => {
    it('should set viewState to completed and store results', () => {
      const store = useScanStore.getState()
      store.startScan('scan-123', 'https://example.com', 'test@example.com', 1, false)

      const completeData: ScanComplete = {
        scanId: 'scan-123',
        totalPagesScanned: 25,
        totalImagesFound: 100,
        nonWebPImagesCount: 75,
        duration: '00:02:30.500',
        completedAt: '2024-01-01T00:02:30Z',
        reachedPageLimit: false,
      }
      store.handleScanComplete(completeData)

      const state = useScanStore.getState()
      expect(state.viewState).toBe('completed')
      expect(state.pagesScanned).toBe(25)
      expect(state.totalImagesFound).toBe(100)
      expect(state.nonWebPImagesCount).toBe(75)
      expect(state.duration).toBe('00:02:30.500')
      expect(state.completedAt).toBe('2024-01-01T00:02:30Z')
      expect(state.reachedPageLimit).toBe(false)
      expect(state.progressPercent).toBe(100)
      expect(state.currentUrl).toBeNull()
    })

    it('should set reachedPageLimit when true', () => {
      const store = useScanStore.getState()
      store.startScan('scan-123', 'https://example.com', 'test@example.com', 1, false)

      store.handleScanComplete({
        scanId: 'scan-123',
        totalPagesScanned: 50, // Hit limit
        totalImagesFound: 200,
        nonWebPImagesCount: 150,
        duration: '00:05:00.000',
        completedAt: '2024-01-01T00:05:00Z',
        reachedPageLimit: true,
      })

      expect(useScanStore.getState().reachedPageLimit).toBe(true)
    })

    it('should ignore updates for different scanId', () => {
      const store = useScanStore.getState()
      store.startScan('scan-123', 'https://example.com', 'test@example.com', 1, false)

      store.handleScanComplete({
        scanId: 'different-scan',
        totalPagesScanned: 100,
        totalImagesFound: 500,
        nonWebPImagesCount: 400,
        duration: '00:10:00.000',
        completedAt: '2024-01-01T00:10:00Z',
        reachedPageLimit: false,
      })

      const state = useScanStore.getState()
      expect(state.viewState).toBe('queued') // Still queued
      expect(state.pagesScanned).toBe(0)
    })
  })

  describe('handleScanFailed()', () => {
    it('should set viewState to failed and store error message', () => {
      const store = useScanStore.getState()
      store.startScan('scan-123', 'https://example.com', 'test@example.com', 1, false)

      const failData: ScanFailed = {
        scanId: 'scan-123',
        errorMessage: 'Connection timeout',
        failedAt: '2024-01-01T00:01:00Z',
      }
      store.handleScanFailed(failData)

      const state = useScanStore.getState()
      expect(state.viewState).toBe('failed')
      expect(state.errorMessage).toBe('Connection timeout')
    })

    it('should ignore updates for different scanId', () => {
      const store = useScanStore.getState()
      store.startScan('scan-123', 'https://example.com', 'test@example.com', 1, false)

      store.handleScanFailed({
        scanId: 'different-scan',
        errorMessage: 'Some error',
        failedAt: '2024-01-01T00:01:00Z',
      })

      const state = useScanStore.getState()
      expect(state.viewState).toBe('queued') // Still queued
      expect(state.errorMessage).toBeNull()
    })
  })

  describe('syncFromSnapshot()', () => {
    it('should correctly map Queued status to queued viewState', () => {
      const store = useScanStore.getState()

      const snapshot: ScanProgressSnapshot = {
        status: 'Queued',
        queuePosition: 3,
        pagesScanned: 0,
        pagesDiscovered: 0,
        nonWebPImagesCount: 0,
        progressPercent: 0,
        currentUrl: null,
        errorMessage: null,
      }
      store.syncFromSnapshot(snapshot)

      expect(useScanStore.getState().viewState).toBe('queued')
      expect(useScanStore.getState().queuePosition).toBe(3)
    })

    it('should correctly map Processing status to scanning viewState', () => {
      const store = useScanStore.getState()

      const snapshot: ScanProgressSnapshot = {
        status: 'Processing',
        queuePosition: 0,
        pagesScanned: 10,
        pagesDiscovered: 25,
        nonWebPImagesCount: 5,
        progressPercent: 40,
        currentUrl: 'https://example.com/current',
        errorMessage: null,
      }
      store.syncFromSnapshot(snapshot)

      const state = useScanStore.getState()
      expect(state.viewState).toBe('scanning')
      expect(state.pagesScanned).toBe(10)
      expect(state.pagesDiscovered).toBe(25)
      expect(state.nonWebPImagesCount).toBe(5)
      expect(state.progressPercent).toBe(40)
      expect(state.currentUrl).toBe('https://example.com/current')
    })

    it('should correctly map Completed status to completed viewState', () => {
      const store = useScanStore.getState()

      const snapshot: ScanProgressSnapshot = {
        status: 'Completed',
        queuePosition: 0,
        pagesScanned: 50,
        pagesDiscovered: 50,
        nonWebPImagesCount: 30,
        progressPercent: 100,
        currentUrl: null,
        errorMessage: null,
      }
      store.syncFromSnapshot(snapshot)

      expect(useScanStore.getState().viewState).toBe('completed')
    })

    it('should correctly map Failed status to failed viewState', () => {
      const store = useScanStore.getState()

      const snapshot: ScanProgressSnapshot = {
        status: 'Failed',
        queuePosition: 0,
        pagesScanned: 5,
        pagesDiscovered: 10,
        nonWebPImagesCount: 2,
        progressPercent: 50,
        currentUrl: null,
        errorMessage: 'Network error occurred',
      }
      store.syncFromSnapshot(snapshot)

      const state = useScanStore.getState()
      expect(state.viewState).toBe('failed')
      expect(state.errorMessage).toBe('Network error occurred')
    })

    it('should map unknown status to idle viewState', () => {
      const store = useScanStore.getState()

      const snapshot: ScanProgressSnapshot = {
        status: 'Unknown',
        queuePosition: 0,
        pagesScanned: 0,
        pagesDiscovered: 0,
        nonWebPImagesCount: 0,
        progressPercent: 0,
        currentUrl: null,
        errorMessage: null,
      }
      store.syncFromSnapshot(snapshot)

      expect(useScanStore.getState().viewState).toBe('idle')
    })
  })

  describe('mapStatusToViewState() via syncFromSnapshot', () => {
    const testCases: Array<{ status: string; expected: string }> = [
      { status: 'Queued', expected: 'queued' },
      { status: 'Processing', expected: 'scanning' },
      { status: 'Completed', expected: 'completed' },
      { status: 'Failed', expected: 'failed' },
      { status: 'InvalidStatus', expected: 'idle' },
      { status: '', expected: 'idle' },
    ]

    testCases.forEach(({ status, expected }) => {
      it(`should map "${status}" status to "${expected}" viewState`, () => {
        const store = useScanStore.getState()

        store.syncFromSnapshot({
          status,
          queuePosition: 0,
          pagesScanned: 0,
          pagesDiscovered: 0,
          nonWebPImagesCount: 0,
          progressPercent: 0,
          currentUrl: null,
          errorMessage: null,
        })

        expect(useScanStore.getState().viewState).toBe(expected)
      })
    })
  })
})
