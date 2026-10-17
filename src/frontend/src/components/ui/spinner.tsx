import { useEffect, useState, type HTMLAttributes } from 'react'
import { Loader2, AlertCircle } from 'lucide-react'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/lib/cn'

export const spinnerVariants = cva('animate-spin text-[var(--color-accent)] shrink-0', {
  variants: {
    size: {
      sm: 'w-3.5 h-3.5',
      md: 'w-5 h-5',
      lg: 'w-7 h-7',
    },
  },
  defaultVariants: {
    size: 'md',
  },
})

export interface SpinnerProps
  extends HTMLAttributes<HTMLDivElement>, VariantProps<typeof spinnerVariants> {
  label?: string
  timeoutMs?: number // Safeguard against indefinite spinners (§13)
  onTimeout?: () => void
  timeoutMessage?: string
}

export function Spinner({
  size = 'md',
  label = 'Loading...',
  timeoutMs,
  onTimeout,
  timeoutMessage = 'Loading timed out',
  className,
  ...props
}: SpinnerProps) {
  const [hasTimedOut, setHasTimedOut] = useState(false)

  useEffect(() => {
    if (!timeoutMs) return

    const timer = setTimeout(() => {
      setHasTimedOut(true)
      onTimeout?.()
    }, timeoutMs)

    return () => {
      clearTimeout(timer)
    }
  }, [timeoutMs, onTimeout])

  if (hasTimedOut) {
    return (
      <div
        role="alert"
        className={cn(
          'inline-flex items-center gap-1.5 text-xs text-[var(--color-critical)] font-medium',
          className,
        )}
        {...props}
      >
        <AlertCircle size={14} strokeWidth={1.5} />
        <span>{timeoutMessage}</span>
      </div>
    )
  }

  return (
    <div
      role="status"
      aria-label={label}
      className={cn('inline-flex items-center justify-center', className)}
      {...props}
    >
      <Loader2 className={cn(spinnerVariants({ size }))} strokeWidth={1.5} />
      <span className="sr-only">{label}</span>
    </div>
  )
}
