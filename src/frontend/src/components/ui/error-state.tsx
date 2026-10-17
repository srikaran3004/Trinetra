import { useState, type ReactNode, type HTMLAttributes } from 'react'
import { AlertOctagon, ChevronDown, ChevronRight, RotateCcw } from 'lucide-react'
import { Button } from './button'
import { cn } from '@/lib/cn'

export interface ErrorStateProps extends HTMLAttributes<HTMLDivElement> {
  title?: string
  message?: string
  description?: string
  details?: string
  onRetry?: () => void
  action?: ReactNode
  actionText?: string
}

export function ErrorState({
  title = 'Failed to load telemetry',
  message,
  description,
  details,
  onRetry,
  action,
  actionText = 'Try again',
  className,
  ...props
}: ErrorStateProps) {
  const [showDetails, setShowDetails] = useState(false)
  const displayMessage = message || description || 'An unexpected error occurred.'

  return (
    <div
      role="alert"
      className={cn(
        'flex flex-col items-center justify-center text-center p-6 sm:p-8',
        'border border-[color-mix(in_srgb,var(--color-critical)_30%,transparent)] rounded-[var(--radius-lg)] bg-[color-mix(in_srgb,var(--color-critical)_5%,transparent)]',
        className,
      )}
      {...props}
    >
      <div className="flex items-center justify-center w-10 h-10 rounded-[var(--radius-full)] bg-[color-mix(in_srgb,var(--color-critical)_15%,transparent)] text-[var(--color-critical)] mb-3">
        <AlertOctagon size={20} strokeWidth={1.5} />
      </div>

      <h4 className="text-sm font-semibold text-[var(--color-text-primary)] mb-1">{title}</h4>
      <p className="text-xs text-[var(--color-text-secondary)] max-w-md mb-4 leading-relaxed">
        {displayMessage}
      </p>

      <div className="flex items-center gap-2">
        {onRetry && (
          <Button size="sm" variant="secondary" onPress={onRetry}>
            <RotateCcw size={12} strokeWidth={1.5} />
            <span>{actionText}</span>
          </Button>
        )}
        {action}
      </div>

      {details && (
        <div className="w-full max-w-lg mt-4 text-start">
          <button
            type="button"
            onClick={() => {
              setShowDetails((prev) => !prev)
            }}
            className="flex items-center gap-1 text-[11px] font-mono text-[var(--color-text-tertiary)] hover:text-[var(--color-text-secondary)] cursor-pointer"
          >
            {showDetails ? <ChevronDown size={12} /> : <ChevronRight size={12} />}
            <span>Technical details</span>
          </button>
          {showDetails && (
            <pre className="mt-1.5 p-2.5 bg-[var(--color-canvas)] border border-[var(--color-border-subtle)] rounded-[var(--radius-xs)] font-mono text-[11px] text-[var(--color-critical)] overflow-x-auto whitespace-pre-wrap">
              {details}
            </pre>
          )}
        </div>
      )}
    </div>
  )
}
