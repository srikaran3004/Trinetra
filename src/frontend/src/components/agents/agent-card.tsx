import { Link } from 'react-router'
import {
  Bot,
  Terminal,
  Clock,
  ShieldAlert,
  ShieldCheck,
  AlertCircle,
  ExternalLink,
  Hourglass,
  Zap,
} from 'lucide-react'
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { StatusDot } from '@/components/ui/status-dot'
import { RelativeTime } from '@/components/ui/relative-time'
import { cn } from '@/lib/cn'
import type { Agent, AgentStatus } from '@/api/types'

export interface AgentCardProps {
  agent: Agent
  className?: string
}

function formatElapsed(seconds: number): string {
  if (seconds <= 0) return 'Just started'
  const mins = Math.floor(seconds / 60)
  const secs = seconds % 60
  if (mins === 0) return `${secs}s`
  return `${mins}m ${secs.toString().padStart(2, '0')}s`
}

function getStatusBadge(status: AgentStatus) {
  switch (status) {
    case 'working':
      return (
        <Badge variant="success" size="sm" className="gap-1.5 font-medium">
          <StatusDot status="success" size="sm" isPulsing />
          <span>Working</span>
        </Badge>
      )
    case 'waiting':
      return (
        <Badge variant="medium" size="sm" className="gap-1.5 font-medium">
          <StatusDot status="medium" size="sm" />
          <Hourglass size={11} className="animate-spin" />
          <span>Waiting on Gate</span>
        </Badge>
      )
    case 'failed':
      return (
        <Badge variant="critical" size="sm" className="gap-1.5 font-medium">
          <StatusDot status="critical" size="sm" />
          <AlertCircle size={11} />
          <span>Interrupted / Failed</span>
        </Badge>
      )
    case 'idle':
    default:
      return (
        <Badge variant="neutral" size="sm" className="gap-1.5 font-medium">
          <StatusDot status="neutral" size="sm" />
          <span>Idle</span>
        </Badge>
      )
  }
}

