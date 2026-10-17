import type { ReactNode } from 'react'
import {
  MenuTrigger as RACMenuTrigger,
  Menu as RACMenu,
  MenuItem as RACMenuItem,
  Popover as RACPopover,
  Separator as RACSeparator,
  type MenuProps as RACMenuProps,
  type MenuItemProps as RACMenuItemProps,
} from 'react-aria-components'
import { cn } from '@/lib/cn'

export interface MenuItemOption {
  id: string
  label: string
  icon?: ReactNode
  shortcut?: string
  isDanger?: boolean
  isDisabled?: boolean
  onAction?: () => void
}

export interface MenuProps<T extends object = MenuItemOption> extends Omit<
  RACMenuProps<T>,
  'children' | 'items'
> {
  trigger: ReactNode
  items?: MenuItemOption[]
  className?: string
  children?: ReactNode
  onAction?: (key: string | number) => void
}

export function MenuItem({
  className,
  isDanger = false,
  children,
  icon,
  shortcut,
  ...props
}: RACMenuItemProps & {
  isDanger?: boolean
  icon?: ReactNode
  shortcut?: string
  children?: ReactNode
}) {
  return (
    <RACMenuItem
      className={({ isFocused, isDisabled }) =>
        cn(
          'flex items-center justify-between gap-3 px-3 py-1.5 text-xs rounded-[var(--radius-xs)] outline-none cursor-pointer transition-colors duration-120',
          isDanger ? 'text-[var(--color-critical)]' : 'text-[var(--color-text-primary)]',
          isFocused &&
            (isDanger
              ? 'bg-[color-mix(in_srgb,var(--color-critical)_12%,transparent)]'
              : 'bg-[var(--color-surface-hover)]'),
          isDisabled && 'opacity-50 cursor-not-allowed',
          className,
        )
      }
      {...props}
    >
      <div className="flex items-center gap-2 truncate">
        {icon && <span className="shrink-0 text-[var(--color-text-tertiary)]">{icon}</span>}
        <span className="truncate">{children}</span>
      </div>
      {shortcut && (
        <span className="font-mono text-[10px] text-[var(--color-text-tertiary)] ms-auto">
          {shortcut}
        </span>
      )}
    </RACMenuItem>
  )
}

export function MenuSeparator() {
  return <RACSeparator className="h-px bg-[var(--color-border-subtle)] my-1" />
}

export function Menu<T extends object>({
  trigger,
  items,
  className,
  children,
  onAction,
  ...props
}: MenuProps<T>) {
  return (
    <RACMenuTrigger>
      {trigger}
      <RACPopover
        className={cn(
          'z-[var(--z-dropdown)] min-w-44 p-1 bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)]',
          'rounded-[var(--radius-md)] shadow-[var(--shadow-floating)] outline-none',
        )}
      >
        <RACMenu
          className={cn('outline-none flex flex-col gap-0.5', className)}
          onAction={onAction}
          {...props}
        >
          {items
            ? items.map((item) => (
                <MenuItem
                  key={item.id}
                  id={item.id}
                  textValue={item.label}
                  isDanger={item.isDanger}
                  isDisabled={item.isDisabled}
                  icon={item.icon}
                  shortcut={item.shortcut}
                  onAction={item.onAction}
                >
                  {item.label}
                </MenuItem>
              ))
            : children}
        </RACMenu>
      </RACPopover>
    </RACMenuTrigger>
  )
}
