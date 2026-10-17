/**
 * Trinetra Realtime SignalR Client
 *
 * Production wrapper around @microsoft/signalr HubConnection with
 * automatic exponential backoff reconnect, strict typing, and token integration.
 */
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr'
import type { SignalREventMap, SignalREventName } from './signalr-events'

export type ConnectionState = 'connecting' | 'connected' | 'reconnecting' | 'disconnected'

export interface SignalRClientOptions {
  hubUrl?: string
  getAccessToken?: () => string | null | Promise<string | null>
  onStateChange?: (state: ConnectionState) => void
}

export class SignalRClient {
  private connection: HubConnection | null = null
  private hubUrl: string
  private getAccessToken?: () => string | null | Promise<string | null>
  private onStateChange?: (state: ConnectionState) => void
  private connectionState: ConnectionState = 'disconnected'

  constructor(options: SignalRClientOptions = {}) {
    this.hubUrl = options.hubUrl || import.meta.env.VITE_SIGNALR_HUB_URL || '/hubs/incidents'
    this.getAccessToken = options.getAccessToken
    this.onStateChange = options.onStateChange
  }

  get state(): ConnectionState {
    return this.connectionState
  }

  get isConnected(): boolean {
    return this.connectionState === 'connected'
  }

  private updateState(newState: ConnectionState) {
    if (this.connectionState !== newState) {
      this.connectionState = newState
      this.onStateChange?.(newState)
    }
  }

  async start(): Promise<void> {
    if (this.connection && this.connection.state === HubConnectionState.Connected) {
      return
    }

    this.updateState('connecting')

    try {
      const builder = new HubConnectionBuilder()
        .withUrl(this.hubUrl, {
          accessTokenFactory: async () => {
            const token = this.getAccessToken ? await this.getAccessToken() : null
            return token || ''
          },
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(LogLevel.Warning)

      this.connection = builder.build()

      this.connection.onreconnecting(() => {
        this.updateState('reconnecting')
      })

      this.connection.onreconnected(() => {
        this.updateState('connected')
      })

      this.connection.onclose(() => {
        this.updateState('disconnected')
      })

      await this.connection.start()
      this.updateState('connected')
    } catch (err) {
      this.updateState('disconnected')
      throw err
    }
  }

  async stop(): Promise<void> {
    if (!this.connection) {
      return
    }

    try {
      await this.connection.stop()
    } finally {
      this.updateState('disconnected')
      this.connection = null
    }
  }

  /**
   * Register a typed listener for a SignalR hub event.
   * Returns an unsubscription function.
   */
  on<K extends SignalREventName>(
    event: K,
    callback: (payload: SignalREventMap[K]) => void,
  ): () => void {
    if (!this.connection) {
      return () => {}
    }

    this.connection.on(event, callback)

    return () => {
      this.connection?.off(event, callback)
    }
  }

  async joinIncidentGroup(incidentId: string): Promise<void> {
    if (this.connection && this.connection.state === HubConnectionState.Connected) {
      await this.connection.invoke('JoinIncidentGroup', incidentId)
    }
  }

  async leaveIncidentGroup(incidentId: string): Promise<void> {
    if (this.connection && this.connection.state === HubConnectionState.Connected) {
      await this.connection.invoke('LeaveIncidentGroup', incidentId)
    }
  }
}
