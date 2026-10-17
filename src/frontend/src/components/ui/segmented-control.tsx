import type { ReactNode } from 'react'
import {
  RadioGroup as RACRadioGroup,
  Radio as RACRadio,
  type RadioGroupProps as RACRadioGroupProps,
} from 'react-aria-components'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/lib/cn'

export const segmentVariants = cva(
  [
    'flex items-center justify-center gap-1.5 font-medium rounded-[var(--radius-xs)] cursor-pointer select-none transition-all duration-120',
    'focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-[var(--color-focus-ring)]',
    'disabled:opacity-50 disabled:cursor-not-allowed',
  ],
  {
    variants: {
      segmentSize: {
        sm: 'h-7 px-2.5 text-xs',
        md: 'h-8 px-3 text-xs',
        lg: 'h-9 px-3.5 text-sm',
      },
    },
    defaultVariants: {
      segmentSize: 'md',
    },
  },
)

export interface SegmentOption {
  id: string
  label: string
  icon?: ReactNode
  isDisabled?: boolean
}

export interface SegmentedControlProps
  extends
    Omit<RACRadioGroupProps, 'children' | 'orientation'>,
    VariantProps<typeof segmentVariants> {
  options: SegmentOption[]
  className?: string
  'aria-label': string
}

export function SegmentedControl({
  options,
  className,
  segmentSize = 'md',
  'aria-label': ariaLabel,
  isDisabled,
  ...props
}: SegmentedControlProps) {
  return (
    <RACRadioGroup
      orientation="horizontal"
      aria-label={ariaLabel}
      isDisabled={isDisabled}
      className={cn(
        'inline-flex items-center p-0.5 bg-[var(--color-canvas)] border border-[var(--color-border-subtle)] rounded-[var(--radius-sm)]',
        isDisabled && 'opacity-50 cursor-not-allowed',
        className,
      )}
      {...props}
    >
      {options.map((option) => (
        <RACRadio
          key={option.id}
          value={option.id}
          isDisabled={option.isDisabled}
          className={({ isSelected, isFocusVisible, isDisabled: itemDisabled }) =>
            cn(
              segmentVariants({ segmentSize }),
              isSelected
                ? 'bg-[var(--color-surface-raised)] text-[var(--color-text-primary)] shadow-sm font-semibold'
                : 'text-[var(--color-text-tertiary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-surface-hover)]',
              isFocusVisible && 'ring-2 ring-[var(--color-focus-ring)]',
              itemDisabled && 'opacity-50 cursor-not-allowed',
            )
          }
        >
          {option.icon && <span className="shrink-0">{option.icon}</span>}
          <span>{option.label}</span>
        </RACRadio>
      ))}
    </RACRadioGroup>
  )
}
