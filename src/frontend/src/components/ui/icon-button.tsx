import { isValidElement, type ReactNode } from 'react'
import { Button as RACButton, type ButtonProps as RACButtonProps } from 'react-aria-components'
import { cva, type VariantProps } from 'class-variance-authority'
import { Loader2 } from 'lucide-react'
import { cn } from '@/lib/cn'

export const iconButtonVariants = cva(
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
        sm: 'h-7 w-7 rounded-[var(--radius-sm)]',
        md: 'h-9 w-9 rounded-[var(--radius-sm)]',
        lg: 'h-11 w-11 rounded-[var(--radius-md)]', // 44px minimum hit area per Apple Touch reference
      },
    },
    defaultVariants: {
      variant: 'ghost',
      size: 'md',
    },
  },
)

export interface IconButtonProps
  extends Omit<RACButtonProps, 'children'>, VariantProps<typeof iconButtonVariants> {
  /** Mandatory accessible label for screen readers */
  'aria-label': string
  icon: ReactNode | React.ComponentType<{ size?: number; className?: string; strokeWidth?: number }>
  isLoading?: boolean
}

export function IconButton({
  className,
  variant,
  size,
  icon,
  isLoading = false,
  isDisabled,
  'aria-label': ariaLabel,
  ...props
}: IconButtonProps) {
  const iconSize = size === 'sm' ? 14 : size === 'lg' ? 20 : 16

  const renderIcon = () => {
    if (!icon) return null
    if (isValidElement(icon)) {
      return icon
    }
    if (typeof icon === 'function' || typeof icon === 'object') {
      const IconComponent = icon as React.ComponentType<{
        size?: number
        className?: string
        strokeWidth?: number
      }>
      return <IconComponent size={iconSize} strokeWidth={1.5} />
    }
    return icon
  }

  return (
    <RACButton
      className={cn(iconButtonVariants({ variant, size, className }))}
      aria-label={ariaLabel}
      isDisabled={isDisabled ?? isLoading}
      {...props}
    >
      {isLoading ? (
        <Loader2 className="animate-spin" size={iconSize} strokeWidth={1.5} aria-hidden="true" />
      ) : (
        renderIcon()
      )}
    </RACButton>
  )
}
