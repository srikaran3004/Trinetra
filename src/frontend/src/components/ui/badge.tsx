import type { ReactNode } from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import {
  AlertOctagon,
  AlertTriangle,
  AlertCircle,
  Info,
  CheckCircle2,
  Clock,
  Play,
  Activity,
  ShieldAlert,
  Zap,
} from 'lucide-react'
import { cn } from '@/lib/cn'

export const badgeVariants = cva(
  [
    'inline-flex items-center gap-1.5 font-medium select-none uppercase tracking-wider',
    'rounded-[var(--radius-xs)] border transition-colors duration-120',
  ],
  {
    variants: {
      variant: {
        critical:
          'bg-[color-mix(in_srgb,var(--color-critical)_12%,transparent)] text-[var(--color-critical)] border-[color-mix(in_srgb,var(--color-critical)_30%,transparent)]',
        high: 'bg-[color-mix(in_srgb,var(--color-high)_12%,transparent)] text-[var(--color-high)] border-[color-mix(in_srgb,var(--color-high)_30%,transparent)]',
        medium:
          'bg-[color-mix(in_srgb,var(--color-medium)_12%,transparent)] text-[var(--color-medium)] border-[color-mix(in_srgb,var(--color-medium)_30%,transparent)]',
        low: 'bg-[color-mix(in_srgb,var(--color-low)_12%,transparent)] text-[var(--color-low)] border-[color-mix(in_srgb,var(--color-low)_30%,transparent)]',
        success:
          'bg-[color-mix(in_srgb,var(--color-success)_12%,transparent)] text-[var(--color-success)] border-[color-mix(in_srgb,var(--color-success)_30%,transparent)]',
        accent:
          'bg-[color-mix(in_srgb,var(--color-accent)_12%,transparent)] text-[var(--color-accent-text)] border-[color-mix(in_srgb,var(--color-accent)_30%,transparent)]',
        neutral:
          'bg-[var(--color-surface-hover)] text-[var(--color-text-secondary)] border-[var(--color-border-subtle)]',
        outline:
          'bg-transparent text-[var(--color-text-primary)] border-[var(--color-border-strong)]',
      },
      size: {
        sm: 'h-5 px-1.5 text-[10px]',
        md: 'h-6 px-2 text-xs',
      },
    },
    defaultVariants: {
      variant: 'neutral',
      size: 'md',
    },
  },
)

export type SeverityLevel = 'critical' | 'high' | 'medium' | 'low'
export type IncidentState =
  | 'triggered'
  | 'investigating'
  | 'analysis_complete'
  | 'awaiting_approval'
  | 'executing_remediation'
  | 'monitoring_recovery'
  | 'resolved'
  | 'escalated'
export type Environment = 'prod' | 'staging' | 'dev'

export interface BadgeProps
  extends React.HTMLAttributes<HTMLSpanElement>, VariantProps<typeof badgeVariants> {
  icon?: ReactNode
  severity?: SeverityLevel
  incidentState?: IncidentState
  env?: Environment
}

export function Badge({
  children,
  className,
  variant,
  size,
  icon,
  severity,
  incidentState,
  env,
  ...props
}: BadgeProps) {
  let resolvedVariant = variant
  let resolvedIcon = icon
  let resolvedText = children

  if (severity) {
    resolvedVariant = severity
    if (!resolvedIcon) {
      if (severity === 'critical') resolvedIcon = <AlertOctagon size={12} strokeWidth={1.5} />
      else if (severity === 'high') resolvedIcon = <AlertTriangle size={12} strokeWidth={1.5} />
      else if (severity === 'medium') resolvedIcon = <AlertCircle size={12} strokeWidth={1.5} />
      else resolvedIcon = <Info size={12} strokeWidth={1.5} />
    }
    if (!resolvedText) {
      resolvedText = severity.toUpperCase()
    }
  } else if (incidentState) {
    switch (incidentState) {
      case 'triggered':
        resolvedVariant = 'critical'
        resolvedIcon = resolvedIcon ?? <Zap size={12} strokeWidth={1.5} />
        resolvedText = resolvedText ?? 'Triggered'
        break
      case 'investigating':
        resolvedVariant = 'low'
        resolvedIcon = resolvedIcon ?? <Activity size={12} strokeWidth={1.5} />
        resolvedText = resolvedText ?? 'Investigating'
        break
      case 'analysis_complete':
        resolvedVariant = 'accent'
        resolvedIcon = resolvedIcon ?? <CheckCircle2 size={12} strokeWidth={1.5} />
        resolvedText = resolvedText ?? 'Analysis Complete'
        break
      case 'awaiting_approval':
        resolvedVariant = 'medium'
        resolvedIcon = resolvedIcon ?? <Clock size={12} strokeWidth={1.5} />
        resolvedText = resolvedText ?? 'Awaiting Approval'
        break
      case 'executing_remediation':
        resolvedVariant = 'high'
        resolvedIcon = resolvedIcon ?? <Play size={12} strokeWidth={1.5} />
        resolvedText = resolvedText ?? 'Executing'
        break
      case 'monitoring_recovery':
        resolvedVariant = 'low'
        resolvedIcon = resolvedIcon ?? <Activity size={12} strokeWidth={1.5} />
        resolvedText = resolvedText ?? 'Monitoring'
        break
      case 'resolved':
        resolvedVariant = 'success'
        resolvedIcon = resolvedIcon ?? <CheckCircle2 size={12} strokeWidth={1.5} />
        resolvedText = resolvedText ?? 'Resolved'
        break
      case 'escalated':
        resolvedVariant = 'critical'
        resolvedIcon = resolvedIcon ?? <ShieldAlert size={12} strokeWidth={1.5} />
        resolvedText = resolvedText ?? 'Escalated'
        break
    }
  } else if (env) {
    switch (env) {
      case 'prod':
        resolvedVariant = 'critical'
        resolvedText = resolvedText ?? 'Production'
        break
      case 'staging':
        resolvedVariant = 'medium'
        resolvedText = resolvedText ?? 'Staging'
        break
      case 'dev':
        resolvedVariant = 'neutral'
        resolvedText = resolvedText ?? 'Development'
        break
    }
  }

  return (
    <span className={cn(badgeVariants({ variant: resolvedVariant, size }), className)} {...props}>
      {resolvedIcon && <span className="shrink-0">{resolvedIcon}</span>}
      <span>{resolvedText}</span>
    </span>
  )
}
