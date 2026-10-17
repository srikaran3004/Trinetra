import { Toaster as SonnerToaster, toast } from 'sonner'
import { CheckCircle2, AlertTriangle, AlertOctagon, Info } from 'lucide-react'

export interface ToasterProps {
  position?:
    'top-left' | 'top-right' | 'bottom-left' | 'bottom-right' | 'top-center' | 'bottom-center'
}

export function Toaster({ position = 'bottom-right' }: ToasterProps) {
  return (
    <SonnerToaster
      position={position}
      toastOptions={{
        className:
          '!bg-[var(--color-surface-raised)] !border !border-[var(--color-border-subtle)] !text-[var(--color-text-primary)] !rounded-[var(--radius-md)] !shadow-[var(--shadow-modal)] !font-sans !text-xs !p-3.5',
        descriptionClassName: '!text-[var(--color-text-secondary)] !text-xs',
      }}
    />
  )
}

export const showToast = {
  success: (message: string, description?: string) =>
    toast.success(message, {
      description,
      icon: <CheckCircle2 size={16} strokeWidth={1.5} className="text-[var(--color-success)]" />,
    }),
  error: (message: string, description?: string) =>
    toast.error(message, {
      description,
      icon: <AlertOctagon size={16} strokeWidth={1.5} className="text-[var(--color-critical)]" />,
    }),
  warning: (message: string, description?: string) =>
    toast.warning(message, {
      description,
      icon: <AlertTriangle size={16} strokeWidth={1.5} className="text-[var(--color-high)]" />,
    }),
  info: (message: string, description?: string) =>
    toast.info(message, {
      description,
      icon: <Info size={16} strokeWidth={1.5} className="text-[var(--color-low)]" />,
    }),
}
