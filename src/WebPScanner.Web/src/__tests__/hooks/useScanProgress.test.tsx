import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { useScanStore } from '../../store/scanStore'

// Create mock objects at module level so they persist across tests
const mockConnection = {
  start: vi.fn().mockResolvedValue(undefined),
  stop: vi.fn().mockResolvedValue(undefined),
  invoke: vi.fn().mockResolvedValue(undefined),
  on: vi.fn(),
  onreconnecting: vi.fn(),
  onreconnected: vi.fn(),
  onclose: vi.fn(),
  state: 'Disconnected' as string,
}

const mockBuilder = {
  withUrl: vi.fn().mockReturnThis(),
  withAutomaticReconnect: vi.fn().mockReturnThis(),
  configureLogging: vi.fn().mockReturnThis(),
  build: vi.fn().mockReturnValue(mockConnection),
}

// Mock @microsoft/signalr with a proper class constructor
vi.mock('@microsoft/signalr', () => {
  // Create HubConnectionBuilder as a proper class that can be instantiated with new
  class MockHubConnectionBuilder {
    withUrl(url: string) {
      mockBuilder.withUrl(url)
      return this
    }
    withAutomaticReconnect(delays: number[]) {
      mockBuilder.withAutomaticReconnect(delays)
      return this
    }
    configureLogging(level: number) {
      mockBuilder.configureLogging(level)
      return this
    }
    build() {
      mockBuilder.build()
      return mockConnection
    }
  }

  return {
    HubConnectionBuilder: MockHubConnectionBuilder,
    HubConnectionState: {
      Disconnected: 'Disconnected',
      Connecting: 'Connecting',
      Connected: 'Connected',
      Disconnecting: 'Disconnecting',
      Reconnecting: 'Reconnecting',
    },
    LogLevel: {
      Warning: 3,
    },
  }
})

// Import after mock is set up
import * as signalR from '@microsoft/signalr'
import { useScanProgress } from '../../hooks/useScanProgress'

