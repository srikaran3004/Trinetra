import { useState, type ReactNode } from 'react'
import {
  DialogTrigger as RACDialogTrigger,
  ModalOverlay as RACModalOverlay,
  Modal as RACModal,
  Dialog as RACDialog,
  Heading as RACHeading,
  type ModalOverlayProps as RACModalOverlayProps,
} from 'react-aria-components'
import { AlertTriangle, X } from 'lucide-react'
import { Button } from './button'
import { TextField } from './text-field'
import { cn } from '@/lib/cn'

export interface DialogProps extends RACModalOverlayProps {
  isOpen?: boolean
  onOpenChange?: (isOpen: boolean) => void
  trigger?: ReactNode
  title: string
  description?: string
  children?: ReactNode
  confirmLabel?: string
  cancelLabel?: string
  confirmVariant?: 'primary' | 'danger'
  confirmPhrase?: string // For high-risk actions per §7.6
  onConfirm?: () => void
  onCancel?: () => void
  className?: string
}

export function Dialog({
  isOpen,
  onOpenChange,
  trigger,
  title,
  description,
  children,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  confirmVariant = 'primary',
  confirmPhrase,
  onConfirm,
  onCancel,
  className,
  ...props
}: DialogProps) {
  const [typedPhrase, setTypedPhrase] = useState('')

  const isConfirmDisabled = confirmPhrase ? typedPhrase.trim() !== confirmPhrase.trim() : false

  const renderDialogContent = (close: () => void) => (
    <>
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-center gap-2.5">
          {confirmVariant === 'danger' && (
            <div className="p-2 rounded-[var(--radius-full)] bg-[color-mix(in_srgb,var(--color-critical)_15%,transparent)] text-[var(--color-critical)] shrink-0">
              <AlertTriangle size={18} strokeWidth={1.5} />
            </div>
          )}
          <div className="flex flex-col">
            <RACHeading
              slot="title"
              className="text-base font-semibold text-[var(--color-text-primary)]"
            >
              {title}
            </RACHeading>
            {description && (
              <p className="text-xs text-[var(--color-text-secondary)] mt-0.5 leading-relaxed">
                {description}
              </p>
            )}
          </div>
        </div>

        <button
          type="button"
          onClick={() => {
            onCancel?.()
            close()
          }}
          aria-label="Close dialog"
          className="text-[var(--color-text-tertiary)] hover:text-[var(--color-text-primary)] transition-colors duration-120 p-1 rounded-[var(--radius-xs)] cursor-pointer"
        >
          <X size={16} strokeWidth={1.5} />
        </button>
      </div>

      {children && <div className="text-xs text-[var(--color-text-secondary)]">{children}</div>}

      {/* High-risk typed confirmation per §7.6 */}
      {confirmPhrase && (
        <div className="flex flex-col gap-2 p-3 bg-[var(--color-canvas)] border border-[var(--color-border-subtle)] rounded-[var(--radius-sm)]">
          <p className="text-xs text-[var(--color-text-secondary)]">
            High-risk action. To proceed, please type{' '}
            <strong className="font-mono text-[var(--color-text-primary)] select-all">
              {confirmPhrase}
            </strong>{' '}
            below:
          </p>
          <TextField
            placeholder={`Type "${confirmPhrase}"`}
            value={typedPhrase}
            onChange={(val) => {
              setTypedPhrase(val)
            }}
            inputSize="sm"
            aria-label="Confirmation phrase"
          />
        </div>
      )}

      <div className="flex items-center justify-end gap-2.5 pt-2 border-t border-[var(--color-border-subtle)] mt-2">
        <Button
          variant="secondary"
          size="sm"
          onPress={() => {
            onCancel?.()
            close()
          }}
        >
          {cancelLabel}
        </Button>
        <Button
          variant={confirmVariant}
          size="sm"
          isDisabled={isConfirmDisabled}
          onPress={() => {
            onConfirm?.()
            close()
          }}
        >
          {confirmLabel}
        </Button>
      </div>
    </>
  )

  const modal = (
    <RACModalOverlay
      isOpen={isOpen}
      onOpenChange={onOpenChange}
      isDismissable
      className={({ isEntering, isExiting }) =>
        cn(
          'fixed inset-0 z-[var(--z-overlay)] bg-black/60 backdrop-blur-xs flex items-center justify-center p-4',
          'transition-opacity duration-200',
          isEntering && 'opacity-100',
          isExiting && 'opacity-0',
        )
      }
      {...props}
    >
      <RACModal
        className={({ isEntering, isExiting }) =>
          cn(
            'w-full max-w-lg p-5 bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)]',
            'rounded-[var(--radius-lg)] shadow-[var(--shadow-modal)] outline-none',
            'transition-transform duration-200',
            isEntering && 'scale-100',
            isExiting && 'scale-95',
          )
        }
      >
        <RACDialog className={cn('relative outline-none flex flex-col gap-4', className)}>
          {({ close }) => renderDialogContent(close)}
        </RACDialog>
      </RACModal>
    </RACModalOverlay>
  )

  if (trigger) {
    return (
      <RACDialogTrigger isOpen={isOpen} onOpenChange={onOpenChange}>
        {trigger}
        {modal}
      </RACDialogTrigger>
    )
  }

  return modal
}
