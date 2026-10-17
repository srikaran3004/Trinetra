import type { ReactNode } from 'react'
import {
  TooltipTrigger as RACTooltipTrigger,
  Tooltip as RACTooltip,
  OverlayArrow as RACOverlayArrow,
  type TooltipProps as RACTooltipProps,
} from 'react-aria-components'
import { cn } from '@/lib/cn'

export interface TooltipProps extends Omit<RACTooltipProps, 'children'> {
  content: ReactNode
  children: ReactNode
  delay?: number
  closeDelay?: number
  className?: string
}

export function Tooltip({
  content,
  children,
  delay = 400, // 400ms delay per brief §4.2
  closeDelay = 0,
  className,
  ...props
}: TooltipProps) {
  return (
    <RACTooltipTrigger delay={delay} closeDelay={closeDelay}>
      {children}
      <RACTooltip
        offset={6}
        className={({ isEntering, isExiting }) =>
          cn(
            'z-[var(--z-tooltip)] px-2.5 py-1 text-xs font-medium text-[var(--color-text-primary)]',
            'bg-[var(--color-surface-hover)] border border-[var(--color-border-strong)] rounded-[var(--radius-xs)] shadow-[var(--shadow-floating)]',
            'outline-none select-none pointer-events-none transition-opacity duration-120',
            isEntering && 'opacity-100',
            isExiting && 'opacity-0',
            className,
          )
        }
        {...props}
      >
        <RACOverlayArrow>
          <svg
            width={8}
            height={8}
            viewBox="0 0 8 8"
            className="fill-[var(--color-surface-hover)] stroke-[var(--color-border-strong)]"
          >
            <path d="M0 0 L4 4 L8 0" />
          </svg>
        </RACOverlayArrow>
        {content}
      </RACTooltip>
    </RACTooltipTrigger>
  )
}