describe('useScanProgress', () => {
  beforeEach(() => {
    // Reset store state
    useScanStore.getState().reset()

    // Reset mock implementations
    mockConnection.start.mockReset().mockResolvedValue(undefined)
    mockConnection.stop.mockReset().mockResolvedValue(undefined)
    mockConnection.invoke.mockReset().mockResolvedValue(undefined)
    mockConnection.on.mockReset()
    mockConnection.onreconnecting.mockReset()
    mockConnection.onreconnected.mockReset()
    mockConnection.onclose.mockReset()
    mockConnection.state = 'Disconnected'

    mockBuilder.withUrl.mockClear()
    mockBuilder.withAutomaticReconnect.mockClear()
    mockBuilder.configureLogging.mockClear()
    mockBuilder.build.mockClear()
  })

  afterEach(() => {
    vi.clearAllMocks()
  })

  describe('connectToHub', () => {
    it('should establish connection with correct URL', async () => {
      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      expect(mockBuilder.withUrl).toHaveBeenCalledWith('/hubs/scanprogress')
    })

    it('should configure automatic reconnect with correct delays', async () => {
      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      expect(mockBuilder.withAutomaticReconnect).toHaveBeenCalledWith([0, 2000, 5000, 10000, 30000])
    })

    it('should configure logging level to Warning', async () => {
      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      expect(mockBuilder.configureLogging).toHaveBeenCalledWith(signalR.LogLevel.Warning)
    })

    it('should subscribe to all SignalR events', async () => {
      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      const onCalls = mockConnection.on.mock.calls.map((call) => call[0])
      expect(onCalls).toContain('QueuePositionUpdate')
      expect(onCalls).toContain('ScanStarted')
      expect(onCalls).toContain('PageProgress')
      expect(onCalls).toContain('ImageFound')
      expect(onCalls).toContain('ScanComplete')
      expect(onCalls).toContain('ScanFailed')
    })

    it('should call SubscribeToScan with scanId after connection', async () => {
      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      expect(mockConnection.invoke).toHaveBeenCalledWith('SubscribeToScan', 'scan-123')
    })

    it('should update store to connected state on success', async () => {
      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      const state = useScanStore.getState()
      expect(state.isConnected).toBe(true)
      expect(state.isConnecting).toBe(false)
    })

    it('should set connecting state before connection starts', async () => {
      let connectingStateBeforeStart = false

      mockConnection.start.mockImplementation(async () => {
        connectingStateBeforeStart = useScanStore.getState().isConnecting
      })

      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      expect(connectingStateBeforeStart).toBe(true)
    })

    it('should handle connection failure gracefully', async () => {
      const connectionError = new Error('Connection failed')
      mockConnection.start.mockRejectedValue(connectionError)

      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      const state = useScanStore.getState()
      expect(state.isConnected).toBe(false)
      expect(state.isConnecting).toBe(false)
      expect(state.connectionError).toBe('Connection failed')
    })

    it('should set generic error message for non-Error exceptions', async () => {
      mockConnection.start.mockRejectedValue('String error')

      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      const state = useScanStore.getState()
      expect(state.connectionError).toBe('Failed to connect to progress updates')
    })

    it('should prevent concurrent connection attempts', async () => {
      // Make start resolve slowly
      let resolveStart: () => void
      mockConnection.start.mockImplementation(
        () =>
          new Promise<void>((resolve) => {
            resolveStart = resolve
          })
      )

      const { result } = renderHook(() => useScanProgress())

      // Start two connections simultaneously
      act(() => {
        result.current.connect('scan-123')
        result.current.connect('scan-123') // Should be ignored
      })

      // Allow first connection to complete
      await act(async () => {
        resolveStart!()
        await new Promise((resolve) => setTimeout(resolve, 10))
      })

      // Should only have built one connection
      expect(mockBuilder.build).toHaveBeenCalledTimes(1)
    })

    it('should not reconnect if already connected to same scan', async () => {
      mockConnection.state = 'Connected'

      const { result } = renderHook(() => useScanProgress())

      // First connection
      await act(async () => {
        await result.current.connect('scan-123')
      })

      // Reset mock calls
      mockBuilder.build.mockClear()

      // Second connection to same scan
      await act(async () => {
        await result.current.connect('scan-123')
      })

      // Should not build a new connection
      expect(mockBuilder.build).not.toHaveBeenCalled()
    })
  })

  describe('disconnectFromHub', () => {
    it('should stop connection gracefully', async () => {
      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      mockConnection.state = 'Connected'

      await act(async () => {
        await result.current.disconnect()
      })

      expect(mockConnection.invoke).toHaveBeenCalledWith('UnsubscribeFromScan', 'scan-123')
      expect(mockConnection.stop).toHaveBeenCalled()
    })

    it('should update store to disconnected state', async () => {
      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      await act(async () => {
        await result.current.disconnect()
      })

      expect(useScanStore.getState().isConnected).toBe(false)
    })

    it('should handle disconnect errors gracefully', async () => {
      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      mockConnection.state = 'Connected'
      mockConnection.stop.mockRejectedValue(new Error('Stop failed'))

      // Should not throw
      await act(async () => {
        await result.current.disconnect()
      })

      expect(useScanStore.getState().isConnected).toBe(false)
    })

    it('should do nothing if no connection exists', async () => {
      const { result } = renderHook(() => useScanProgress())

      // Should not throw
      await act(async () => {
        await result.current.disconnect()
      })

      expect(mockConnection.stop).not.toHaveBeenCalled()
    })
  })

  describe('event handlers', () => {
    it('should call store.handleQueuePositionUpdate on QueuePositionUpdate event', async () => {
      // Start a scan first so the scanId matches
      useScanStore.getState().startScan('scan-123', 'https://example.com', 'test@test.com', 1, false)

      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      // Find and call the QueuePositionUpdate handler
      const onCall = mockConnection.on.mock.calls.find((call) => call[0] === 'QueuePositionUpdate')
      expect(onCall).toBeDefined()

      const handler = onCall![1]
      const eventData = { scanId: 'scan-123', position: 2, totalInQueue: 5 }

      act(() => {
        handler(eventData)
      })

      expect(useScanStore.getState().queuePosition).toBe(2)
      expect(useScanStore.getState().totalInQueue).toBe(5)
    })

    it('should call store.handleScanStarted on ScanStarted event', async () => {
      useScanStore.getState().startScan('scan-123', 'https://example.com', 'test@test.com', 1, false)

      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      const onCall = mockConnection.on.mock.calls.find((call) => call[0] === 'ScanStarted')
      const handler = onCall![1]

      act(() => {
        handler({
          scanId: 'scan-123',
          targetUrl: 'https://example.com',
          startedAt: '2024-01-01T00:00:00Z',
        })
      })

      expect(useScanStore.getState().viewState).toBe('scanning')
    })

    it('should call store.handlePageProgress on PageProgress event', async () => {
      useScanStore.getState().startScan('scan-123', 'https://example.com', 'test@test.com', 1, false)

      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      const onCall = mockConnection.on.mock.calls.find((call) => call[0] === 'PageProgress')
      const handler = onCall![1]

      act(() => {
        handler({
          scanId: 'scan-123',
          currentUrl: 'https://example.com/page1',
          pagesScanned: 5,
          pagesDiscovered: 10,
          progressPercent: 50,
        })
      })

      const state = useScanStore.getState()
      expect(state.pagesScanned).toBe(5)
      expect(state.currentUrl).toBe('https://example.com/page1')
    })

    it('should call store.handleImageFound on ImageFound event', async () => {
      useScanStore.getState().startScan('scan-123', 'https://example.com', 'test@test.com', 1, false)

      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      const onCall = mockConnection.on.mock.calls.find((call) => call[0] === 'ImageFound')
      const handler = onCall![1]

      act(() => {
        handler({
          scanId: 'scan-123',
          imageUrl: 'https://example.com/image.png',
          mimeType: 'image/png',
          size: 1024,
          isNonWebP: true,
          totalNonWebPCount: 1,
          pageUrl: 'https://example.com',
        })
      })

      expect(useScanStore.getState().totalImagesFound).toBe(1)
    })

    it('should call store.handleScanComplete on ScanComplete event', async () => {
      useScanStore.getState().startScan('scan-123', 'https://example.com', 'test@test.com', 1, false)

      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      const onCall = mockConnection.on.mock.calls.find((call) => call[0] === 'ScanComplete')
      const handler = onCall![1]

      act(() => {
        handler({
          scanId: 'scan-123',
          totalPagesScanned: 25,
          totalImagesFound: 100,
          nonWebPImagesCount: 75,
          duration: '00:02:30.500',
          completedAt: '2024-01-01T00:02:30Z',
          reachedPageLimit: false,
        })
      })

      expect(useScanStore.getState().viewState).toBe('completed')
    })

    it('should call store.handleScanFailed on ScanFailed event', async () => {
      useScanStore.getState().startScan('scan-123', 'https://example.com', 'test@test.com', 1, false)

      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      const onCall = mockConnection.on.mock.calls.find((call) => call[0] === 'ScanFailed')
      const handler = onCall![1]

      act(() => {
        handler({
          scanId: 'scan-123',
          errorMessage: 'Connection timeout',
          failedAt: '2024-01-01T00:01:00Z',
        })
      })

      const state = useScanStore.getState()
      expect(state.viewState).toBe('failed')
      expect(state.errorMessage).toBe('Connection timeout')
    })
  })

  describe('connection state callbacks', () => {
    it('should handle onreconnecting callback', async () => {
      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      // Get the onreconnecting callback
      expect(mockConnection.onreconnecting).toHaveBeenCalled()
      const onreconnectingCall = mockConnection.onreconnecting.mock.calls[0]
      const reconnectingCallback = onreconnectingCall[0]

      // Simulate reconnecting
      act(() => {
        reconnectingCallback(new Error('Connection lost'))
      })

      const state = useScanStore.getState()
      expect(state.isConnected).toBe(false)
      expect(state.isConnecting).toBe(true)
    })

    it('should handle onreconnected callback and re-subscribe', async () => {
      mockConnection.invoke.mockResolvedValue(null)

      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      // Get the onreconnected callback
      expect(mockConnection.onreconnected).toHaveBeenCalled()
      const onreconnectedCall = mockConnection.onreconnected.mock.calls[0]
      const reconnectedCallback = onreconnectedCall[0]

      // Clear previous invoke calls
      mockConnection.invoke.mockClear()

      // Simulate reconnected
      await act(async () => {
        await reconnectedCallback()
      })

      const state = useScanStore.getState()
      expect(state.isConnected).toBe(true)
      expect(state.isConnecting).toBe(false)

      // Should have re-subscribed
      expect(mockConnection.invoke).toHaveBeenCalledWith('SubscribeToScan', 'scan-123')
      expect(mockConnection.invoke).toHaveBeenCalledWith('GetCurrentProgress', 'scan-123')
    })

    it('should sync state from snapshot on reconnect', async () => {
      const snapshot = {
        status: 'Processing',
        queuePosition: 0,
        pagesScanned: 10,
        pagesDiscovered: 20,
        nonWebPImagesCount: 5,
        progressPercent: 50,
        currentUrl: 'https://example.com/page5',
        errorMessage: null,
      }

      mockConnection.invoke.mockImplementation((method: string) => {
        if (method === 'GetCurrentProgress') {
          return Promise.resolve(snapshot)
        }
        return Promise.resolve(undefined)
      })

      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      expect(mockConnection.onreconnected).toHaveBeenCalled()
      const onreconnectedCall = mockConnection.onreconnected.mock.calls[0]
      const reconnectedCallback = onreconnectedCall[0]

      await act(async () => {
        await reconnectedCallback()
      })

      const state = useScanStore.getState()
      expect(state.viewState).toBe('scanning')
      expect(state.pagesScanned).toBe(10)
      expect(state.progressPercent).toBe(50)
    })

    it('should handle onclose callback', async () => {
      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      expect(mockConnection.onclose).toHaveBeenCalled()
      const oncloseCall = mockConnection.onclose.mock.calls[0]
      const closeCallback = oncloseCall[0]

      act(() => {
        closeCallback(new Error('Connection closed'))
      })

      const state = useScanStore.getState()
      expect(state.isConnected).toBe(false)
      expect(state.isConnecting).toBe(false)
    })
  })

  describe('return values', () => {
    it('should return connect and disconnect functions', () => {
      const { result } = renderHook(() => useScanProgress())

      expect(result.current.connect).toBeInstanceOf(Function)
      expect(result.current.disconnect).toBeInstanceOf(Function)
    })
  })

  describe('reconnect delays', () => {
    it('should use correct reconnect delay values [0, 2000, 5000, 10000, 30000]', async () => {
      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      // Verify the builder was called with correct reconnect delays
      expect(mockBuilder.withAutomaticReconnect).toHaveBeenCalledWith([0, 2000, 5000, 10000, 30000])
    })
  })

  describe('hub URL', () => {
    it('should connect to /hubs/scanprogress endpoint', async () => {
      const { result } = renderHook(() => useScanProgress())

      await act(async () => {
        await result.current.connect('scan-123')
      })

      expect(mockBuilder.withUrl).toHaveBeenCalledWith('/hubs/scanprogress')
    })
  })
})
