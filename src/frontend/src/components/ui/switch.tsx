import type { ReactNode } from 'react'
import { Switch as RACSwitch, type SwitchProps as RACSwitchProps } from 'react-aria-components'
import { cn } from '@/lib/cn'

export interface SwitchProps extends Omit<RACSwitchProps, 'children'> {
  children?: ReactNode
  description?: string
  className?: string
}

export function Switch({ children, description, className, ...props }: SwitchProps) {
  return (
    <RACSwitch
      className={({ isDisabled }) =>
        cn(
          'group flex items-start justify-between gap-4 text-sm cursor-pointer select-none text-[var(--color-text-primary)]',
          isDisabled && 'opacity-50 cursor-not-allowed',
          className,
        )
      }
      {...props}
    >
      {({ isSelected, isFocusVisible }) => (
        <>
          {(children || description) && (
            <div className="flex flex-col text-start me-auto">
              {children && <span className="font-medium text-xs leading-5">{children}</span>}
              {description && (
                <span className="text-xs text-[var(--color-text-tertiary)]">{description}</span>
              )}
            </div>
          )}

          <div
            className={cn(
              'relative inline-flex h-5 w-9 shrink-0 cursor-pointer rounded-[var(--radius-full)] border-2 border-transparent transition-colors duration-120 ease-in-out',
              isSelected
                ? 'bg-[var(--color-accent)]'
                : 'bg-[var(--color-surface-hover)] border-[var(--color-border-strong)]',
              isFocusVisible &&
                'ring-2 ring-offset-2 ring-[var(--color-focus-ring)] ring-offset-[var(--color-canvas)]',
            )}
          >
            <span
              className={cn(
                'pointer-events-none inline-block h-4 w-4 rounded-[var(--radius-full)] bg-white shadow-sm ring-0 transition duration-120 ease-in-out',
                isSelected ? 'translate-x-4' : 'translate-x-0',
              )}
            />
          </div>
        </>
      )}
    </RACSwitch>
  )
}
