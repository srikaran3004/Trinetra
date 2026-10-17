import {
  Check,
  AlertTriangle,
  ShieldAlert,
  Zap,
  Activity,
  Clock,
  Play,
  CheckCircle2,
} from 'lucide-react'
import { cn } from '@/lib/cn'
import type { IncidentStatus } from '@/api/types'

import type { LucideIcon } from 'lucide-react'

export interface StateProgressProps extends React.HTMLAttributes<HTMLDivElement> {
  currentStatus: IncidentStatus
}

interface StepConfig {
  id: IncidentStatus
  label: string
  shortLabel: string
  icon: LucideIcon
}

const LINEAR_STEPS: StepConfig[] = [
  { id: 'triggered', label: 'Triggered', shortLabel: 'Triggered', icon: Zap },
  { id: 'investigating', label: 'Investigating', shortLabel: 'Investigating', icon: Activity },
  {
    id: 'analysis_complete',
    label: 'Analysis Complete',
    shortLabel: 'Analyzed',
    icon: CheckCircle2,
  },
  { id: 'awaiting_approval', label: 'Awaiting Approval', shortLabel: 'Approval', icon: Clock },
  {
    id: 'executing_remediation',
    label: 'Executing Remediation',
    shortLabel: 'Executing',
    icon: Play,
  },
  {
    id: 'monitoring_recovery',
    label: 'Monitoring Recovery',
    shortLabel: 'Monitoring',
    icon: Activity,
  },
  { id: 'resolved', label: 'Resolved', shortLabel: 'Resolved', icon: Check },
]

export function StateProgress({ currentStatus, className, ...props }: StateProgressProps) {
  const isEscalated = currentStatus === 'escalated'
  const currentIndex = isEscalated ? -1 : LINEAR_STEPS.findIndex((s) => s.id === currentStatus)

  return (
    <div
      className={cn('w-full py-2 select-none', className)}
      role="region"
      aria-label="Incident State Progress"
      {...props}
    >
      {/* Escalated Alert Banner if state is escalated */}
      {isEscalated && (
        <div
          role="alert"
          className="mb-4 flex items-center justify-between p-3 rounded-[var(--radius-md)] bg-[color-mix(in_srgb,var(--color-critical)_12%,transparent)] border border-[color-mix(in_srgb,var(--color-critical)_35%,transparent)] text-[var(--color-critical)]"
        >
          <div className="flex items-center gap-2.5">
            <ShieldAlert size={18} className="shrink-0 animate-pulse" />
            <div>
              <span className="font-semibold text-[var(--text-sm)]">
                Escalated to Human On-Call
              </span>
              <p className="text-[var(--text-xs)] text-[var(--color-text-secondary)] mt-0.5">
                Autonomous agent investigation halted due to critical threshold or policy gate.
                Manual intervention required.
              </p>
            </div>
          </div>
          <span className="px-2 py-0.5 rounded-[var(--radius-xs)] bg-[color-mix(in_srgb,var(--color-critical)_25%,transparent)] text-[10px] font-mono uppercase tracking-wider font-semibold">
            ESCALATED BRANCH
          </span>
        </div>
      )}

      <ol className="flex items-center justify-between w-full relative overflow-x-auto pb-2 scrollbar-none">
        {LINEAR_STEPS.map((step, idx) => {
          const Icon = step.icon
          const isCompleted = !isEscalated && idx < currentIndex
          const isCurrent = !isEscalated && idx === currentIndex
          const isUpcoming = isEscalated || idx > currentIndex

          return (
            <li
              key={step.id}
              className="flex-1 flex flex-col items-center relative min-w-[80px] group"
              aria-current={isCurrent ? 'step' : undefined}
            >
              {/* Connector line to next step */}
              {idx < LINEAR_STEPS.length - 1 && (
                <div
                  className={cn(
                    'absolute top-4 start-1/2 end-[-50%] h-[2px] transition-colors duration-200 -z-0',
                    isCompleted ? 'bg-[var(--color-accent)]' : 'bg-[var(--color-border-subtle)]',
                  )}
                  aria-hidden="true"
                />
              )}

              {/* Node Indicator */}
              <div
                className={cn(
                  'w-8 h-8 rounded-full flex items-center justify-center border-2 transition-all duration-160 z-10 bg-[var(--color-surface-raised)]',
                  isCompleted &&
                    'border-[var(--color-accent)] bg-[var(--color-accent)] text-[var(--color-canvas)]',
                  isCurrent &&
                    'border-[var(--color-accent)] ring-4 ring-[color-mix(in_srgb,var(--color-accent)_20%,transparent)] text-[var(--color-accent)] animate-pulse',
                  isUpcoming &&
                    'border-[var(--color-border-subtle)] text-[var(--color-text-tertiary)] bg-[var(--color-surface-hover)]',
                )}
              >
                {isCompleted ? (
                  <Check size={14} strokeWidth={2.5} />
                ) : (
                  <Icon size={14} strokeWidth={isCurrent ? 2 : 1.5} />
                )}
              </div>

              {/* Step label */}
              <div className="mt-2 text-center">
                <span
                  className={cn(
                    'block text-[var(--text-xs)] transition-colors leading-tight',
                    isCurrent && 'font-semibold text-[var(--color-text-primary)]',
                    isCompleted && 'font-medium text-[var(--color-text-secondary)]',
                    isUpcoming && 'text-[var(--color-text-tertiary)]',
                  )}
                >
                  <span className="hidden sm:inline">{step.label}</span>
                  <span className="sm:hidden">{step.shortLabel}</span>
                </span>
              </div>
            </li>
          )
        })}

        {/* Escalated Alternate Branch Node */}
        {isEscalated && (
          <li
            className="flex-none flex flex-col items-center relative min-w-[90px] ml-2 pl-2 border-l border-dashed border-[var(--color-border-subtle)]"
            aria-current="step"
          >
            <div className="w-8 h-8 rounded-full flex items-center justify-center border-2 border-[var(--color-critical)] bg-[color-mix(in_srgb,var(--color-critical)_15%,transparent)] text-[var(--color-critical)] ring-4 ring-[color-mix(in_srgb,var(--color-critical)_25%,transparent)]">
              <AlertTriangle size={15} strokeWidth={2} />
            </div>
            <span className="mt-2 text-[var(--text-xs)] font-bold text-[var(--color-critical)]">
              Escalated
            </span>
          </li>
        )}
      </ol>
    </div>
  )
}
