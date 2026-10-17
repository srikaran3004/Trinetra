import * as React from 'react'
import { cn } from '@/lib/cn'

export interface KeyboardShortcutProps extends React.HTMLAttributes<HTMLElement> {
  keys: string[]
}

/**
 * KeyboardShortcut — Renders a key sequence inside semantic <kbd> tags.
 * Styled in Geist Mono font, token radius, and subtle border per brief §4.1.
 *
 * @example
 *   <KeyboardShortcut keys={['⌘', 'K']} />
 *   <KeyboardShortcut keys={['g', 'i']} />
 */
export function KeyboardShortcut({ keys, className, ...props }: KeyboardShortcutProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 font-mono text-[var(--text-xs)] text-[var(--color-text-secondary)]',
        className,
      )}
      {...props}
    >
      {keys.map((key, i) => (
        <kbd
          key={`${key}-${String(i)}`}
          className="inline-flex min-w-[20px] h-5 items-center justify-center px-1.5 py-0.5 rounded-[var(--radius-xs)] bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)] text-[var(--text-2xs)] font-mono font-medium shadow-[0_1px_0_0_var(--color-border-subtle)] text-[var(--color-text-primary)]"
        >
          {key}
        </kbd>
      ))}
    </span>
  )
}
