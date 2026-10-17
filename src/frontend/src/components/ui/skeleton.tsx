import type { HTMLAttributes } from 'react'
import { cn } from '@/lib/cn'

export interface SkeletonProps extends HTMLAttributes<HTMLDivElement> {
  variant?: 'text' | 'rect' | 'circle'
}

export function Skeleton({ variant = 'rect', className, ...props }: SkeletonProps) {
  return (
    <div
      aria-hidden="true"
      className={cn(
        'bg-[var(--color-surface-hover)] animate-pulse select-none',
        variant === 'circle' && 'rounded-[var(--radius-full)]',
        variant === 'text' && 'h-4 w-full rounded-[var(--radius-xs)]',
        variant === 'rect' && 'rounded-[var(--radius-sm)]',
        className,
      )}
      {...props}
    />
  )
}
