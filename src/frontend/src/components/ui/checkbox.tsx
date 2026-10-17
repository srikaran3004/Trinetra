import type { ReactNode } from 'react'
import {
  Checkbox as RACCheckbox,
  CheckboxGroup as RACCheckboxGroup,
  Label as RACLabel,
  FieldError as RACFieldError,
  Text as RACText,
  type CheckboxProps as RACCheckboxProps,
  type CheckboxGroupProps as RACCheckboxGroupProps,
} from 'react-aria-components'
import { Check, Minus } from 'lucide-react'
import { cn } from '@/lib/cn'

export interface CheckboxProps extends Omit<RACCheckboxProps, 'children'> {
  children?: ReactNode
  description?: string
  className?: string
}

export function Checkbox({ children, description, className, ...props }: CheckboxProps) {
  return (
    <RACCheckbox
      className={({ isDisabled }) =>
        cn(
          'group flex items-start gap-2.5 text-sm cursor-pointer select-none text-[var(--color-text-primary)]',
          isDisabled && 'opacity-50 cursor-not-allowed',
          className,
        )
      }
      {...props}
    >
      {({ isSelected, isIndeterminate, isFocusVisible }) => (
        <>
          <div
            className={cn(
              'w-4 h-4 mt-0.5 shrink-0 flex items-center justify-center rounded-[var(--radius-xs)] border transition-colors duration-120',
              'border-[var(--color-border-strong)] bg-[var(--color-surface-raised)]',
              (isSelected || isIndeterminate) &&
                'bg-[var(--color-accent)] border-[var(--color-accent)] text-white',
              isFocusVisible &&
                'ring-2 ring-offset-2 ring-[var(--color-focus-ring)] ring-offset-[var(--color-canvas)]',
              'group-hover:border-[var(--color-accent)]',
            )}
          >
            {isIndeterminate ? (
              <Minus size={12} strokeWidth={2.5} />
            ) : isSelected ? (
              <Check size={12} strokeWidth={2.5} />
            ) : null}
          </div>

          {(children || description) && (
            <div className="flex flex-col text-start">
              {children && <span className="font-medium text-xs leading-5">{children}</span>}
              {description && (
                <span className="text-xs text-[var(--color-text-tertiary)]">{description}</span>
              )}
            </div>
          )}
        </>
      )}
    </RACCheckbox>
  )
}

export interface CheckboxGroupProps extends Omit<RACCheckboxGroupProps, 'children'> {
  label?: string
  description?: string
  errorMessage?: string
  children: ReactNode
  className?: string
}

export function CheckboxGroup({
  label,
  description,
  errorMessage,
  children,
  className,
  isInvalid,
  isDisabled,
  isRequired,
  ...props
}: CheckboxGroupProps) {
  return (
    <RACCheckboxGroup
      className={cn('flex flex-col gap-2 text-start', className)}
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

      <div className="flex flex-col gap-2">{children}</div>

      {description && !errorMessage && (
        <RACText slot="description" className="text-xs text-[var(--color-text-tertiary)]">
          {description}
        </RACText>
      )}

      {(errorMessage || isInvalid) && (
        <RACFieldError className="text-xs text-[var(--color-critical)]">
          {errorMessage || 'Selection error'}
        </RACFieldError>
      )}
    </RACCheckboxGroup>
  )
}
