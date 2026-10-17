import { useEffect, useCallback } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/api/client'
import { useRealtime, useRealtimeSubscription } from '@/realtime/realtime-context'
import type { IncidentDetail } from '@/api/types'

import type {
  IncidentStatusChangedPayload,
  AgentActionCompletedPayload,
} from '@/realtime/signalr-events'

export function useIncidentDetail(incidentId: string | undefined) {
  const queryClient = useQueryClient()
  const { joinIncidentGroup, leaveIncidentGroup } = useRealtime()

  // 1. Fetch incident detail
  const incidentQuery = useQuery({
    queryKey: ['incident', incidentId],
    queryFn: async () => {
      if (!incidentId) throw new Error('Incident ID is required')
      return apiClient.getIncidentById(incidentId)
    },
    enabled: Boolean(incidentId),
  })

  // 2. Fetch incident audit events
  const eventsQuery = useQuery({
    queryKey: ['incident-events', incidentId],
    queryFn: async () => {
      if (!incidentId) throw new Error('Incident ID is required')
      return apiClient.getIncidentEvents(incidentId)
    },
    enabled: Boolean(incidentId),
  })

  // 3. Fetch remediation options
  const remediationQuery = useQuery({
    queryKey: ['incident-remediation', incidentId],
    queryFn: async () => {
      if (!incidentId) throw new Error('Incident ID is required')
      return apiClient.getIncidentRemediation(incidentId)
    },
    enabled: Boolean(incidentId),
  })

  // Join/leave SignalR incident group for realtime updates
  useEffect(() => {
    if (incidentId) {
      void joinIncidentGroup(incidentId)
      return () => {
        void leaveIncidentGroup(incidentId)
      }
    }
  }, [incidentId, joinIncidentGroup, leaveIncidentGroup])

  // Handle realtime status change
  const handleStatusChanged = useCallback(
    (event: IncidentStatusChangedPayload) => {
      if (event.incidentId === incidentId) {
        queryClient.setQueryData<IncidentDetail | undefined>(['incident', incidentId], (old) => {
          if (!old) return old
          return {
            ...old,
            status: event.status,
          }
        })
      }
    },
    [incidentId, queryClient],
  )

  // Handle agent action completed event
  const handleAgentAction = useCallback(
    (event: AgentActionCompletedPayload) => {
      if (event.incidentId === incidentId) {
        void eventsQuery.refetch()
        void incidentQuery.refetch()
      }
    },
    [incidentId, eventsQuery, incidentQuery],
  )

  useRealtimeSubscription('IncidentStatusChanged', handleStatusChanged)
  useRealtimeSubscription('AgentActionCompleted', handleAgentAction)

  return {
    incident: incidentQuery.data,
    events: eventsQuery.data?.events ?? incidentQuery.data?.events ?? [],
    remediation: remediationQuery.data,
    isLoading: incidentQuery.isLoading || eventsQuery.isLoading,
    isError: incidentQuery.isError || eventsQuery.isError,
    error: incidentQuery.error || eventsQuery.error,
    refetch: async () => {
      await Promise.all([
        incidentQuery.refetch(),
        eventsQuery.refetch(),
        remediationQuery.refetch(),
      ])
    },
  }
}
