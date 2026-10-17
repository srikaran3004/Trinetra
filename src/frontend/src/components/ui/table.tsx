import * as React from 'react'
import { cn } from '@/lib/cn'

export const Table = React.forwardRef<
  HTMLTableElement,
  React.TableHTMLAttributes<HTMLTableElement>
>(({ className, ...props }, ref) => (
  <div className="relative w-full overflow-auto">
    <table
      ref={ref}
      role="table"
      className={cn(
        'w-full caption-bottom text-[var(--text-sm)] text-[var(--color-text-primary)] border-collapse select-text',
        className,
      )}
      {...props}
    />
  </div>
))
Table.displayName = 'Table'

export const TableHeader = React.forwardRef<
  HTMLTableSectionElement,
  React.HTMLAttributes<HTMLTableSectionElement>
>(({ className, ...props }, ref) => (
  <thead
    ref={ref}
    role="rowgroup"
    className={cn(
      'border-b border-[var(--color-border-subtle)] bg-[var(--color-surface-hover)]/30',
      className,
    )}
    {...props}
  />
))
TableHeader.displayName = 'TableHeader'

export const TableBody = React.forwardRef<
  HTMLTableSectionElement,
  React.HTMLAttributes<HTMLTableSectionElement>
>(({ className, ...props }, ref) => (
  <tbody
    ref={ref}
    role="rowgroup"
    className={cn('divide-y divide-[var(--color-border-subtle)]', className)}
    {...props}
  />
))
TableBody.displayName = 'TableBody'

export const TableFooter = React.forwardRef<
  HTMLTableSectionElement,
  React.HTMLAttributes<HTMLTableSectionElement>
>(({ className, ...props }, ref) => (
  <tfoot
    ref={ref}
    role="rowgroup"
    className={cn(
      'border-t border-[var(--color-border-subtle)] bg-[var(--color-surface-hover)]/40 font-medium',
      className,
    )}
    {...props}
  />
))
TableFooter.displayName = 'TableFooter'

export interface TableRowProps extends React.HTMLAttributes<HTMLTableRowElement> {
  selected?: boolean
}

export const TableRow = React.forwardRef<HTMLTableRowElement, TableRowProps>(
  ({ className, selected, ...props }, ref) => (
    <tr
      ref={ref}
      role="row"
      aria-selected={selected}
      data-selected={selected ? 'true' : undefined}
      className={cn(
        'border-b border-[var(--color-border-subtle)] transition-colors duration-120',
        'hover:bg-[var(--color-surface-hover)]/60',
        'data-[selected=true]:bg-[var(--color-surface-hover)] data-[selected=true]:ring-1 data-[selected=true]:ring-[var(--color-focus-ring)] data-[selected=true]:ring-inset',
        className,
      )}
      {...props}
    />
  ),
)
TableRow.displayName = 'TableRow'

export const TableHead = React.forwardRef<
  HTMLTableCellElement,
  React.ThHTMLAttributes<HTMLTableCellElement> & { numeric?: boolean }
>(({ className, numeric, ...props }, ref) => (
  <th
    ref={ref}
    role="columnheader"
    scope="col"
    data-numeric={numeric ? 'true' : undefined}
    className={cn(
      'h-10 px-4 text-left align-middle font-medium text-[var(--text-xs)] uppercase tracking-[0.06em] text-[var(--color-text-tertiary)] select-none',
      numeric && 'text-right',
      className,
    )}
    {...props}
  />
))
TableHead.displayName = 'TableHead'

export const TableCell = React.forwardRef<
  HTMLTableCellElement,
  React.TdHTMLAttributes<HTMLTableCellElement> & { numeric?: boolean }
>(({ className, numeric, ...props }, ref) => (
  <td
    ref={ref}
    role="cell"
    data-numeric={numeric ? 'true' : undefined}
    className={cn(
      'p-4 align-middle text-[var(--text-sm)] text-[var(--color-text-primary)]',
      numeric && 'text-right font-mono tabular-nums',
      className,
    )}
    {...props}
  />
))
TableCell.displayName = 'TableCell'

export const TableCaption = React.forwardRef<
  HTMLTableCaptionElement,
  React.HTMLAttributes<HTMLTableCaptionElement>
>(({ className, ...props }, ref) => (
  <caption
    ref={ref}
    className={cn('mt-4 text-[var(--text-xs)] text-[var(--color-text-tertiary)]', className)}
    {...props}
  />
))
TableCaption.displayName = 'TableCaption'