export function AgentCard({ agent, className }: AgentCardProps) {
  const isWorking = agent.status === 'working'
  const isWaiting = agent.status === 'waiting'
  const hasViolations = agent.guardrail_violations_count > 0

  return (
    <Card
      className={cn(
        'transition-all duration-120 bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)]',
        'hover:border-[var(--color-border-strong)]',
        isWorking && 'border-[color-mix(in_srgb,var(--color-success)_30%,transparent)]',
        isWaiting && 'border-[color-mix(in_srgb,var(--color-warning)_30%,transparent)]',
        agent.status === 'failed' &&
          'border-[color-mix(in_srgb,var(--color-critical)_40%,transparent)]',
        className,
      )}
      data-testid={`agent-card-${agent.id}`}
      aria-label={`Agent ${agent.name}: status ${agent.status}`}
    >
      <CardHeader className="pb-3 border-b border-[var(--color-border-subtle)]">
        <div className="flex items-start justify-between gap-3">
          <div className="flex items-center gap-2.5">
            <div
              className={cn(
                'w-8 h-8 rounded-[var(--radius-sm)] flex items-center justify-center shrink-0',
                isWorking &&
                  'bg-[color-mix(in_srgb,var(--color-success)_15%,transparent)] text-[var(--color-success)]',
                isWaiting &&
                  'bg-[color-mix(in_srgb,var(--color-warning)_15%,transparent)] text-[var(--color-warning)]',
                agent.status === 'failed' &&
                  'bg-[color-mix(in_srgb,var(--color-critical)_15%,transparent)] text-[var(--color-critical)]',
                agent.status === 'idle' &&
                  'bg-[var(--color-surface-hover)] text-[var(--color-text-secondary)]',
              )}
            >
              <Bot size={18} />
            </div>
            <div>
              <CardTitle className="text-[var(--text-sm)] font-semibold text-[var(--color-text-primary)]">
                {agent.name}
              </CardTitle>
              <span className="font-mono text-[11px] text-[var(--color-text-tertiary)] block">
                {agent.id}
              </span>
            </div>
          </div>

          <div className="shrink-0">{getStatusBadge(agent.status)}</div>
        </div>
      </CardHeader>

      <CardContent className="pt-3.5 space-y-3.5">
        {/* Active Incident Link if assigned */}
        {agent.current_incident_id ? (
          <div className="flex items-center justify-between text-[var(--text-xs)] p-2 rounded-[var(--radius-sm)] bg-[var(--color-surface-hover)]/60 border border-[var(--color-border-subtle)]">
            <span className="text-[var(--color-text-tertiary)] font-mono text-[11px]">
              Assigned Incident:
            </span>
            <Link
              to={`/incidents/${agent.current_incident_id}`}
              className="font-mono text-[var(--color-accent-text)] font-semibold hover:underline flex items-center gap-1 select-none"
            >
              <span>{agent.current_incident_id}</span>
              <ExternalLink size={11} />
            </Link>
          </div>
        ) : (
          <div className="flex items-center justify-between text-[var(--text-xs)] p-2 rounded-[var(--radius-sm)] bg-[var(--color-surface-hover)]/30 border border-dashed border-[var(--color-border-subtle)] text-[var(--color-text-tertiary)]">
            <span className="text-[11px]">Assigned Incident:</span>
            <span className="italic text-[11px]">Standby (No Active Incident)</span>
          </div>
        )}

        {/* Current Tool Call */}
        <div>
          <div className="flex items-center justify-between mb-1.5">
            <span className="text-[10px] font-mono uppercase tracking-wider text-[var(--color-text-tertiary)] flex items-center gap-1">
              <Terminal size={11} />
              <span>Current Tool Call</span>
            </span>
            {agent.elapsed_time_seconds > 0 && (
              <span className="text-[10px] font-mono text-[var(--color-text-secondary)] flex items-center gap-1">
                <Clock size={10} />
                <span>Elapsed: {formatElapsed(agent.elapsed_time_seconds)}</span>
              </span>
            )}
          </div>

          {agent.current_tool_call ? (
            <div className="p-2 rounded-[var(--radius-sm)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] font-mono text-[11px] text-[var(--color-text-primary)] break-all select-all">
              <span className="text-[var(--color-accent-text)] select-none mr-1.5">$</span>
              <span>{agent.current_tool_call}</span>
            </div>
          ) : (
            <div className="p-2 rounded-[var(--radius-sm)] bg-[var(--color-surface-hover)]/40 border border-[var(--color-border-subtle)] font-mono text-[11px] text-[var(--color-text-tertiary)] italic">
              Awaiting next autonomous dispatch
            </div>
          )}
        </div>

        {/* Guardrail Violation Count & Last Active Timestamp */}
        <div className="flex items-center justify-between pt-2 border-t border-[var(--color-border-subtle)] text-[var(--text-xs)]">
          {/* Guardrail status (§7.5 & §10) */}
          <div className="flex items-center gap-1.5">
            {hasViolations ? (
              <Badge
                variant="critical"
                size="sm"
                className="gap-1 font-semibold"
                aria-label={`${agent.guardrail_violations_count} guardrail violation intercepted`}
              >
                <ShieldAlert size={12} />
                <span>{agent.guardrail_violations_count} Guardrail Intercepted</span>
              </Badge>
            ) : (
              <span className="flex items-center gap-1 text-[var(--color-text-tertiary)] text-[11px]">
                <ShieldCheck size={12} className="text-[var(--color-success)]" />
                <span>0 Guardrail Violations</span>
              </span>
            )}
          </div>

          {/* Last Active Timestamp */}
          <div className="flex items-center gap-1 text-[11px] text-[var(--color-text-tertiary)]">
            <Zap size={11} />
            <span>Active</span>
            <RelativeTime date={agent.last_active_at} />
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
