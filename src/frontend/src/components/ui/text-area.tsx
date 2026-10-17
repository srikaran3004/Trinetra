import {
  TextField as RACTextField,
  Label as RACLabel,
  TextArea as RACTextArea,
  FieldError as RACFieldError,
  Text as RACText,
  type TextFieldProps as RACTextFieldProps,
} from 'react-aria-components'
import { cn } from '@/lib/cn'

export interface TextAreaProps extends Omit<RACTextFieldProps, 'children'> {
  label?: string
  description?: string
  errorMessage?: string
  placeholder?: string
  rows?: number
  className?: string
  textAreaClassName?: string
}

export function TextArea({
  label,
  description,
  errorMessage,
  placeholder,
  rows = 3,
  className,
  textAreaClassName,
  isInvalid,
  isDisabled,
  isRequired,
  ...props
}: TextAreaProps) {
  return (
    <RACTextField
      className={cn('flex flex-col gap-1.5 text-start w-full', className)}
      isInvalid={isInvalid || Boolean(errorMessage)}
      isDisabled={isDisabled}
      isRequired={isRequired}
      {...props}
    >
      {label && (
        <RACLabel className="text-xs font-medium text-[var(--color-text-secondary)]">
          {label}
          {isRequired && <span className="text-[var(--color-critical)] ms-0.5">*</span>}
        </RACLabel>
      )}

      <RACTextArea
        rows={rows}
        placeholder={placeholder}
        className={cn(
          'w-full p-3 text-sm bg-[var(--color-surface-raised)] text-[var(--color-text-primary)] placeholder:text-[var(--color-text-tertiary)]',
          'border border-[var(--color-border-subtle)] rounded-[var(--radius-sm)]',
          'transition-colors duration-120 outline-none resize-y',
          'hover:border-[var(--color-border-strong)]',
          'focus:border-[var(--color-accent)] focus:ring-1 focus:ring-[var(--color-focus-ring)]',
          'disabled:opacity-50 disabled:cursor-not-allowed disabled:bg-[var(--color-canvas)]',
          'data-[invalid]:border-[var(--color-critical)] data-[invalid]:focus:ring-[var(--color-critical)]',
          textAreaClassName,
        )}
      />

      {description && !errorMessage && (
        <RACText slot="description" className="text-xs text-[var(--color-text-tertiary)]">
          {description}
        </RACText>
      )}

      {(errorMessage || isInvalid) && (
        <RACFieldError className="text-xs text-[var(--color-critical)]">
          {errorMessage || 'Invalid input'}
        </RACFieldError>
      )}
    </RACTextField>
  )
}
