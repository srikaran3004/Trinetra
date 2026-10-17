import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/lib/cn'

export const statusDotVariants = cva('relative inline-flex rounded-[var(--radius-full)] shrink-0', {
  variants: {
    status: {
      critical: 'bg-[var(--color-critical)]',
      high: 'bg-[var(--color-high)]',
      medium: 'bg-[var(--color-medium)]',
      low: 'bg-[var(--color-low)]',
      success: 'bg-[var(--color-success)]',
      accent: 'bg-[var(--color-accent)]',
      neutral: 'bg-[var(--color-text-tertiary)]',
    },
    size: {
      sm: 'w-1.5 h-1.5',
      md: 'w-2 h-2',
      lg: 'w-2.5 h-2.5',
    },
  },
  defaultVariants: {
    status: 'neutral',
    size: 'md',
  },
})

export interface StatusDotProps
  extends React.HTMLAttributes<HTMLSpanElement>, VariantProps<typeof statusDotVariants> {
  label?: string
  isPulsing?: boolean
  showLabel?: boolean
}

export function StatusDot({
  status = 'neutral',
  size = 'md',
  label,
  isPulsing = false,
  showLabel = false,
  className,
  ...props
}: StatusDotProps) {
  const accessibleLabel = label ?? `${status ?? 'unknown'} status`

  return (
    <span className={cn('inline-flex items-center gap-2', className)} {...props}>
      <span className="relative flex items-center justify-center">
        {isPulsing && (
          <span
            className={cn(
              'absolute inline-flex h-full w-full animate-ping rounded-[var(--radius-full)] opacity-75',
              statusDotVariants({ status, size }),
            )}
          />
        )}
        <span
          className={statusDotVariants({ status, size })}
          role="status"
          aria-label={showLabel ? undefined : accessibleLabel}
        />
      </span>
      {showLabel ? (
        <span className="text-xs text-[var(--color-text-primary)] font-medium">
          {label ?? status}
        </span>
      ) : (
        <span className="sr-only">{accessibleLabel}</span>
      )}
    </span>
  )
}
