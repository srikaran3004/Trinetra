import { useState } from 'react'
import { ShieldAlert, Clock, AlertTriangle, Radio, Search, RefreshCw, Bug } from 'lucide-react'
import { ApprovalCard } from '@/components/approvals/approval-card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { TextField } from '@/components/ui/text-field'
import { SegmentedControl } from '@/components/ui/segmented-control'
import { Skeleton } from '@/components/ui/skeleton'
import { EmptyState } from '@/components/ui/empty-state'
import { ErrorState } from '@/components/ui/error-state'
import { useApprovals, type ApprovalFilterStatus } from '@/hooks/use-approvals'
import { useRealtime } from '@/realtime/realtime-context'
import { mockSignalREmitter } from '@/realtime/mock-signalr-emitter'

export function ApprovalsPage() {
  const [statusFilter, setStatusFilter] = useState<ApprovalFilterStatus>('pending')
  const [searchQuery, setSearchQuery] = useState('')

  const { isConnected } = useRealtime()

  const {
    approvals,
    metrics,
    isLoading,
    isError,
    error,
    refetch,
    actionError,
    clearActionError,
    simulateFailure,
    setSimulateFailure,
    approve,
    deny,
    isApproving,
    isDenying,
  } = useApprovals({
    statusFilter,
    searchQuery,
  })

  // Developer trigger: Simulate a new approval required over SignalR
  const handleSimulateApprovalEvent = () => {
    const incId = `INC-2024-${Math.floor(1000 + Math.random() * 9000)}`
    mockSignalREmitter.emit('ApprovalRequired', {
      incidentId: incId,
      eventId: `evt-sim-${Date.now()}`,
      actionDescription: 'Scale out api-gateway replicas from 4 to 12',
      toolCall: 'scale_deployment',
      timeoutMinutes: 15,
      timestamp: new Date().toISOString(),
    })
  }

  return (
    <div className="p-[var(--space-6)] max-w-7xl mx-auto space-y-[var(--space-6)]">
      {/* Page Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2.5">
            <h2 className="text-[var(--text-xl)] font-bold text-[var(--color-text-primary)] m-0">
              Remediation Approval Queue
            </h2>
            <Badge variant={isConnected ? 'success' : 'neutral'} size="sm" className="gap-1.5">
              <Radio
                size={11}
                className={isConnected ? 'animate-pulse text-[var(--color-success)]' : ''}
              />
              {isConnected ? 'Live SignalR' : 'Offline'}
            </Badge>
          </div>
          <p className="text-[var(--text-sm)] text-[var(--color-text-secondary)] mt-1">
            Human-in-the-loop verification gate for high and medium-risk autonomous remediation
            plans (Brief §7.6).
          </p>
        </div>

        {/* Header Metric Counters */}
        <div className="flex items-center gap-2 flex-wrap">
          <Badge
            variant={metrics.pendingCount > 0 ? 'high' : 'neutral'}
            size="md"
            className="gap-1.5 px-3 py-1 font-mono"
            data-testid="pending-counter-badge"
          >
            <Clock size={13} />
            <span>{metrics.pendingCount} Pending</span>
          </Badge>

          {metrics.expiringSoonCount > 0 && (
            <Badge
              variant="critical"
              size="md"
              className="gap-1.5 px-3 py-1 font-mono animate-pulse"
              data-testid="expiring-counter-badge"
            >
              <AlertTriangle size={13} />
              <span>{metrics.expiringSoonCount} Expiring &lt;5m</span>
            </Badge>
          )}

          <Badge variant="outline" size="md" className="gap-1.5 px-3 py-1 font-mono">
            <ShieldAlert size={13} className="text-[var(--color-critical)]" />
            <span>{metrics.highRiskCount} High Risk</span>
          </Badge>
        </div>
      </div>

      {/* Filter and Control Toolbar */}
      <div className="flex flex-col md:flex-row items-stretch md:items-center justify-between gap-3 p-3 bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)] rounded-[var(--radius-md)]">
        {/* Status Filter Segmented Control */}
        <div className="flex items-center gap-2">
          <SegmentedControl
            aria-label="Filter approvals by status"
            options={[
              { id: 'pending', label: `Pending (${metrics.pendingCount})` },
              { id: 'all', label: `All (${metrics.total})` },
              { id: 'history', label: 'History' },
            ]}
            value={statusFilter}
            onChange={(val: string) => {
              setStatusFilter(val as ApprovalFilterStatus)
            }}
            segmentSize="sm"
          />
        </div>

        {/* Search Input */}
        <div className="flex items-center gap-2 flex-1 max-w-md">
          <TextField
            placeholder="Search by incident, command, action..."
            value={searchQuery}
            onChange={setSearchQuery}
            inputSize="sm"
            aria-label="Search approvals"
            prefixIcon={<Search size={14} className="text-[var(--color-text-tertiary)]" />}
          />
        </div>

        {/* Dev Triggers for Testing */}
        <div className="flex items-center gap-2 self-end md:self-center shrink-0">
          <Button
            variant="ghost"
            size="sm"
            onPress={() => {
              void refetch()
            }}
            aria-label="Refresh approval queue"
          >
            <RefreshCw size={14} />
          </Button>

          <Button
            variant={simulateFailure ? 'danger' : 'secondary'}
            size="sm"
            onPress={() => {
              setSimulateFailure(!simulateFailure)
            }}
            aria-label="Toggle simulated network error to verify optimistic rollback and InlineAlert"
            data-testid="simulate-failure-toggle"
          >
            <Bug size={13} />
            <span className="text-xs">
              {simulateFailure ? 'Rollback Sim: ON' : 'Rollback Sim: OFF'}
            </span>
          </Button>

          <Button
            variant="secondary"
            size="sm"
            onPress={handleSimulateApprovalEvent}
            data-testid="simulate-approval-button"
          >
            Simulate Event
          </Button>
        </div>
      </div>

      {/* Main Content Area */}
      {isLoading ? (
        <div className="space-y-[var(--space-4)]" data-testid="approvals-skeleton">
          <Skeleton className="h-44 w-full rounded-[var(--radius-md)]" />
          <Skeleton className="h-44 w-full rounded-[var(--radius-md)]" />
        </div>
      ) : isError ? (
        <ErrorState
          title="Failed to Load Approval Queue"
          message={
            error instanceof Error ? error.message : 'Unable to connect to Trinetra API gateway.'
          }
          onRetry={() => {
            void refetch()
          }}
        />
      ) : approvals.length === 0 ? (
        <EmptyState
          title={statusFilter === 'pending' ? 'No Pending Approvals' : 'No Approvals Found'}
          description={
            searchQuery
              ? `No approval items match "${searchQuery}". Try clearing search filters.`
              : statusFilter === 'pending'
                ? 'All autonomous remediation proposals have been reviewed or auto-resolved.'
                : 'No approvals recorded in this filter view.'
          }
          action={
            searchQuery ? (
              <Button
                variant="secondary"
                size="sm"
                onPress={() => {
                  setSearchQuery('')
                }}
              >
                Clear Search
              </Button>
            ) : undefined
          }
        />
      ) : (
        <div className="space-y-[var(--space-4)]" data-testid="approvals-list">
          {approvals.map((approval) => (
            <ApprovalCard
              key={approval.id}
              approval={approval}
              onApprove={async (id) => approve({ approvalId: id })}
              onDeny={async (id, reason) => deny({ approvalId: id, reason })}
              isApproving={isApproving}
              isDenying={isDenying}
              errorMessage={actionError?.approvalId === approval.id ? actionError.message : null}
              onClearError={clearActionError}
            />
          ))}
        </div>
      )}
    </div>
  )
}
