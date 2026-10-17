/**
 * Unit Tests for SignalR Realtime Event Contract & Client (Task F2-4)
 *
 * Verifies real-time event schemas, mock emitter dispatch,
 * group subscriptions, and RealtimeProvider integration.
 */
import { describe, it, expect, beforeEach, vi } from 'vitest'
import { useState } from 'react'
import { render, screen, act } from '@testing-library/react'
import {
  IncidentCreatedPayloadSchema,
  IncidentStatusChangedPayloadSchema,
  AgentActionCompletedPayloadSchema,
  ApprovalRequiredPayloadSchema,
  type IncidentCreatedPayload,
} from '@/realtime/signalr-events'
import { MockSignalREmitter } from '@/realtime/mock-signalr-emitter'
import { RealtimeProvider, useRealtime, useRealtimeSubscription } from '@/realtime/realtime-context'
import { AuthProvider } from '@/auth/auth-context'

describe('SignalR Realtime Event Contract & Client (Task F2-4)', () => {
  // ==========================================================================
  // Event Schema Validation Gate
  // ==========================================================================

  describe('SignalR Event Schemas (Doc 04 / Hub Contract)', () => {
    it('validates IncidentCreated payload against IncidentCreatedPayloadSchema', () => {
      const validPayload = {
        id: 'INC-2024-8901',
        incidentId: 'INC-2024-8901',
        title: 'Database connection pool exhaustion',
        severity: 'critical',
        status: 'awaiting_approval',
        triggeredAt: '2026-10-17T16:30:00Z',
      }

      const result = IncidentCreatedPayloadSchema.safeParse(validPayload)
      expect(result.success).toBe(true)
    })

    it('rejects IncidentCreated payload with invalid severity', () => {
      const invalidPayload = {
        id: 'INC-2024-8901',
        incidentId: 'INC-2024-8901',
        title: 'Database connection pool exhaustion',
        severity: 'catastrophic', // Invalid severity
        status: 'awaiting_approval',
        triggeredAt: '2026-10-17T16:30:00Z',
      }

      const result = IncidentCreatedPayloadSchema.safeParse(invalidPayload)
      expect(result.success).toBe(false)
    })

    it('validates IncidentStatusChanged payload', () => {
      const validPayload = {
        incidentId: 'INC-2024-8901',
        status: 'executing_remediation',
        updatedAt: '2026-10-17T16:35:00Z',
      }

      const result = IncidentStatusChangedPayloadSchema.safeParse(validPayload)
      expect(result.success).toBe(true)
    })

    it('validates AgentActionCompleted payload', () => {
      const validPayload = {
        incidentId: 'INC-2024-8901',
        agentName: 'Log Analyzer',
        actionDescription: 'Queried Elasticsearch for exception spikes',
        toolCall: 'query_logs',
        toolResult: 'Found 412 NpgsqlException',
        status: 'success',
        timestamp: '2026-10-17T16:31:25Z',
      }

      const result = AgentActionCompletedPayloadSchema.safeParse(validPayload)
      expect(result.success).toBe(true)
    })

    it('validates ApprovalRequired payload', () => {
      const validPayload = {
        incidentId: 'INC-2024-8901',
        eventId: 'evt-8901-07',
        actionDescription: 'Terminate idle transactions and expand pool',
        toolCall: 'generate_remediation_options',
        timeoutMinutes: 15,
        timestamp: '2026-10-17T16:32:30Z',
      }

      const result = ApprovalRequiredPayloadSchema.safeParse(validPayload)
      expect(result.success).toBe(true)
    })
  })

  // ==========================================================================
  // MockSignalREmitter Implementation
  // ==========================================================================

  describe('MockSignalREmitter', () => {
    let emitter: MockSignalREmitter

    beforeEach(() => {
      emitter = new MockSignalREmitter()
    })

    it('registers a listener and dispatches events correctly', () => {
      const listener = vi.fn()
      emitter.on('IncidentCreated', listener)

      const payload: IncidentCreatedPayload = {
        id: 'INC-2024-9999',
        incidentId: 'INC-2024-9999',
        title: 'Network split detected',
        severity: 'critical',
        status: 'triggered',
        triggeredAt: '2026-10-17T18:00:00Z',
      }

      emitter.emit('IncidentCreated', payload)

      expect(listener).toHaveBeenCalledTimes(1)
      expect(listener).toHaveBeenCalledWith(payload)
    })

    it('unsubscribes listener when calling the returned cleanup function', () => {
      const listener = vi.fn()
      const unsubscribe = emitter.on('IncidentCreated', listener)

      unsubscribe()

      emitter.emit('IncidentCreated', {
        id: 'INC-2024-9999',
        incidentId: 'INC-2024-9999',
        title: 'Network split detected',
        severity: 'critical',
        status: 'triggered',
        triggeredAt: '2026-10-17T18:00:00Z',
      })

      expect(listener).not.toHaveBeenCalled()
    })

    it('tracks joining and leaving incident groups', async () => {
      expect(emitter.isMemberOfGroup('INC-2024-8901')).toBe(false)

      await emitter.joinIncidentGroup('INC-2024-8901')
      expect(emitter.isMemberOfGroup('INC-2024-8901')).toBe(true)

      await emitter.leaveIncidentGroup('INC-2024-8901')
      expect(emitter.isMemberOfGroup('INC-2024-8901')).toBe(false)
    })

    it('suppresses event dispatch when disconnected', () => {
      const listener = vi.fn()
      emitter.on('IncidentCreated', listener)

      emitter.setConnected(false)

      emitter.emit('IncidentCreated', {
        id: 'INC-2024-9999',
        incidentId: 'INC-2024-9999',
        title: 'Network split detected',
        severity: 'critical',
        status: 'triggered',
        triggeredAt: '2026-10-17T18:00:00Z',
      })

      expect(listener).not.toHaveBeenCalled()
    })

    it('clears all listeners and state on reset()', () => {
      const listener = vi.fn()
      emitter.on('IncidentCreated', listener)

      emitter.reset()

      emitter.emit('IncidentCreated', {
        id: 'INC-2024-9999',
        incidentId: 'INC-2024-9999',
        title: 'Network split detected',
        severity: 'critical',
        status: 'triggered',
        triggeredAt: '2026-10-17T18:00:00Z',
      })

      expect(listener).not.toHaveBeenCalled()
    })
  })

  // ==========================================================================
  // RealtimeProvider & Hooks Integration
  // ==========================================================================

  describe('RealtimeProvider & React Hooks', () => {
    function TestConsumer() {
      const { isConnected, connectionState } = useRealtime()
      return (
        <div>
          <div data-testid="connection-status">{isConnected ? 'connected' : 'disconnected'}</div>
          <div data-testid="connection-state">{connectionState}</div>
        </div>
      )
    }

    it('provides connection status to consumer components', () => {
      render(
        <AuthProvider>
          <RealtimeProvider forceMock={true}>
            <TestConsumer />
          </RealtimeProvider>
        </AuthProvider>,
      )

      expect(screen.getByTestId('connection-status')).toHaveTextContent('connected')
      expect(screen.getByTestId('connection-state')).toHaveTextContent('connected')
    })

    it('handles live event subscription via useRealtimeSubscription', () => {
      function SubscriptionConsumer() {
        const { emitMockEvent } = useRealtime()
        const [lastIncident, setLastIncident] = useState<string>('none')

        useRealtimeSubscription('IncidentCreated', (inc) => {
          setLastIncident(inc.title)
        })

        return (
          <div>
            <div data-testid="last-incident">{lastIncident}</div>
            <button
              type="button"
              data-testid="emit-btn"
              onClick={() => {
                emitMockEvent('IncidentCreated', {
                  id: 'INC-999',
                  incidentId: 'INC-999',
                  title: 'New Realtime Incident',
                  severity: 'high',
                  status: 'triggered',
                  triggeredAt: new Date().toISOString(),
                })
              }}
            >
              Emit
            </button>
          </div>
        )
      }

      render(
        <AuthProvider>
          <RealtimeProvider forceMock={true}>
            <SubscriptionConsumer />
          </RealtimeProvider>
        </AuthProvider>,
      )

      expect(screen.getByTestId('last-incident')).toHaveTextContent('none')

      act(() => {
        screen.getByTestId('emit-btn').click()
      })

      expect(screen.getByTestId('last-incident')).toHaveTextContent('New Realtime Incident')
    })
  })
})
