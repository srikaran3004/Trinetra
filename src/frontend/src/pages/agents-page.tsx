import { useState, useEffect, useCallback } from 'react'
import { useSearchParams } from 'react-router'
import { Search, FilterX, Bot, Zap, ShieldAlert, Activity, Hourglass } from 'lucide-react'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { EmptyState } from '@/components/ui/empty-state'
import { ErrorState } from '@/components/ui/error-state'
import { AgentCard } from '@/components/agents/agent-card'
import { useAgents } from '@/hooks/use-agents'
import { mockSignalREmitter } from '@/realtime/mock-signalr-emitter'

const STATUS_FILTER_OPTIONS = [
  { label: 'In-Progress (Default)', value: 'in-progress' },
  { label: 'All Fleet Agents', value: 'all' },
  { label: 'Working', value: 'working' },
  { label: 'Waiting on Gate', value: 'waiting' },
  { label: 'Idle / Standby', value: 'idle' },
  { label: 'Interrupted / Failed', value: 'failed' },
]

export function AgentsPage() {
  const [searchParams, setSearchParams] = useSearchParams()

  // Default to 'in-progress' per Brief §7.5
  const statusParam = searchParams.get('status') || 'in-progress'
  const searchParam = searchParams.get('search') || ''
  const incidentParam = searchParams.get('incident') || ''

  const [searchInput, setSearchInput] = useState(searchParam)

  const { agents, rawAgents, metrics, isLoading, isError, error, refetch } = useAgents({
    status: statusParam,
    search: searchParam,
    incidentId: incidentParam,
  })

  // Filter setters updating URL search parameters
  const updateFilter = useCallback(
    (key: string, value: string) => {
      const nextParams = new URLSearchParams(searchParams)
      if (!value || (key === 'status' && value === 'in-progress')) {
        nextParams.delete(key)
      } else {
        nextParams.set(key, value)
      }
      setSearchParams(nextParams, { replace: true })
    },
    [searchParams, setSearchParams],
  )

  const clearAllFilters = useCallback(() => {
    setSearchInput('')
    setSearchParams(new URLSearchParams(), { replace: true })
  }, [setSearchParams])

  const hasActiveFilters =
    statusParam !== 'in-progress' || searchParam !== '' || incidentParam !== ''

  // Sync debounced search input to URL query
  useEffect(() => {
    const timeout = setTimeout(() => {
      if (searchInput !== searchParam) {
        updateFilter('search', searchInput)
      }
    }, 250)
    return () => {
      clearTimeout(timeout)
    }
  }, [searchInput, searchParam, updateFilter])

  // Dev simulation trigger to demonstrate realtime agent activity
  const triggerSimulatedAction = () => {
    const randomAgent = rawAgents[Math.floor(Math.random() * rawAgents.length)]
    if (!randomAgent) return

    mockSignalREmitter.emit('AgentActionCompleted', {
      incidentId: randomAgent.current_incident_id || 'INC-2024-8901',
      agentName: randomAgent.name,
      actionDescription: `Executed automated metric scrape on pod ${randomAgent.id}-cluster`,
      toolCall: `scrape_container_metrics(pod="${randomAgent.id}-worker")`,
      status: 'working',
      timestamp: new Date().toISOString(),
    })
  }

  // Dev simulation trigger for guardrail interception
  const triggerSimulatedGuardrailViolation = () => {
    mockSignalREmitter.emit('AgentActionCompleted', {
      incidentId: 'INC-2024-8901',
      agentName: 'Auto-Executor',
      actionDescription: 'BLOCKED: Attempted unauthorized root directory write',
      toolCall: 'intercept_command("rm -rf /var/run/temp/*")',
      status: 'blocked',
      timestamp: new Date().toISOString(),
    })
  }

  return (
    <div className="p-4 sm:p-6 max-w-7xl mx-auto space-y-6">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2.5">
            <h2 className="text-[var(--text-xl)] font-bold text-[var(--color-text-primary)] m-0 tracking-tight">
              Autonomous Agent Fleet
            </h2>
            <div className="flex items-center gap-1.5 px-2 py-0.5 rounded-[var(--radius-full)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] text-[10px] text-[var(--color-text-secondary)] font-mono">
              <span className="w-1.5 h-1.5 rounded-full bg-[var(--color-success)] animate-pulse" />
              <span>SIGNALR LIVE</span>
            </div>
          </div>
          <p className="text-[var(--text-sm)] text-[var(--color-text-secondary)] mt-1">
            Real-time status, active tool calls, execution telemetry, and guardrail enforcement
            across all 8 specialized agents.
          </p>
        </div>

        {/* Fleet Metrics & Simulation CTAs */}
        <div className="flex items-center flex-wrap gap-2">
          <Badge variant="success" size="sm" className="gap-1 font-medium">
            <Activity size={12} />
            <span>{metrics.working} Working</span>
          </Badge>
          <Badge variant="medium" size="sm" className="gap-1 font-medium">
            <Hourglass size={12} />
            <span>{metrics.waiting} Waiting</span>
          </Badge>
          <Badge variant="neutral" size="sm" className="gap-1 font-medium">
            <Bot size={12} />
            <span>{metrics.idle} Idle</span>
          </Badge>
          {metrics.totalViolations > 0 && (
            <Badge variant="critical" size="sm" className="gap-1 font-semibold">
              <ShieldAlert size={12} />
              <span>{metrics.totalViolations} Guardrail Violations</span>
            </Badge>
          )}

          {import.meta.env.VITE_MOCK_API !== 'false' && (
            <div className="flex items-center gap-1.5 ml-2">
              <Button
                variant="secondary"
                size="sm"
                onClick={triggerSimulatedAction}
                className="gap-1 text-xs"
                aria-label="Simulate agent action"
              >
                <Zap size={12} className="text-[var(--color-accent-text)]" />
                <span>Simulate Tool Call</span>
              </Button>
              <Button
                variant="secondary"
                size="sm"
                onClick={triggerSimulatedGuardrailViolation}
                className="gap-1 text-xs text-[var(--color-critical)]"
                aria-label="Simulate guardrail violation"
              >
                <ShieldAlert size={12} />
                <span>Simulate Blocked</span>
              </Button>
            </div>
          )}
        </div>
      </div>

      {/* Filter Toolbar (State lives in URL) */}
      <Card className="p-3 sm:p-4 bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)]">
        <div className="flex flex-col sm:flex-row sm:items-center gap-3 justify-between">
          <div className="flex flex-wrap items-center gap-2.5 flex-1">
            {/* Search Input */}
            <div className="relative min-w-[220px] flex-1 sm:max-w-xs">
              <Search
                size={14}
                className="absolute left-3 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)] pointer-events-none"
              />
              <input
                type="text"
                value={searchInput}
                onChange={(e) => {
                  setSearchInput(e.target.value)
                }}
                placeholder="Search agents, tool calls..."
                aria-label="Search agents"
                className="w-full h-9 pl-9 pr-3 rounded-[var(--radius-sm)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] text-[var(--text-xs)] text-[var(--color-text-primary)] placeholder:text-[var(--color-text-tertiary)] focus:outline-none focus:ring-1 focus:ring-[var(--color-focus-ring)] transition-colors"
              />
            </div>

            {/* Status Filter Dropdown */}
            <select
              value={statusParam}
              onChange={(e) => {
                updateFilter('status', e.target.value)
              }}
              aria-label="Filter agents by status"
              className="h-9 px-3 rounded-[var(--radius-sm)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] text-[var(--text-xs)] text-[var(--color-text-primary)] focus:outline-none focus:ring-1 focus:ring-[var(--color-focus-ring)] cursor-pointer"
            >
              {STATUS_FILTER_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>

            {/* Clear Filters Button */}
            {hasActiveFilters && (
              <Button
                variant="ghost"
                size="sm"
                onClick={clearAllFilters}
                className="gap-1 text-[var(--text-xs)] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] h-9"
              >
                <FilterX size={13} />
                <span>Reset to In-Progress</span>
              </Button>
            )}
          </div>

          {/* Active view indicator */}
          <div className="text-[var(--text-xs)] text-[var(--color-text-tertiary)] font-mono">
            {statusParam === 'in-progress' ? (
              <span className="flex items-center gap-1.5">
                <span className="w-2 h-2 rounded-full bg-[var(--color-accent)]" />
                <span>Showing in-progress incident agents</span>
              </span>
            ) : (
              <span>
                Showing {agents.length} of {rawAgents.length} agents
              </span>
            )}
          </div>
        </div>
      </Card>

      {/* Main Content: Agent Cards Grid */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {Array.from({ length: 6 }).map((_, i) => (
            <Card
              key={i}
              className="p-4 space-y-3 bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)]"
            >
              <div className="flex items-center justify-between">
                <Skeleton className="h-6 w-36" />
                <Skeleton className="h-5 w-20" />
              </div>
              <Skeleton className="h-14 w-full" />
              <div className="flex justify-between">
                <Skeleton className="h-4 w-24" />
                <Skeleton className="h-4 w-20" />
              </div>
            </Card>
          ))}
        </div>
      ) : isError ? (
        <ErrorState
          title="Failed to load agent fleet"
          description={error?.message || 'Could not connect to the Trinetra agent runtime.'}
          onRetry={() => {
            void refetch()
          }}
        />
      ) : agents.length === 0 ? (
        <EmptyState
          title="No agents match filter"
          description={
            statusParam === 'in-progress'
              ? 'No autonomous agents are currently executing tools for in-progress incidents. Switch to "All Fleet Agents" to inspect idle agents.'
              : 'No agents found matching your current filter criteria. Try resetting your search filters.'
          }
          action={
            <Button
              variant="secondary"
              onClick={() => {
                updateFilter('status', 'all')
              }}
            >
              View All Fleet Agents
            </Button>
          }
        />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {agents.map((agent) => (
            <AgentCard key={agent.id} agent={agent} />
          ))}
        </div>
      )}
    </div>
  )
}
