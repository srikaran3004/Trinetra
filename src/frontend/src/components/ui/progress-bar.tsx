import {
  ProgressBar as RACProgressBar,
  Label as RACLabel,
  type ProgressBarProps as RACProgressBarProps,
} from 'react-aria-components'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/lib/cn'

export const progressBarVariants = cva('h-full transition-all duration-200 ease-out', {
  variants: {
    variant: {
      accent: 'bg-[var(--color-accent)]',
      success: 'bg-[var(--color-success)]',
      critical: 'bg-[var(--color-critical)]',
      high: 'bg-[var(--color-high)]',
      medium: 'bg-[var(--color-medium)]',
    },
  },
  defaultVariants: {
    variant: 'accent',
  },
})

export interface ProgressBarProps
  extends Omit<RACProgressBarProps, 'children'>, VariantProps<typeof progressBarVariants> {
  label?: string
  showValue?: boolean
  className?: string
  trackClassName?: string
}

export function ProgressBar({
  label,
  showValue = false,
  variant = 'accent',
  className,
  trackClassName,
  isIndeterminate = false,
  value = 0,
  minValue = 0,
  maxValue = 100,
  ...props
}: ProgressBarProps) {
  const percentage = Math.min(100, Math.max(0, ((value - minValue) / (maxValue - minValue)) * 100))

  return (
    <RACProgressBar
      className={cn('flex flex-col gap-1.5 w-full text-start', className)}
      isIndeterminate={isIndeterminate}
      value={value}
      minValue={minValue}
      maxValue={maxValue}
      {...props}
    >
      {(label || showValue) && (
        <div className="flex items-center justify-between text-xs text-[var(--color-text-secondary)] font-medium">
          {label && <RACLabel>{label}</RACLabel>}
          {showValue && !isIndeterminate && (
            <span className="font-mono text-[var(--color-text-tertiary)]">
              {Math.round(percentage)}%
            </span>
          )}
        </div>
      )}

      <div
        className={cn(
          'relative w-full h-2 bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] rounded-[var(--radius-full)] overflow-hidden',
          trackClassName,
        )}
      >
        {isIndeterminate ? (
          <div
            className={cn(
              'h-full w-1/3 animate-[pulse_1.5s_ease-in-out_infinite]',
              progressBarVariants({ variant }),
            )}
          />
        ) : (
          <div
            className={cn('h-full', progressBarVariants({ variant }))}
            style={{ width: `${percentage}%` }}
          />
        )}
      </div>
    </RACProgressBar>
  )
}
