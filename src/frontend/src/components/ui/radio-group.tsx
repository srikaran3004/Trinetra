import type { ReactNode } from 'react'
import {
  RadioGroup as RACRadioGroup,
  Radio as RACRadio,
  Label as RACLabel,
  FieldError as RACFieldError,
  Text as RACText,
  type RadioGroupProps as RACRadioGroupProps,
  type RadioProps as RACRadioProps,
} from 'react-aria-components'
import { cn } from '@/lib/cn'

export interface RadioProps extends Omit<RACRadioProps, 'children'> {
  children?: ReactNode
  description?: string
  className?: string
}

export function Radio({ children, description, className, ...props }: RadioProps) {
  return (
    <RACRadio
      className={({ isDisabled }) =>
        cn(
          'group flex items-start gap-2.5 text-sm cursor-pointer select-none text-[var(--color-text-primary)]',
          isDisabled && 'opacity-50 cursor-not-allowed',
          className,
        )
      }
      {...props}
    >
      {({ isSelected, isFocusVisible }) => (
        <>
          <div
            className={cn(
              'w-4 h-4 mt-0.5 shrink-0 flex items-center justify-center rounded-[var(--radius-full)] border transition-colors duration-120',
              'border-[var(--color-border-strong)] bg-[var(--color-surface-raised)]',
              isSelected && 'border-[var(--color-accent)]',
              isFocusVisible &&
                'ring-2 ring-offset-2 ring-[var(--color-focus-ring)] ring-offset-[var(--color-canvas)]',
              'group-hover:border-[var(--color-accent)]',
            )}
          >
            {isSelected && (
              <div className="w-2 h-2 rounded-[var(--radius-full)] bg-[var(--color-accent)]" />
            )}
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
    </RACRadio>
  )
}

export interface RadioGroupProps extends Omit<RACRadioGroupProps, 'children'> {
  label?: string
  description?: string
  errorMessage?: string
  children: ReactNode
  className?: string
  orientation?: 'horizontal' | 'vertical'
}

export function RadioGroup({
  label,
  description,
  errorMessage,
  children,
  className,
  orientation = 'vertical',
  isInvalid,
  isDisabled,
  isRequired,
  ...props
}: RadioGroupProps) {
  return (
    <RACRadioGroup
      className={cn('flex flex-col gap-2 text-start', className)}
      isInvalid={isInvalid || Boolean(errorMessage)}
      isDisabled={isDisabled}
      isRequired={isRequired}
      orientation={orientation}
      {...props}
    >
      {label && (
        <RACLabel className="text-xs font-medium text-[var(--color-text-secondary)]">
          {label}
          {isRequired && <span className="text-[var(--color-critical)] ms-0.5">*</span>}
        </RACLabel>
      )}

      <div
        className={cn(
          'flex gap-3',
          orientation === 'horizontal' ? 'flex-row flex-wrap items-center' : 'flex-col',
        )}
      >
        {children}
      </div>

      {description && !errorMessage && (
        <RACText slot="description" className="text-xs text-[var(--color-text-tertiary)]">
          {description}
        </RACText>
      )}

      {(errorMessage || isInvalid) && (
        <RACFieldError className="text-xs text-[var(--color-critical)]">
          {errorMessage || 'Selection required'}
        </RACFieldError>
      )}
    </RACRadioGroup>
  )
}
