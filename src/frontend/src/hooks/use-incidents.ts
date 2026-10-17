import { useState, useCallback, useMemo } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/api/client'
import type { IncidentSummary } from '@/api/types'
import { useRealtimeSubscription } from '@/realtime/realtime-context'
import type {
  IncidentCreatedPayload,
  IncidentStatusChangedPayload,
} from '@/realtime/signalr-events'

export interface UseIncidentsFilterOptions {
  status?: string
  severity?: string
  service?: string
  search?: string
}

export function useIncidents(filters: UseIncidentsFilterOptions = {}) {
  const queryClient = useQueryClient()
  const [highlightedIncidentId, setHighlightedIncidentId] = useState<string | null>(null)

  const normalizedStatus = useMemo(() => {
    if (!filters.status || filters.status === 'all') return undefined
    return filters.status.split(',').map((s) => s.trim())
  }, [filters.status])

  const normalizedSeverity = useMemo(() => {
    if (!filters.severity || filters.severity === 'all') return undefined
    return filters.severity.split(',').map((s) => s.trim())
  }, [filters.severity])

  const normalizedService = useMemo(() => {
    if (!filters.service || filters.service === 'all') return undefined
    return filters.service.trim()
  }, [filters.service])

  const queryKey = useMemo(
    () => ['incidents', normalizedStatus, normalizedSeverity, normalizedService],
    [normalizedStatus, normalizedSeverity, normalizedService],
  )

  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey,
    queryFn: async () => {
      return apiClient.getIncidents({
        status: normalizedStatus,
        severity: normalizedSeverity,
        service: normalizedService,
      })
    },
  })

  // Handle incoming realtime IncidentCreated SignalR events
  const handleIncidentCreated = useCallback(
    (event: IncidentCreatedPayload) => {
      const incId = event.incidentId || event.id
      // Highlight the arriving incident for 1.2s per Brief §8
      setHighlightedIncidentId(incId)
      setTimeout(() => {
        setHighlightedIncidentId(null)
      }, 1200)

      // Announce arrival via accessibility live region
      const liveRegion = document.getElementById('incident-live-announcer')
      if (liveRegion) {
        liveRegion.textContent = `New ${event.severity} incident ${incId} created: ${event.title}`
      }

      // Optimistically update query cache
      queryClient.setQueriesData<{ incidents: IncidentSummary[]; total: number } | undefined>(
        { queryKey: ['incidents'] },
        (oldData) => {
          if (!oldData) return oldData
          const exists = oldData.incidents.some((i) => i.id === incId)
          if (exists) return oldData

          const newSummary: IncidentSummary = {
            id: incId,
            alert_id: `ALT-${incId}`,
            title: event.title,
            severity: event.severity,
            status: event.status,
            affected_services: ['auth-api'],
            triggered_at: event.triggeredAt || new Date().toISOString(),
            assigned_to: 'Incident Coordinator Agent',
            root_cause: null,
          }

          return {
            ...oldData,
            incidents: [newSummary, ...oldData.incidents],
            total: oldData.total + 1,
          }
        },
      )
    },
    [queryClient],
  )

  // Handle incoming realtime IncidentStatusChanged SignalR events
  const handleStatusChanged = useCallback(
    (event: IncidentStatusChangedPayload) => {
      queryClient.setQueriesData<{ incidents: IncidentSummary[]; total: number } | undefined>(
        { queryKey: ['incidents'] },
        (oldData) => {
          if (!oldData) return oldData
          return {
            ...oldData,
            incidents: oldData.incidents.map((inc) =>
              inc.id === event.incidentId ? { ...inc, status: event.status } : inc,
            ),
          }
        },
      )
    },
    [queryClient],
  )

  useRealtimeSubscription('IncidentCreated', handleIncidentCreated)
  useRealtimeSubscription('IncidentStatusChanged', handleStatusChanged)

  // Filter in-memory by search string if provided
  const filteredIncidents = useMemo(() => {
    const list = data?.incidents || []
    if (!filters.search || !filters.search.trim()) {
      return list
    }
    const q = filters.search.toLowerCase().trim()
    return list.filter(
      (inc) =>
        inc.id.toLowerCase().includes(q) ||
        inc.title.toLowerCase().includes(q) ||
        inc.affected_services.some((s) => s.toLowerCase().includes(q)),
    )
  }, [data?.incidents, filters.search])

  return {
    incidents: filteredIncidents,
    rawIncidents: data?.incidents || [],
    total: data?.total ?? filteredIncidents.length,
    isLoading,
    isError,
    error,
    refetch,
    highlightedIncidentId,
  }
}
