/**
 * Trinetra Realtime Context & Hook
 *
 * Provides reactive access to SignalR events and connection status
 * across the Trinetra console shell and screens (§10).
 */
import {
  createContext,
  useContext,
  useEffect,
  useState,
  useCallback,
  useRef,
  type ReactNode,
} from 'react'
import { useAuth } from '../auth/auth-context'
import type { SignalREventMap, SignalREventName } from './signalr-events'
import { mockSignalREmitter, MockSignalREmitter } from './mock-signalr-emitter'
import { SignalRClient, type ConnectionState } from './signalr-client'

export interface RealtimeContextValue {
  connectionState: ConnectionState
  isConnected: boolean
  on: <K extends SignalREventName>(
    event: K,
    listener: (payload: SignalREventMap[K]) => void,
  ) => () => void
  joinIncidentGroup: (incidentId: string) => Promise<void>
  leaveIncidentGroup: (incidentId: string) => Promise<void>
  emitMockEvent: <K extends SignalREventName>(event: K, payload: SignalREventMap[K]) => void
  mockEmitter?: MockSignalREmitter
}

const RealtimeContext = createContext<RealtimeContextValue | null>(null)

export interface RealtimeProviderProps {
  children: ReactNode
  forceMock?: boolean
}

export function RealtimeProvider({ children, forceMock }: RealtimeProviderProps) {
  const { token } = useAuth()
  const isMock =
    forceMock ??
    (import.meta.env.VITE_MOCK_API === 'true' ||
      (import.meta.env.DEV && import.meta.env.VITE_MOCK_API !== 'false'))

  const [connectionState, setConnectionState] = useState<ConnectionState>(
    isMock ? 'connected' : 'connecting',
  )

  const clientRef = useRef<SignalRClient | null>(null)

  useEffect(() => {
    if (isMock) {
      setConnectionState('connected')
      return
    }

    const client = new SignalRClient({
      getAccessToken: () => token,
      onStateChange: (state) => {
        setConnectionState(state)
      },
    })
    clientRef.current = client

    client.start().catch((err: unknown) => {
      console.warn('SignalR failed to connect to backend, falling back to mock:', err)
      setConnectionState('disconnected')
    })

    return () => {
      void client.stop()
      clientRef.current = null
    }
  }, [isMock, token])

  const on = useCallback(
    <K extends SignalREventName>(
      event: K,
      listener: (payload: SignalREventMap[K]) => void,
    ): (() => void) => {
      if (isMock) {
        return mockSignalREmitter.on(event, listener)
      }
      return clientRef.current ? clientRef.current.on(event, listener) : () => {}
    },
    [isMock],
  )

  const joinIncidentGroup = useCallback(
    async (incidentId: string): Promise<void> => {
      if (isMock) {
        return mockSignalREmitter.joinIncidentGroup(incidentId)
      }
      return clientRef.current ? clientRef.current.joinIncidentGroup(incidentId) : undefined
    },
    [isMock],
  )

  const leaveIncidentGroup = useCallback(
    async (incidentId: string): Promise<void> => {
      if (isMock) {
        return mockSignalREmitter.leaveIncidentGroup(incidentId)
      }
      return clientRef.current ? clientRef.current.leaveIncidentGroup(incidentId) : undefined
    },
    [isMock],
  )

  const emitMockEvent = useCallback(
    <K extends SignalREventName>(event: K, payload: SignalREventMap[K]): void => {
      mockSignalREmitter.emit(event, payload)
    },
    [],
  )

  const value: RealtimeContextValue = {
    connectionState,
    isConnected: connectionState === 'connected',
    on,
    joinIncidentGroup,
    leaveIncidentGroup,
    emitMockEvent,
    mockEmitter: isMock ? mockSignalREmitter : undefined,
  }

  return <RealtimeContext.Provider value={value}>{children}</RealtimeContext.Provider>
}

export function useRealtime(): RealtimeContextValue {
  const context = useContext(RealtimeContext)
  if (!context) {
    throw new Error('useRealtime must be used within a RealtimeProvider')
  }
  return context
}

/**
 * Convenient hook for subscribing to a specific SignalR event with automatic cleanup.
 */
export function useRealtimeSubscription<K extends SignalREventName>(
  event: K,
  callback: (payload: SignalREventMap[K]) => void,
) {
  const { on } = useRealtime()

  const savedCallback = useRef(callback)
  useEffect(() => {
    savedCallback.current = callback
  }, [callback])

  useEffect(() => {
    const unsubscribe = on(event, (payload) => {
      savedCallback.current(payload)
    })
    return unsubscribe
  }, [event, on])
}
