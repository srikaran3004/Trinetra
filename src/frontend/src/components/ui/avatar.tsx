import { useState } from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { Bot, User } from 'lucide-react'
import { cn } from '@/lib/cn'

export const avatarVariants = cva(
  [
    'relative inline-flex items-center justify-center shrink-0 select-none overflow-hidden',
    'rounded-[var(--radius-full)] border border-[var(--color-border-subtle)] bg-[var(--color-surface-hover)] text-[var(--color-text-primary)]',
    'font-medium uppercase tracking-tight',
  ],
  {
    variants: {
      size: {
        sm: 'w-6 h-6 text-[10px]',
        md: 'w-8 h-8 text-xs',
        lg: 'w-10 h-10 text-sm',
      },
    },
    defaultVariants: {
      size: 'md',
    },
  },
)

export interface AvatarProps
  extends React.HTMLAttributes<HTMLDivElement>, VariantProps<typeof avatarVariants> {
  src?: string
  alt?: string
  initials?: string
  isAgent?: boolean
}

export function Avatar({
  src,
  alt = '',
  initials,
  isAgent = false,
  size = 'md',
  className,
  ...props
}: AvatarProps) {
  const [hasError, setHasError] = useState(false)

  const iconSize = size === 'sm' ? 12 : size === 'lg' ? 20 : 16

  return (
    <div
      role="img"
      aria-label={alt || initials || (isAgent ? 'Agent avatar' : 'User avatar')}
      className={cn(avatarVariants({ size }), className)}
      {...props}
    >
      {src && !hasError ? (
        <img
          src={src}
          alt={alt}
          onError={() => {
            setHasError(true)
          }}
          className="h-full w-full object-cover"
        />
      ) : initials ? (
        <span>{initials.slice(0, 2)}</span>
      ) : isAgent ? (
        <Bot size={iconSize} strokeWidth={1.5} className="text-[var(--color-accent-text)]" />
      ) : (
        <User size={iconSize} strokeWidth={1.5} className="text-[var(--color-text-tertiary)]" />
      )}
    </div>
  )
}
