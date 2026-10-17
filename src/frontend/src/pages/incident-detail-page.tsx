import { useParams, useNavigate } from 'react-router'
import { ArrowLeft, Clock, Layers, Zap } from 'lucide-react'
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Avatar } from '@/components/ui/avatar'
import { Skeleton } from '@/components/ui/skeleton'
import { ErrorState } from '@/components/ui/error-state'
import { RelativeTime } from '@/components/ui/relative-time'
import { StateProgress } from '@/components/ui/state-progress'
import { RootCauseCard } from '@/components/incidents/root-cause-card'
import { RemediationCard } from '@/components/incidents/remediation-card'
import { AgentTimeline } from '@/components/incidents/agent-timeline'
import { DiagnosticSnapshots } from '@/components/incidents/diagnostic-snapshots'
import { useIncidentDetail } from '@/hooks/use-incident-detail'

export function IncidentDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()

  const { incident, events, remediation, isLoading, isError, error } = useIncidentDetail(id)

  if (isLoading) {
    return (
      <div className="p-4 sm:p-6 max-w-7xl mx-auto space-y-6">
        <Skeleton className="h-10 w-48" />
        <Skeleton className="h-28 w-full" />
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2 space-y-6">
            <Skeleton className="h-64 w-full" />
            <Skeleton className="h-64 w-full" />
          </div>
          <div className="space-y-6">
            <Skeleton className="h-48 w-full" />
            <Skeleton className="h-48 w-full" />
          </div>
        </div>
      </div>
    )
  }

  if (isError || !incident) {
    return (
      <div className="p-4 sm:p-6 max-w-7xl mx-auto">
        <ErrorState
          title={`Incident ${id || ''} not found`}
          description={
            error?.message ||
            'The requested incident record could not be retrieved from the incident repository.'
          }
          actionText="Back to Incidents"
          onRetry={() => {
            void navigate('/incidents')
          }}
        />
      </div>
    )
  }

  return (
    <div className="p-4 sm:p-6 max-w-7xl mx-auto space-y-6">
      {/* Back button and navigation breadcrumb */}
      <div>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => {
            void navigate('/incidents')
          }}
          className="gap-1.5 text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] -ml-2"
        >
          <ArrowLeft size={14} />
          <span>Back to Incidents</span>
        </Button>
      </div>

      {/* Incident Header Card */}
      <Card className="p-5 sm:p-6 bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)] space-y-5">
        <div className="flex flex-col md:flex-row md:items-start justify-between gap-4">
          <div className="space-y-2">
            <div className="flex flex-wrap items-center gap-2.5">
              <span className="font-mono text-[var(--text-sm)] font-bold text-[var(--color-accent-text)] bg-[color-mix(in_srgb,var(--color-accent)_12%,transparent)] px-2 py-0.5 rounded-[var(--radius-xs)] border border-[color-mix(in_srgb,var(--color-accent)_30%,transparent)]">
                {incident.id}
              </span>
              <Badge severity={incident.severity} size="sm" />
              <Badge incidentState={incident.status} size="sm" />
            </div>

            <h2 className="text-[var(--text-xl)] sm:text-[var(--text-2xl)] font-bold text-[var(--color-text-primary)] tracking-tight leading-snug m-0">
              {incident.title}
            </h2>

            {/* Sub-header Metadata */}
            <div className="flex flex-wrap items-center gap-4 text-[var(--text-xs)] text-[var(--color-text-secondary)] pt-1">
              <div className="flex items-center gap-1.5 font-mono">
                <Clock size={13} className="text-[var(--color-text-tertiary)]" />
                <span>Triggered:</span>
                <RelativeTime date={incident.triggered_at} className="font-semibold" />
              </div>

              <div className="flex items-center gap-1.5">
                <Avatar
                  isAgent={
                    incident.assigned_to?.includes('Agent') ||
                    incident.assigned_to === 'Auto-Executor'
                  }
                  size="sm"
                />
                <span>Assigned:</span>
                <span className="font-medium text-[var(--color-text-primary)]">
                  {incident.assigned_to || 'Autonomous Coordinator'}
                </span>
              </div>
            </div>
          </div>

          {/* Quick CTA if awaiting approval */}
          {incident.status === 'awaiting_approval' && (
            <div className="shrink-0 flex items-center">
              <Button
                variant="primary"
                onClick={() => {
                  void navigate('/approvals')
                }}
                className="gap-2 shadow-sm font-semibold"
              >
                <span>Review Approval Gate</span>
                <Zap size={14} />
              </Button>
            </div>
          )}
        </div>

        {/* StateProgress Stepper (§7.4) */}
        <div className="pt-4 border-t border-[var(--color-border-subtle)]">
          <span className="text-[11px] font-mono uppercase tracking-wider text-[var(--color-text-tertiary)] block mb-2">
            Incident Lifecycle Progression
          </span>
          <StateProgress currentStatus={incident.status} />
        </div>
      </Card>

      {/* Main Grid: Details + Audit + Snapshots */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Left Column (2 cols): Root Cause, Remediation Plans, Agent Timeline */}
        <div className="lg:col-span-2 space-y-6">
          {/* Root Cause Card with ConfidenceBar */}
          <RootCauseCard incident={incident} />

          {/* Proposed Remediation Plans with Backend Verdicts */}
          <RemediationCard options={remediation?.options || []} incidentId={incident.id} />

          {/* Autonomous Agent Timeline with distinct Guardrail Styling */}
          <AgentTimeline events={events} />
        </div>

        {/* Right Column (1 col): Incident Scope & Diagnostic Snapshots */}
        <div className="space-y-6">
          {/* Incident Scope Panel */}
          <Card className="bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)]">
            <CardHeader className="pb-3 border-b border-[var(--color-border-subtle)]">
              <div className="flex items-center gap-2">
                <Layers size={18} className="text-[var(--color-accent)]" />
                <CardTitle>Impacted Scope</CardTitle>
              </div>
            </CardHeader>
            <CardContent className="pt-4 space-y-3.5 text-[var(--text-xs)]">
              <div>
                <span className="text-[var(--color-text-tertiary)] block uppercase tracking-wider text-[10px] font-mono mb-1">
                  Alert Source ID
                </span>
                <span className="font-mono text-[var(--color-text-primary)] font-medium">
                  {incident.alert_id}
                </span>
              </div>

              <div>
                <span className="text-[var(--color-text-tertiary)] block uppercase tracking-wider text-[10px] font-mono mb-1">
                  Affected Services
                </span>
                <div className="flex flex-wrap gap-1.5">
                  {incident.affected_services.map((svc) => (
                    <span
                      key={svc}
                      className="px-2 py-0.5 rounded-[var(--radius-xs)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] font-mono text-[var(--color-text-primary)] font-medium"
                    >
                      {svc}
                    </span>
                  ))}
                </div>
              </div>

              {incident.affected_hosts.length > 0 && (
                <div>
                  <span className="text-[var(--color-text-tertiary)] block uppercase tracking-wider text-[10px] font-mono mb-1">
                    Affected Infrastructure Hosts
                  </span>
                  <div className="flex flex-wrap gap-1.5">
                    {incident.affected_hosts.map((host) => (
                      <span
                        key={host}
                        className="px-2 py-0.5 rounded-[var(--radius-xs)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] font-mono text-[var(--color-text-secondary)]"
                      >
                        {host}
                      </span>
                    ))}
                  </div>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Diagnostic Telemetry, Logs & Linked Runbooks */}
          <DiagnosticSnapshots incident={incident} />
        </div>
      </div>
    </div>
  )
}
