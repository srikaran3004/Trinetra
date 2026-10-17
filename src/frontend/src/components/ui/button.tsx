import type { ReactNode } from 'react'
import { Button as RACButton, type ButtonProps as RACButtonProps } from 'react-aria-components'
import { cva, type VariantProps } from 'class-variance-authority'
import { Loader2 } from 'lucide-react'
import { cn } from '@/lib/cn'

/**
 * Button variants using class-variance-authority.
 * Tokens only: colors from CSS custom properties, radii, and transitions.
 * References:
 * - Linear: calm, precise, dense; 4 variants (primary, secondary, ghost, danger), 3 sizes.
 * - Stripe: hover tint, loading keeps width with spinner.
 * - GitHub: 2px focus-visible ring with 2px offset.
 */
export const buttonVariants = cva(
  [
    'relative inline-flex items-center justify-center font-medium select-none transition-colors duration-120 ease-out',
    'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--color-focus-ring)]',
    'disabled:pointer-events-none disabled:opacity-50',
    'data-[pressed]:opacity-90',
  ],
  {
    variants: {
      variant: {
        primary: [
          'bg-[var(--color-accent)] text-white hover:bg-[var(--color-accent-text)]',
          'border border-transparent',
        ],
        secondary: [
          'bg-[var(--color-surface-raised)] text-[var(--color-text-primary)]',
          'border border-[var(--color-border-subtle)] hover:border-[var(--color-border-strong)] hover:bg-[var(--color-surface-hover)]',
        ],
        ghost: [
          'bg-transparent text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-surface-hover)]',
          'border border-transparent',
        ],
        danger: [
          'bg-[var(--color-critical)] text-white hover:opacity-90',
          'border border-transparent',
        ],
      },
      size: {
        sm: 'h-7 px-2.5 text-[var(--text-xs)] rounded-[var(--radius-sm)] gap-1.5',
        md: 'h-9 px-3.5 text-[var(--text-base)] rounded-[var(--radius-sm)] gap-2',
        lg: 'h-11 px-5 text-[var(--text-md)] rounded-[var(--radius-md)] gap-2.5',
      },
    },
    defaultVariants: {
      variant: 'primary',
      size: 'md',
    },
  },
)

export interface ButtonProps
  extends Omit<RACButtonProps, 'children'>, VariantProps<typeof buttonVariants> {
  children?: ReactNode
  isLoading?: boolean
  loadingText?: string
}

export function Button({
  className,
  variant,
  size,
  isLoading = false,
  loadingText,
  children,
  isDisabled,
  ...props
}: ButtonProps) {
  return (
    <RACButton
      className={cn(buttonVariants({ variant, size, className }))}
      isDisabled={isDisabled ?? isLoading}
      {...props}
    >
      {isLoading ? (
        <span className="inline-flex items-center gap-2">
          <Loader2
            className="animate-spin"
            size={size === 'sm' ? 14 : size === 'lg' ? 18 : 16}
            strokeWidth={1.5}
            aria-hidden="true"
          />
          {loadingText ? <span>{loadingText}</span> : children}
        </span>
      ) : (
        children
      )}
    </RACButton>
  )
}
