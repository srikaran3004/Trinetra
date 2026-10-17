import { useState, useEffect, useCallback, useMemo } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/api/client'
import type { IncidentSummary, IncidentSeverity, IncidentStatus } from '@/api/types'
import { useRealtimeSubscription } from '@/realtime/realtime-context'

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
    (event: {
      incident_id: string
      title: string
      severity: string
      service: string
      timestamp: string
    }) => {
      // Highlight the arriving incident for 1.2s per Brief §8
      setHighlightedIncidentId(event.incident_id)
      const timer = setTimeout(() => {
        setHighlightedIncidentId(null)
      }, 1200)

      // Announce arrival via accessibility live region
      const liveRegion = document.getElementById('incident-live-announcer')
      if (liveRegion) {
        liveRegion.textContent = `New ${event.severity} incident ${event.incident_id} created: ${event.title}`
      }

      // Optimistically update query cache
      queryClient.setQueriesData<
        { incidents: IncidentSummary[]; total: number } | undefined
      >(
        { queryKey: ['incidents'] },
        (oldData) => {
          if (!oldData) return oldData
          const exists = oldData.incidents.some((i) => i.id === event.incident_id)
          if (exists) return oldData

          const newSummary: IncidentSummary = {
            id: event.incident_id,
            title: event.title,
            severity: event.severity as IncidentSeverity,
            status: 'triggered',
            affected_services: [event.service],
            triggered_at: event.timestamp || new Date().toISOString(),
            assigned_to: 'Incident Coordinator Agent',
          }

          return {
            ...oldData,
            incidents: [newSummary, ...oldData.incidents],
            total: oldData.total + 1,
          }
        },
      )

      return () => clearTimeout(timer)
    },
    [queryClient],
  )

  // Handle incoming realtime IncidentStatusChanged SignalR events
  const handleStatusChanged = useCallback(
    (event: { incident_id: string; previous_status: string; new_status: string }) => {
      queryClient.setQueriesData<
        { incidents: IncidentSummary[]; total: number } | undefined
      >(
        { queryKey: ['incidents'] },
        (oldData) => {
          if (!oldData) return oldData
          return {
            ...oldData,
            incidents: oldData.incidents.map((inc) =>
              inc.id === event.incident_id
                ? { ...inc, status: event.new_status as IncidentStatus }
                : inc,
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
