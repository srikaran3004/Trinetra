import { useState, useEffect, useRef, useMemo, useCallback } from 'react'
import { useNavigate, useSearchParams } from 'react-router'
import {
  Search,
  FilterX,
  Radio,
  PlusCircle,
  Clock,
  CheckCircle2,
  AlertTriangle,
  Zap,
} from 'lucide-react'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Avatar } from '@/components/ui/avatar'
import { Skeleton } from '@/components/ui/skeleton'
import { EmptyState } from '@/components/ui/empty-state'
import { ErrorState } from '@/components/ui/error-state'
import { RelativeTime } from '@/components/ui/relative-time'
import { KeyboardShortcut } from '@/components/ui/keyboard-shortcut'
import {
  Table,
  TableHeader,
  TableBody,
  TableRow,
  TableHead,
  TableCell,
} from '@/components/ui/table'
import { useIncidents } from '@/hooks/use-incidents'
import { mockSignalREmitter } from '@/realtime/mock-signalr-emitter'
import { cn } from '@/lib/cn'
import type { IncidentSummary } from '@/api/types'

const STATUS_OPTIONS = [
  { label: 'All Statuses', value: 'all' },
  { label: 'Triggered', value: 'triggered' },
  { label: 'Investigating', value: 'investigating' },
  { label: 'Analysis Complete', value: 'analysis_complete' },
  { label: 'Awaiting Approval', value: 'awaiting_approval' },
  { label: 'Executing Remediation', value: 'executing_remediation' },
  { label: 'Monitoring Recovery', value: 'monitoring_recovery' },
  { label: 'Resolved', value: 'resolved' },
  { label: 'Escalated', value: 'escalated' },
]

const SEVERITY_OPTIONS = [
  { label: 'All Severities', value: 'all' },
  { label: 'Critical', value: 'critical' },
  { label: 'High', value: 'high' },
  { label: 'Medium', value: 'medium' },
  { label: 'Low', value: 'low' },
]

