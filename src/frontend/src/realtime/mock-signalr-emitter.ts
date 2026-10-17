/**
 * Mock SignalR Emitter
 *
 * In-memory event bus providing identical semantics to ASP.NET Core SignalR
 * for tests and local mock development (§10).
 */
import type { SignalREventMap, SignalREventName } from './signalr-events'

type Listener<T> = (payload: T) => void

export class MockSignalREmitter {
  private listeners: Map<SignalREventName, Set<Listener<unknown>>> = new Map()
  private activeGroups: Set<string> = new Set()
  private isConnectedState = true

  get isConnected(): boolean {
    return this.isConnectedState
  }

  setConnected(connected: boolean): void {
    this.isConnectedState = connected
  }

  /**
   * Register a typed listener for a SignalR event.
   * Returns an unsubscription function.
   */
  on<K extends SignalREventName>(
    event: K,
    listener: (payload: SignalREventMap[K]) => void,
  ): () => void {
    let set = this.listeners.get(event)
    if (!set) {
      set = new Set()
      this.listeners.set(event, set)
    }

    const untypedListener = listener as Listener<unknown>
    set.add(untypedListener)

    return () => {
      set.delete(untypedListener)
    }
  }

  /**
   * Emit a typed event to all registered listeners.
   */
  emit<K extends SignalREventName>(event: K, payload: SignalREventMap[K]): void {
    if (!this.isConnectedState) {
      return
    }

    const set = this.listeners.get(event)
    if (set) {
      set.forEach((listener) => {
        try {
          listener(payload)
        } catch (err) {
          console.error(`Error in SignalR listener for ${event}:`, err)
        }
      })
    }
  }

  /**
   * Client-to-server mock: join incident group
   */
  joinIncidentGroup(incidentId: string): Promise<void> {
    this.activeGroups.add(`incident_${incidentId}`)
    return Promise.resolve()
  }

  /**
   * Client-to-server mock: leave incident group
   */
  leaveIncidentGroup(incidentId: string): Promise<void> {
    this.activeGroups.delete(`incident_${incidentId}`)
    return Promise.resolve()
  }

  isMemberOfGroup(incidentId: string): boolean {
    return this.activeGroups.has(`incident_${incidentId}`)
  }

  /**
   * Clear all listeners and groups (useful in test teardown).
   */
  reset(): void {
    this.listeners.clear()
    this.activeGroups.clear()
    this.isConnectedState = true
  }
}

export const mockSignalREmitter = new MockSignalREmitter()
