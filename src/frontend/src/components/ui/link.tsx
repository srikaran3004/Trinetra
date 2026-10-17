import type { ReactNode } from 'react'
import { Link as RACLink, type LinkProps as RACLinkProps } from 'react-aria-components'
import { cva, type VariantProps } from 'class-variance-authority'
import { ExternalLink } from 'lucide-react'
import { cn } from '@/lib/cn'

export const linkVariants = cva(
  [
    'inline-flex items-center gap-1 font-medium underline-offset-4 transition-colors duration-120 cursor-pointer',
    'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-[var(--color-focus-ring)] rounded-[var(--radius-xs)]',
    'disabled:pointer-events-none disabled:opacity-50',
  ],
  {
    variants: {
      variant: {
        default: 'text-[var(--color-accent)] hover:text-[var(--color-accent-text)] underline',
        subtle:
          'text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:underline',
        muted: 'text-[var(--color-text-tertiary)] hover:text-[var(--color-text-secondary)]',
      },
    },
    defaultVariants: {
      variant: 'default',
    },
  },
)

export interface LinkProps
  extends Omit<RACLinkProps, 'children'>, VariantProps<typeof linkVariants> {
  children?: ReactNode
  isExternal?: boolean
}

export function Link({ className, variant, isExternal = false, children, ...props }: LinkProps) {
  const externalProps = isExternal ? { target: '_blank' as const, rel: 'noopener noreferrer' } : {}

  return (
    <RACLink className={cn(linkVariants({ variant, className }))} {...externalProps} {...props}>
      {children}
      {isExternal && <ExternalLink size={12} strokeWidth={1.5} aria-label="(opens in new tab)" />}
    </RACLink>
  )
}
