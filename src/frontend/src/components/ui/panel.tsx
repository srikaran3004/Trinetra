import type { ReactNode, HTMLAttributes } from 'react'
import { cn } from '@/lib/cn'

export interface PanelProps extends Omit<HTMLAttributes<HTMLDivElement>, 'title'> {
  title?: ReactNode
  subtitle?: ReactNode
  actions?: ReactNode
  variant?: 'raised' | 'canvas'
}

export function Panel({
  title,
  subtitle,
  actions,
  children,
  className,
  variant = 'raised',
  ...props
}: PanelProps) {
  return (
    <div
      className={cn(
        'border border-[var(--color-border-subtle)] rounded-[var(--radius-md)] overflow-hidden',
        variant === 'raised' ? 'bg-[var(--color-surface-raised)]' : 'bg-[var(--color-canvas)]',
        className,
      )}
      {...props}
    >
      {(title || actions) && (
        <div className="flex items-center justify-between px-4 py-3 border-b border-[var(--color-border-subtle)] bg-[var(--color-surface-hover)]">
          <div className="flex flex-col gap-0.5">
            {typeof title === 'string' ? (
              <h4 className="text-xs font-semibold text-[var(--color-text-primary)] uppercase tracking-wider">
                {title}
              </h4>
            ) : (
              title
            )}
            {subtitle && (
              <span className="text-xs text-[var(--color-text-tertiary)]">{subtitle}</span>
            )}
          </div>
          {actions && <div className="flex items-center gap-2">{actions}</div>}
        </div>
      )}
      <div className="p-4">{children}</div>
    </div>
  )
}
