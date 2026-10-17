import { useState } from 'react'
import {
  History,
  ShieldAlert,
  ChevronDown,
  ChevronRight,
  Terminal,
  Clock,
  AlertOctagon,
} from 'lucide-react'
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Avatar } from '@/components/ui/avatar'
import { CodeBlock } from '@/components/ui/code-block'
import { RelativeTime } from '@/components/ui/relative-time'
import { cn } from '@/lib/cn'
import type { IncidentEvent } from '@/api/types'

export interface AgentTimelineProps {
  events: IncidentEvent[]
}

export function AgentTimeline({ events }: AgentTimelineProps) {
  const [expandedEventIds, setExpandedEventIds] = useState<Record<string, boolean>>({})

  const toggleExpand = (id: string) => {
    setExpandedEventIds((prev) => ({
      ...prev,
      [id]: !prev[id],
    }))
  }

  // Sort events chronologically (earliest to latest or latest first)
  const sortedEvents = [...events].sort(
    (a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime(),
  )

  return (
    <Card className="bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)]">
      <CardHeader className="pb-3 border-b border-[var(--color-border-subtle)]">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <History size={18} className="text-[var(--color-accent)]" />
            <CardTitle>Autonomous Agent Investigation Timeline</CardTitle>
          </div>
          <span className="text-[var(--text-xs)] text-[var(--color-text-tertiary)]">
            {events.length} Audit {events.length === 1 ? 'Event' : 'Events'} Logged
          </span>
        </div>
      </CardHeader>

      <CardContent className="pt-6">
        {events.length === 0 ? (
          <p className="text-[var(--text-sm)] text-[var(--color-text-secondary)] italic">
            Waiting for agent execution events to stream...
          </p>
        ) : (
          <div className="relative border-l-2 border-[var(--color-border-subtle)] ml-3 space-y-6">
            {sortedEvents.map((evt, idx) => {
              const isGuardrail =
                evt.guardrail_triggered || evt.event_type === 'guardrail_violation'
              const isExpanded = Boolean(expandedEventIds[evt.id || idx])

              return (
                <div key={evt.id || idx} className="relative pl-6">
                  {/* Timeline Node Bullet */}
                  <div
                    className={cn(
                      'absolute -left-[9px] top-1 w-4 h-4 rounded-full border-2 bg-[var(--color-surface-raised)] flex items-center justify-center',
                      isGuardrail
                        ? 'border-[var(--color-critical)] bg-[var(--color-critical)] text-[var(--color-canvas)] ring-4 ring-[color-mix(in_srgb,var(--color-critical)_25%,transparent)]'
                        : 'border-[var(--color-accent)] text-[var(--color-accent)]',
                    )}
                  >
                    {isGuardrail ? (
                      <AlertOctagon size={10} strokeWidth={3} />
                    ) : (
                      <span className="w-1.5 h-1.5 rounded-full bg-[var(--color-accent)]" />
                    )}
                  </div>

                  {/* Event Card Container — Guardrail events are visually distinct and labeled in text per Brief §7.4 & §10 */}
                  <div
                    data-testid={`timeline-event-${evt.id}`}
                    className={cn(
                      'p-4 rounded-[var(--radius-md)] border transition-all duration-120',
                      isGuardrail
                        ? 'bg-[color-mix(in_srgb,var(--color-critical)_10%,transparent)] border-[var(--color-critical)] ring-1 ring-[var(--color-critical)]/40 shadow-[0_0_12px_rgba(255,107,107,0.15)]'
                        : 'bg-[var(--color-surface-hover)]/30 border-[var(--color-border-subtle)] hover:border-[var(--color-border-strong)]',
                    )}
                  >
                    {/* Header Row */}
                    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                      <div className="flex items-center gap-2">
                        <Avatar
                          isAgent={!evt.agent_name.includes('Human')}
                          initials={evt.agent_name.includes('Human') ? 'OP' : undefined}
                          size="sm"
                        />
                        <div>
                          <span className="text-[var(--text-sm)] font-semibold text-[var(--color-text-primary)]">
                            {evt.agent_name}
                          </span>
                          <span className="text-[var(--text-xs)] text-[var(--color-text-tertiary)] block">
                            {evt.event_type.replace(/_/g, ' ')}
                          </span>
                        </div>
                      </div>

                      {/* Timestamps & Distinct Guardrail Badge */}
                      <div className="flex items-center gap-2">
                        {isGuardrail && (
                          <Badge
                            variant="critical"
                            size="sm"
                            icon={<ShieldAlert size={12} />}
                            className="font-bold tracking-wide"
                          >
                            GUARDRAIL TRIGGERED
                          </Badge>
                        )}

                        <span className="text-[var(--text-xs)] font-mono text-[var(--color-text-tertiary)] flex items-center gap-1">
                          <Clock size={12} />
                          <RelativeTime date={evt.timestamp} />
                        </span>
                      </div>
                    </div>

                    {/* Action Description */}
                    <p
                      className={cn(
                        'text-[var(--text-sm)] mt-2.5 leading-relaxed',
                        isGuardrail
                          ? 'text-[var(--color-critical)] font-medium'
                          : 'text-[var(--color-text-primary)]',
                      )}
                    >
                      {evt.action_description || 'Autonomous step executed.'}
                    </p>

                    {/* Tool Call pill & toggle details */}
                    {evt.tool_call && (
                      <div className="mt-3 pt-2.5 border-t border-[var(--color-border-subtle)] flex items-center justify-between">
                        <div className="flex items-center gap-1.5 font-mono text-[var(--text-xs)] text-[var(--color-text-secondary)]">
                          <Terminal size={12} className="text-[var(--color-accent-text)]" />
                          <span>Tool:</span>
                          <span className="px-1.5 py-0.5 rounded-[var(--radius-xs)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] text-[var(--color-text-primary)] font-semibold">
                            {evt.tool_call}
                          </span>
                        </div>

                        {evt.details && (
                          <button
                            type="button"
                            onClick={() => {
                              toggleExpand(evt.id || String(idx))
                            }}
                            className="text-[var(--text-xs)] text-[var(--color-accent-text)] hover:underline flex items-center gap-0.5 cursor-pointer select-none"
                          >
                            <span>{isExpanded ? 'Hide payload' : 'Inspect payload'}</span>
                            {isExpanded ? <ChevronDown size={12} /> : <ChevronRight size={12} />}
                          </button>
                        )}
                      </div>
                    )}

                    {/* Expandable Payload & Parameter Details */}
                    {isExpanded && evt.details && (
                      <div className="mt-2.5 pt-2 border-t border-[var(--color-border-subtle)] space-y-1.5">
                        <span className="text-[11px] font-mono uppercase tracking-wider text-[var(--color-text-tertiary)]">
                          Execution Payload / Guardrail Interception Log
                        </span>
                        <CodeBlock code={evt.details} language="bash" />
                      </div>
                    )}
                  </div>
                </div>
              )
            })}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
