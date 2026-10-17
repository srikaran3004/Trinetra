import type { ReactNode, HTMLAttributes } from 'react'
import { cva, type VariantProps } from 'class-variance-authority'
import { AlertOctagon, AlertTriangle, Info, CheckCircle2, X } from 'lucide-react'
import { cn } from '@/lib/cn'

export const inlineAlertVariants = cva(
  'relative flex items-start gap-3 p-3.5 rounded-[var(--radius-sm)] border text-xs leading-relaxed transition-colors duration-120',
  {
    variants: {
      variant: {
        critical:
          'bg-[color-mix(in_srgb,var(--color-critical)_10%,transparent)] border-[color-mix(in_srgb,var(--color-critical)_30%,transparent)] text-[var(--color-text-primary)]',
        warning:
          'bg-[color-mix(in_srgb,var(--color-high)_10%,transparent)] border-[color-mix(in_srgb,var(--color-high)_30%,transparent)] text-[var(--color-text-primary)]',
        info: 'bg-[color-mix(in_srgb,var(--color-low)_10%,transparent)] border-[color-mix(in_srgb,var(--color-low)_30%,transparent)] text-[var(--color-text-primary)]',
        success:
          'bg-[color-mix(in_srgb,var(--color-success)_10%,transparent)] border-[color-mix(in_srgb,var(--color-success)_30%,transparent)] text-[var(--color-text-primary)]',
      },
    },
    defaultVariants: {
      variant: 'info',
    },
  },
)

export interface InlineAlertProps
  extends HTMLAttributes<HTMLDivElement>, VariantProps<typeof inlineAlertVariants> {
  title?: string
  action?: ReactNode
  onDismiss?: () => void
}

export function InlineAlert({
  variant = 'info',
  title,
  action,
  onDismiss,
  children,
  className,
  ...props
}: InlineAlertProps) {
  const iconMap = {
    critical: (
      <AlertOctagon
        size={16}
        strokeWidth={1.5}
        className="text-[var(--color-critical)] shrink-0 mt-0.5"
      />
    ),
    warning: (
      <AlertTriangle
        size={16}
        strokeWidth={1.5}
        className="text-[var(--color-high)] shrink-0 mt-0.5"
      />
    ),
    info: <Info size={16} strokeWidth={1.5} className="text-[var(--color-low)] shrink-0 mt-0.5" />,
    success: (
      <CheckCircle2
        size={16}
        strokeWidth={1.5}
        className="text-[var(--color-success)] shrink-0 mt-0.5"
      />
    ),
  }

  return (
    <div role="alert" className={cn(inlineAlertVariants({ variant }), className)} {...props}>
      {iconMap[variant ?? 'info']}
      <div className="flex-1 flex flex-col gap-0.5">
        {title && <h5 className="font-semibold text-[var(--color-text-primary)]">{title}</h5>}
        <div className="text-[var(--color-text-secondary)]">{children}</div>
        {action && <div className="mt-2">{action}</div>}
      </div>
      {onDismiss && (
        <button
          type="button"
          onClick={onDismiss}
          aria-label="Dismiss alert"
          className="text-[var(--color-text-tertiary)] hover:text-[var(--color-text-primary)] transition-colors duration-120 p-1 rounded-[var(--radius-xs)] cursor-pointer"
        >
          <X size={14} strokeWidth={1.5} />
        </button>
      )}
    </div>
  )
}
