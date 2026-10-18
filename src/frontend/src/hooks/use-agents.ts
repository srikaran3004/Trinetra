import { useCallback, useMemo } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { apiClient } from '@/api/client'
import { useRealtimeSubscription } from '@/realtime/realtime-context'
import type { Agent, AgentStatus, AgentsResponse } from '@/api/types'
import type { AgentActionCompletedPayload } from '@/realtime/signalr-events'

export interface UseAgentsFilterOptions {
  status?: string // 'in-progress' | 'all' | 'working' | 'waiting' | 'idle' | 'failed'
  search?: string
  incidentId?: string
}

export function useAgents(filters: UseAgentsFilterOptions = {}) {
  const queryClient = useQueryClient()
  const statusFilter = filters.status || 'in-progress'
  const searchFilter = filters.search?.toLowerCase().trim() || ''
  const incidentFilter = filters.incidentId?.trim() || ''

  // 1. Fetch agents from REST API
  const { data, isLoading, isError, error, refetch } = useQuery<AgentsResponse>({
    queryKey: ['agents'],
    queryFn: async () => apiClient.getAgents(),
  })

  // 2. Realtime SignalR update handler
  const handleAgentAction = useCallback(
    (event: AgentActionCompletedPayload) => {
      queryClient.setQueryData<AgentsResponse | undefined>(['agents'], (oldData) => {
        if (!oldData) return oldData

        const updatedAgents = oldData.agents.map((agent) => {
          const isMatch =
            agent.name.toLowerCase() === event.agentName.toLowerCase() ||
            agent.id.toLowerCase() === event.agentName.toLowerCase()

          if (!isMatch) return agent

          const isBlocked = event.status === 'blocked'
          const isWaiting =
            event.toolCall?.includes('approval') ||
            event.actionDescription.toLowerCase().includes('approval')

          let newStatus: AgentStatus = agent.status
          if (isBlocked || event.status === 'failure') {
            newStatus = 'failed'
          } else if (isWaiting) {
            newStatus = 'waiting'
          } else if (event.status === 'success' || event.status === 'working') {
            newStatus = 'working'
          }

          return {
            ...agent,
            status: newStatus,
            current_tool_call: event.toolCall || event.actionDescription,
            current_incident_id: event.incidentId || agent.current_incident_id,
            last_active_at: event.timestamp || new Date().toISOString(),
            elapsed_time_seconds: 0, // Reset elapsed for fresh tool call
            guardrail_violations_count: isBlocked
              ? agent.guardrail_violations_count + 1
              : agent.guardrail_violations_count,
          }
        })

        return {
          ...oldData,
          agents: updatedAgents,
        }
      })
    },
    [queryClient],
  )

  useRealtimeSubscription('AgentActionCompleted', handleAgentAction)

  // 3. Filter agents based on URL criteria
  const rawAgents: Agent[] = useMemo(() => data?.agents || [], [data?.agents])

  const filteredAgents = useMemo(() => {
    return rawAgents.filter((agent) => {
      // Status filter
      if (statusFilter === 'in-progress') {
        // In-progress means agent is currently working or waiting on an incident
        const isInProgress =
          agent.status === 'working' ||
          agent.status === 'waiting' ||
          Boolean(agent.current_incident_id)
        if (!isInProgress) return false
      } else if (statusFilter !== 'all') {
        if (agent.status !== statusFilter) return false
      }

      // Incident filter
      if (incidentFilter && agent.current_incident_id !== incidentFilter) {
        return false
      }

      // Search text filter across name and tool call
      if (searchFilter) {
        const nameMatch = agent.name.toLowerCase().includes(searchFilter)
        const toolMatch = agent.current_tool_call?.toLowerCase().includes(searchFilter) ?? false
        const incidentMatch =
          agent.current_incident_id?.toLowerCase().includes(searchFilter) ?? false
        if (!nameMatch && !toolMatch && !incidentMatch) {
          return false
        }
      }

      return true
    })
  }, [rawAgents, statusFilter, incidentFilter, searchFilter])

  // Aggregate metrics
  const metrics = useMemo(() => {
    const workingCount = rawAgents.filter((a) => a.status === 'working').length
    const waitingCount = rawAgents.filter((a) => a.status === 'waiting').length
    const idleCount = rawAgents.filter((a) => a.status === 'idle').length
    const failedCount = rawAgents.filter((a) => a.status === 'failed').length
    const inProgressCount = rawAgents.filter(
      (a) => a.status === 'working' || a.status === 'waiting' || Boolean(a.current_incident_id),
    ).length
    const totalViolations = rawAgents.reduce((sum, a) => sum + a.guardrail_violations_count, 0)

    return {
      total: rawAgents.length,
      working: workingCount,
      waiting: waitingCount,
      idle: idleCount,
      failed: failedCount,
      inProgress: inProgressCount,
      totalViolations,
    }
  }, [rawAgents])

  return {
    agents: filteredAgents,
    rawAgents,
    metrics,
    isLoading,
    isError,
    error,
    refetch,
  }
}
