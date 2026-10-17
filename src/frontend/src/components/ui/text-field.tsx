import type { ReactNode } from 'react'
import {
  TextField as RACTextField,
  Label as RACLabel,
  Input as RACInput,
  FieldError as RACFieldError,
  Text as RACText,
  type TextFieldProps as RACTextFieldProps,
} from 'react-aria-components'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/lib/cn'

export const inputFieldVariants = cva(
  [
    'w-full bg-[var(--color-surface-raised)] text-[var(--color-text-primary)] placeholder:text-[var(--color-text-tertiary)]',
    'border border-[var(--color-border-subtle)] rounded-[var(--radius-sm)]',
    'transition-colors duration-120 outline-none',
    'hover:border-[var(--color-border-strong)]',
    'focus:border-[var(--color-accent)] focus:ring-1 focus:ring-[var(--color-focus-ring)]',
    'disabled:opacity-50 disabled:cursor-not-allowed disabled:bg-[var(--color-canvas)]',
    'data-[invalid]:border-[var(--color-critical)] data-[invalid]:focus:ring-[var(--color-critical)]',
  ],
  {
    variants: {
      inputSize: {
        sm: 'h-8 px-2.5 text-xs',
        md: 'h-9 px-3 text-sm',
        lg: 'h-10 px-3.5 text-sm',
      },
    },
    defaultVariants: {
      inputSize: 'md',
    },
  },
)

export interface TextFieldProps
  extends Omit<RACTextFieldProps, 'children'>, VariantProps<typeof inputFieldVariants> {
  label?: string
  description?: string
  errorMessage?: string
  prefixIcon?: ReactNode
  suffixIcon?: ReactNode
  placeholder?: string
  className?: string
  inputClassName?: string
}

export function TextField({
  label,
  description,
  errorMessage,
  prefixIcon,
  suffixIcon,
  placeholder,
  className,
  inputClassName,
  inputSize = 'md',
  isInvalid,
  isDisabled,
  isRequired,
  ...props
}: TextFieldProps) {
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

      <div className="relative flex items-center w-full">
        {prefixIcon && (
          <span className="absolute inset-inline-start-3 flex items-center pointer-events-none text-[var(--color-text-tertiary)]">
            {prefixIcon}
          </span>
        )}

        <RACInput
          placeholder={placeholder}
          className={cn(
            inputFieldVariants({ inputSize }),
            prefixIcon && 'ps-9',
            suffixIcon && 'pe-9',
            inputClassName,
          )}
        />

        {suffixIcon && (
          <span className="absolute inset-inline-end-3 flex items-center pointer-events-none text-[var(--color-text-tertiary)]">
            {suffixIcon}
          </span>
        )}
      </div>

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
