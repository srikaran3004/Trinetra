import type { ReactNode, HTMLAttributes } from 'react'
import { TrendingUp, TrendingDown } from 'lucide-react'
import { cn } from '@/lib/cn'

export interface MetricTileProps extends HTMLAttributes<HTMLDivElement> {
  label: string
  value: string | number
  unit?: string
  target?: string
  delta?: {
    value: string
    isPositive?: boolean
    label?: string
  }
  status?: 'critical' | 'high' | 'medium' | 'low' | 'success' | 'neutral'
  icon?: ReactNode
}

export function MetricTile({
  label,
  value,
  unit,
  target,
  delta,
  status = 'neutral',
  icon,
  className,
  ...props
}: MetricTileProps) {
  const statusColorMap = {
    critical: 'text-[var(--color-critical)]',
    high: 'text-[var(--color-high)]',
    medium: 'text-[var(--color-medium)]',
    low: 'text-[var(--color-low)]',
    success: 'text-[var(--color-success)]',
    neutral: 'text-[var(--color-text-primary)]',
  }

  return (
    <div
      role="region"
      aria-label={`${label}: ${value}${unit ? ` ${unit}` : ''}`}
      className={cn(
        'flex flex-col gap-2 p-4 bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)] rounded-[var(--radius-md)]',
        'transition-colors duration-120 hover:border-[var(--color-border-strong)]',
        className,
      )}
      {...props}
    >
      <div className="flex items-center justify-between text-xs text-[var(--color-text-secondary)]">
        <span className="font-medium truncate">{label}</span>
        {icon && <span className="text-[var(--color-text-tertiary)] shrink-0 ms-2">{icon}</span>}
      </div>

      <div className="flex items-baseline gap-1.5">
        <span
          className={cn(
            'text-2xl sm:text-3xl font-semibold font-mono tracking-tight',
            statusColorMap[status],
          )}
        >
          {value}
        </span>
        {unit && (
          <span className="text-xs font-mono text-[var(--color-text-tertiary)]">{unit}</span>
        )}
      </div>

      <div className="flex items-center justify-between gap-2 text-xs pt-1 border-t border-[var(--color-border-subtle)]">
        {target && (
          <span className="inline-flex items-center gap-1 text-[var(--color-text-tertiary)]">
            <span className="text-[10px] uppercase font-semibold tracking-wider text-[var(--color-accent-text)] bg-[color-mix(in_srgb,var(--color-accent)_10%,transparent)] px-1 py-0.5 rounded-[var(--radius-xs)]">
              Target
            </span>
            <span>{target}</span>
          </span>
        )}

        {delta && (
          <span
            className={cn(
              'inline-flex items-center gap-0.5 ms-auto font-mono text-[11px]',
              delta.isPositive ? 'text-[var(--color-success)]' : 'text-[var(--color-critical)]',
            )}
          >
            {delta.isPositive ? (
              <TrendingUp size={12} strokeWidth={1.5} />
            ) : (
              <TrendingDown size={12} strokeWidth={1.5} />
            )}
            <span>{delta.value}</span>
            {delta.label && (
              <span className="text-[var(--color-text-tertiary)] ms-1">{delta.label}</span>
            )}
          </span>
        )}
      </div>
    </div>
  )
}
