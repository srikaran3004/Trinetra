import type { ReactNode, HTMLAttributes } from 'react'
import { Inbox } from 'lucide-react'
import { cn } from '@/lib/cn'

export interface EmptyStateProps extends HTMLAttributes<HTMLDivElement> {
  icon?: ReactNode
  title: string
  description?: string
  action?: ReactNode
}

export function EmptyState({
  icon,
  title,
  description,
  action,
  className,
  ...props
}: EmptyStateProps) {
  return (
    <div
      className={cn(
        'flex flex-col items-center justify-center text-center p-8 sm:p-12',
        'border border-dashed border-[var(--color-border-subtle)] rounded-[var(--radius-lg)] bg-[var(--color-canvas)]',
        className,
      )}
      {...props}
    >
      <div className="flex items-center justify-center w-12 h-12 rounded-[var(--radius-full)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] text-[var(--color-text-tertiary)] mb-3">
        {icon || <Inbox size={22} strokeWidth={1.5} />}
      </div>
      <h4 className="text-sm font-semibold text-[var(--color-text-primary)] mb-1">{title}</h4>
      {description && (
        <p className="text-xs text-[var(--color-text-secondary)] max-w-sm mb-4 leading-relaxed">
          {description}
        </p>
      )}
      {action && <div className="mt-1">{action}</div>}
    </div>
  )
}