export function IncidentsPage() {
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()

  // URL-driven filter state per Brief §7.3
  const statusParam = searchParams.get('status') || 'all'
  const severityParam = searchParams.get('severity') || 'all'
  const serviceParam = searchParams.get('service') || 'all'
  const searchParam = searchParams.get('search') || ''

  const [searchInput, setSearchInput] = useState(searchParam)
  const [selectedIndex, setSelectedIndex] = useState<number>(0)
  const rowRefs = useRef<(HTMLElement | null)[]>([])

  const {
    incidents,
    rawIncidents,
    isLoading,
    isError,
    error,
    refetch,
    highlightedIncidentId,
  } = useIncidents({
    status: statusParam,
    severity: severityParam,
    service: serviceParam,
    search: searchParam,
  })

  // Extract distinct service names for filter dropdown
  const availableServices = useMemo(() => {
    const set = new Set<string>()
    rawIncidents.forEach((inc) => {
      inc.affected_services.forEach((s) => set.add(s))
    })
    return Array.from(set).sort()
  }, [rawIncidents])

  // Filter setters updating URL search parameters
  const updateFilter = useCallback(
    (key: string, value: string) => {
      const nextParams = new URLSearchParams(searchParams)
      if (!value || value === 'all') {
        nextParams.delete(key)
      } else {
        nextParams.set(key, value)
      }
      setSearchParams(nextParams, { replace: true })
      setSelectedIndex(0)
    },
    [searchParams, setSearchParams],
  )

  const clearAllFilters = useCallback(() => {
    setSearchInput('')
    setSearchParams(new URLSearchParams(), { replace: true })
    setSelectedIndex(0)
  }, [setSearchParams])

  const hasActiveFilters =
    statusParam !== 'all' ||
    severityParam !== 'all' ||
    serviceParam !== 'all' ||
    searchParam !== ''

  // Sync debounced search input to URL query
  useEffect(() => {
    const timeout = setTimeout(() => {
      if (searchInput !== searchParam) {
        updateFilter('search', searchInput)
      }
    }, 250)
    return () => clearTimeout(timeout)
  }, [searchInput, searchParam, updateFilter])

  // Reset selected index if incidents array shrinks
  useEffect(() => {
    if (selectedIndex >= incidents.length && incidents.length > 0) {
      setSelectedIndex(0)
    }
  }, [incidents.length, selectedIndex])

  // Keyboard navigation: j/k to move between rows, Enter to open detail view (§7.3)
  useEffect(() => {
    function handleKeyDown(e: KeyboardEvent) {
      // Ignore if focus is in an input, textarea or select
      const activeEl = document.activeElement
      if (
        activeEl instanceof HTMLInputElement ||
        activeEl instanceof HTMLTextAreaElement ||
        activeEl instanceof HTMLSelectElement
      ) {
        return
      }

      if (e.key === 'j' || e.key === 'ArrowDown') {
        e.preventDefault()
        setSelectedIndex((prev) => {
          const next = Math.min(prev + 1, incidents.length - 1)
          rowRefs.current[next]?.scrollIntoView?.({ block: 'nearest' })
          return next
        })
      } else if (e.key === 'k' || e.key === 'ArrowUp') {
        e.preventDefault()
        setSelectedIndex((prev) => {
          const next = Math.max(prev - 1, 0)
          rowRefs.current[next]?.scrollIntoView?.({ block: 'nearest' })
          return next
        })
      } else if (e.key === 'Enter') {
        if (incidents[selectedIndex]) {
          e.preventDefault()
          navigate(`/incidents/${incidents[selectedIndex].id}`)
        }
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [incidents, selectedIndex, navigate])

  // Count incidents by severity for quick header metrics
  const criticalCount = useMemo(
    () => rawIncidents.filter((i) => i.severity === 'critical' && i.status !== 'resolved').length,
    [rawIncidents],
  )
  const highCount = useMemo(
    () => rawIncidents.filter((i) => i.severity === 'high' && i.status !== 'resolved').length,
    [rawIncidents],
  )
  const activeCount = useMemo(
    () => rawIncidents.filter((i) => i.status !== 'resolved').length,
    [rawIncidents],
  )

  // Simulation trigger to demonstrate realtime arrival
  const triggerSimulatedIncident = () => {
    const idNum = Math.floor(1000 + Math.random() * 9000)
    mockSignalREmitter.emit('IncidentCreated', {
      incident_id: `INC-2024-${idNum}`,
      title: `Simulated memory pressure spike on worker-pool-${idNum % 4}`,
      severity: 'critical',
      service: `worker-service-0${(idNum % 3) + 1}`,
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
              Active Incidents
            </h2>
            <div className="flex items-center gap-1.5 px-2 py-0.5 rounded-[var(--radius-full)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] text-[10px] text-[var(--color-text-secondary)] font-mono">
              <span className="w-1.5 h-1.5 rounded-full bg-[var(--color-success)] animate-pulse" />
              <span>LIVE</span>
            </div>
          </div>
          <p className="text-[var(--text-sm)] text-[var(--color-text-secondary)] mt-1">
            Real-time feed of active system incidents under autonomous investigation and mitigation.
          </p>
        </div>

        {/* Quick Summary Badges & Dev Simulation Trigger */}
        <div className="flex items-center flex-wrap gap-2">
          <Badge severity="critical" size="sm">
            {criticalCount} Critical
          </Badge>
          <Badge severity="high" size="sm">
            {highCount} High
          </Badge>
          <Badge variant="neutral" size="sm">
            {activeCount} Active Total
          </Badge>

          {import.meta.env.VITE_MOCK_API !== 'false' && (
            <Button
              variant="secondary"
              size="sm"
              onClick={triggerSimulatedIncident}
              className="gap-1.5 text-xs ml-2"
              title="Dispatches a mock SignalR IncidentCreated event with 1.2s highlight animation"
            >
              <Zap size={13} className="text-[var(--color-accent-text)]" />
              <span>Simulate Incident</span>
            </Button>
          )}
        </div>
      </div>

      {/* Filter Toolbar (State lives in URL) */}
      <Card className="p-3 sm:p-4 bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)]">
        <div className="flex flex-col lg:flex-row lg:items-center gap-3 justify-between">
          <div className="flex flex-wrap items-center gap-2.5 flex-1">
            {/* Search Input */}
            <div className="relative min-w-[200px] flex-1 sm:max-w-xs">
              <Search
                size={14}
                className="absolute left-3 top-1/2 -translate-y-1/2 text-[var(--color-text-tertiary)] pointer-events-none"
              />
              <input
                type="text"
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                placeholder="Search incidents or ID..."
                aria-label="Search incidents"
                className="w-full h-9 pl-9 pr-3 rounded-[var(--radius-sm)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] text-[var(--text-xs)] text-[var(--color-text-primary)] placeholder:text-[var(--color-text-tertiary)] focus:outline-none focus:ring-1 focus:ring-[var(--color-focus-ring)] transition-colors"
              />
            </div>

            {/* Severity Filter */}
            <select
              value={severityParam}
              onChange={(e) => updateFilter('severity', e.target.value)}
              aria-label="Filter by severity"
              className="h-9 px-3 rounded-[var(--radius-sm)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] text-[var(--text-xs)] text-[var(--color-text-primary)] focus:outline-none focus:ring-1 focus:ring-[var(--color-focus-ring)] cursor-pointer"
            >
              {SEVERITY_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>

            {/* Status Filter */}
            <select
              value={statusParam}
              onChange={(e) => updateFilter('status', e.target.value)}
              aria-label="Filter by status"
              className="h-9 px-3 rounded-[var(--radius-sm)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] text-[var(--text-xs)] text-[var(--color-text-primary)] focus:outline-none focus:ring-1 focus:ring-[var(--color-focus-ring)] cursor-pointer"
            >
              {STATUS_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>

            {/* Service Filter */}
            <select
              value={serviceParam}
              onChange={(e) => updateFilter('service', e.target.value)}
              aria-label="Filter by service"
              className="h-9 px-3 rounded-[var(--radius-sm)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] text-[var(--text-xs)] text-[var(--color-text-primary)] focus:outline-none focus:ring-1 focus:ring-[var(--color-focus-ring)] cursor-pointer"
            >
              <option value="all">All Services</option>
              {availableServices.map((service) => (
                <option key={service} value={service}>
                  {service}
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
                <span>Clear</span>
              </Button>
            )}
          </div>

          {/* Keyboard Navigation Helper Hints */}
          <div className="hidden sm:flex items-center gap-3 text-[var(--text-xs)] text-[var(--color-text-tertiary)]">
            <span className="flex items-center gap-1">
              <KeyboardShortcut keys={['j']} />
              <KeyboardShortcut keys={['k']} />
              <span className="ml-0.5">navigate</span>
            </span>
            <span className="flex items-center gap-1">
              <KeyboardShortcut keys={['↵']} />
              <span className="ml-0.5">open</span>
            </span>
          </div>
        </div>
      </Card>

      {/* Main Content Area */}
      {isLoading ? (
        <Card className="p-4 space-y-3 bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)]">
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-14 w-full" />
          <Skeleton className="h-14 w-full" />
          <Skeleton className="h-14 w-full" />
          <Skeleton className="h-14 w-full" />
        </Card>
      ) : isError ? (
        <ErrorState
          title="Failed to load incidents"
          description={error?.message || 'Could not connect to the Trinetra incident service.'}
          onRetry={() => {
            void refetch()
          }}
        />
      ) : incidents.length === 0 ? (
        <EmptyState
          title="No incidents found"
          description={
            hasActiveFilters
              ? 'No incidents match your current filter criteria. Try adjusting or clearing your filters.'
              : 'All systems are operating normally. No active incidents under investigation.'
          }
          action={
            hasActiveFilters ? (
              <Button variant="secondary" onClick={clearAllFilters}>
                Clear filters
              </Button>
            ) : undefined
          }
        />
      ) : (
        <div className="space-y-4">
          {/* Desktop Table View (>= 768px) per Brief §7.3 */}
          <Card className="hidden md:block p-0 overflow-hidden bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)]">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-[120px]">Severity</TableHead>
                  <TableHead>Incident</TableHead>
                  <TableHead>Services</TableHead>
                  <TableHead className="w-[160px]">State</TableHead>
                  <TableHead className="w-[110px]" numeric>
                    Age
                  </TableHead>
                  <TableHead className="w-[180px]">Assignee</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {incidents.map((incident, index) => {
                  const isSelected = index === selectedIndex
                  const isHighlighted = highlightedIncidentId === incident.id

                  return (
                    <TableRow
                      key={incident.id}
                      ref={(el) => {
                        rowRefs.current[index] = el
                      }}
                      selected={isSelected}
                      onClick={() => navigate(`/incidents/${incident.id}`)}
                      className={cn(
                        'cursor-pointer select-none group',
                        isHighlighted && 'highlight-incident',
                      )}
                      tabIndex={0}
                      onFocus={() => setSelectedIndex(index)}
                      aria-label={`Incident ${incident.id}: ${incident.title}`}
                    >
                      {/* Severity */}
                      <TableCell>
                        <Badge severity={incident.severity} size="sm" />
                      </TableCell>

                      {/* Incident Title & ID */}
                      <TableCell>
                        <div className="flex flex-col gap-0.5">
                          <span className="font-medium text-[var(--text-sm)] text-[var(--color-text-primary)] group-hover:text-[var(--color-accent)] transition-colors line-clamp-1">
                            {incident.title}
                          </span>
                          <span className="font-mono text-[var(--text-xs)] text-[var(--color-text-tertiary)]">
                            {incident.id}
                          </span>
                        </div>
                      </TableCell>

                      {/* Affected Services */}
                      <TableCell>
                        <div className="flex flex-wrap items-center gap-1">
                          {incident.affected_services.map((svc) => (
                            <span
                              key={svc}
                              className="inline-block px-1.5 py-0.5 rounded-[var(--radius-xs)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] text-[10px] font-mono text-[var(--color-text-secondary)]"
                            >
                              {svc}
                            </span>
                          ))}
                        </div>
                      </TableCell>

                      {/* State */}
                      <TableCell>
                        <Badge incidentState={incident.status} size="sm" />
                      </TableCell>

                      {/* Age (Refreshes every 30s) */}
                      <TableCell numeric>
                        <RelativeTime
                          date={incident.triggered_at}
                          className="font-mono text-[var(--text-xs)] text-[var(--color-text-secondary)]"
                        />
                      </TableCell>

                      {/* Assignee */}
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <Avatar
                            isAgent={
                              incident.assigned_to?.includes('Agent') ||
                              incident.assigned_to === 'Auto-Executor'
                            }
                            initials={
                              incident.assigned_to?.includes('Agent')
                                ? undefined
                                : incident.assigned_to
                            }
                            size="sm"
                          />
                          <span className="text-[var(--text-xs)] text-[var(--color-text-secondary)] truncate max-w-[130px]">
                            {incident.assigned_to || 'Unassigned'}
                          </span>
                        </div>
                      </TableCell>
                    </TableRow>
                  )
                })}
              </TableBody>
            </Table>
          </Card>

          {/* Mobile Card Layout (< 768px) per Brief §7.3 */}
          <div className="grid gap-3 md:hidden">
            {incidents.map((incident, index) => {
              const isSelected = index === selectedIndex
              const isHighlighted = highlightedIncidentId === incident.id

              return (
                <Card
                  key={incident.id}
                  ref={(el) => {
                    rowRefs.current[index] = el
                  }}
                  onClick={() => navigate(`/incidents/${incident.id}`)}
                  className={cn(
                    'p-4 cursor-pointer min-h-[44px] transition-all duration-120',
                    'hover:border-[var(--color-border-strong)] active:bg-[var(--color-surface-pressed)]',
                    isSelected &&
                      'ring-2 ring-[var(--color-focus-ring)] bg-[var(--color-surface-hover)]/40',
                    isHighlighted && 'highlight-incident',
                  )}
                  tabIndex={0}
                  onFocus={() => setSelectedIndex(index)}
                  aria-label={`Incident ${incident.id}: ${incident.title}`}
                >
                  <div className="flex items-start justify-between gap-2 mb-2">
                    <div className="flex items-center gap-2">
                      <Badge severity={incident.severity} size="sm" />
                      <span className="font-mono text-[11px] text-[var(--color-text-tertiary)]">
                        {incident.id}
                      </span>
                    </div>
                    <RelativeTime
                      date={incident.triggered_at}
                      className="font-mono text-[11px] text-[var(--color-text-tertiary)]"
                    />
                  </div>

                  <h3 className="text-[var(--text-sm)] font-semibold text-[var(--color-text-primary)] mb-2 leading-snug">
                    {incident.title}
                  </h3>

                  <div className="flex flex-wrap items-center gap-1.5 mb-3">
                    {incident.affected_services.map((svc) => (
                      <span
                        key={svc}
                        className="px-1.5 py-0.5 rounded-[var(--radius-xs)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] text-[10px] font-mono text-[var(--color-text-secondary)]"
                      >
                        {svc}
                      </span>
                    ))}
                  </div>

                  <div className="flex items-center justify-between pt-2.5 border-t border-[var(--color-border-subtle)]">
                    <Badge incidentState={incident.status} size="sm" />
                    <div className="flex items-center gap-1.5 text-[var(--text-xs)] text-[var(--color-text-secondary)]">
                      <Avatar
                        isAgent={
                          incident.assigned_to?.includes('Agent') ||
                          incident.assigned_to === 'Auto-Executor'
                        }
                        size="sm"
                      />
                      <span className="truncate max-w-[120px]">
                        {incident.assigned_to || 'Unassigned'}
                      </span>
                    </div>
                  </div>
                </Card>
              )
            })}
          </div>

          {/* Results count footer */}
          <div className="flex items-center justify-between text-[var(--text-xs)] text-[var(--color-text-tertiary)] px-1">
            <span>
              Showing {incidents.length} of {rawIncidents.length} incidents
            </span>
            <span>Refreshes automatically via SignalR</span>
          </div>
        </div>
      )}
    </div>
  )
}
