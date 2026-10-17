import type { ReactNode } from 'react'
import {
  DialogTrigger as RACDialogTrigger,
  Popover as RACPopover,
  Dialog as RACDialog,
  OverlayArrow as RACOverlayArrow,
  type PopoverProps as RACPopoverProps,
} from 'react-aria-components'
import { cn } from '@/lib/cn'

export interface PopoverProps extends Omit<RACPopoverProps, 'children' | 'trigger'> {
  trigger: ReactNode
  children: ReactNode
  title?: string
  className?: string
  popoverClassName?: string
  showArrow?: boolean
}

export function Popover({
  trigger,
  children,
  title,
  className,
  popoverClassName,
  showArrow = true,
  ...props
}: PopoverProps) {
  return (
    <RACDialogTrigger>
      {trigger}
      <RACPopover
        offset={8}
        className={({ isEntering, isExiting }) =>
          cn(
            'z-[var(--z-dropdown)] bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)]',
            'rounded-[var(--radius-md)] shadow-[var(--shadow-floating)] outline-none',
            'transition-opacity duration-120',
            isEntering && 'opacity-100',
            isExiting && 'opacity-0',
            popoverClassName,
          )
        }
        {...props}
      >
        {showArrow && (
          <RACOverlayArrow>
            <svg
              width={10}
              height={10}
              viewBox="0 0 10 10"
              className="fill-[var(--color-surface-raised)] stroke-[var(--color-border-subtle)]"
            >
              <path d="M0 0 L5 5 L10 0" />
            </svg>
          </RACOverlayArrow>
        )}
        <RACDialog className={cn('outline-none p-3 max-w-sm', className)}>
          {title && (
            <h4 className="text-xs font-semibold text-[var(--color-text-primary)] mb-2 uppercase tracking-wider">
              {title}
            </h4>
          )}
          {children}
        </RACDialog>
      </RACPopover>
    </RACDialogTrigger>
  )
}
