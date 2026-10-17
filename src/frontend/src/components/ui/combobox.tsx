import type { ReactNode } from 'react'
import {
  ComboBox as RACComboBox,
  Label as RACLabel,
  Input as RACInput,
  Button as RACButton,
  Popover as RACPopover,
  ListBox as RACListBox,
  ListBoxItem as RACListBoxItem,
  FieldError as RACFieldError,
  Text as RACText,
  type ComboBoxProps as RACComboBoxProps,
} from 'react-aria-components'
import { ChevronDown, Check } from 'lucide-react'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/lib/cn'

export const comboboxVariants = cva(
  [
    'flex items-center w-full bg-[var(--color-surface-raised)] text-[var(--color-text-primary)]',
    'border border-[var(--color-border-subtle)] rounded-[var(--radius-sm)]',
    'transition-colors duration-120',
    'hover:border-[var(--color-border-strong)]',
    'focus-within:border-[var(--color-accent)] focus-within:ring-1 focus-within:ring-[var(--color-focus-ring)]',
    'disabled:opacity-50 disabled:cursor-not-allowed disabled:bg-[var(--color-canvas)]',
    'data-[invalid]:border-[var(--color-critical)]',
  ],
  {
    variants: {
      comboSize: {
        sm: 'h-8 text-xs',
        md: 'h-9 text-sm',
        lg: 'h-10 text-sm',
      },
    },
    defaultVariants: {
      comboSize: 'md',
    },
  },
)

export interface ComboboxOption {
  id: string | number
  label: string
  description?: string
  isDisabled?: boolean
}

export interface ComboboxProps<T extends object>
  extends Omit<RACComboBoxProps<T>, 'children'>, VariantProps<typeof comboboxVariants> {
  label?: string
  description?: string
  errorMessage?: string
  placeholder?: string
  options?: ComboboxOption[]
  className?: string
  inputClassName?: string
  children?: ReactNode | ((item: T) => ReactNode)
}

export function Combobox<T extends object>({
  label,
  description,
  errorMessage,
  placeholder = 'Type or select...',
  options,
  className,
  inputClassName,
  comboSize = 'md',
  isInvalid,
  isDisabled,
  isRequired,
  children,
  ...props
}: ComboboxProps<T>) {
  return (
    <RACComboBox
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

      <div className={cn(comboboxVariants({ comboSize }))}>
        <RACInput
          placeholder={placeholder}
          className={cn(
            'w-full h-full px-3 bg-transparent outline-none text-[var(--color-text-primary)] placeholder:text-[var(--color-text-tertiary)]',
            inputClassName,
          )}
        />
        <RACButton className="px-2.5 h-full flex items-center justify-center outline-none cursor-pointer text-[var(--color-text-tertiary)] hover:text-[var(--color-text-secondary)]">
          <ChevronDown size={14} strokeWidth={1.5} />
        </RACButton>
      </div>

      {description && !errorMessage && (
        <RACText slot="description" className="text-xs text-[var(--color-text-tertiary)]">
          {description}
        </RACText>
      )}

      {(errorMessage || isInvalid) && (
        <RACFieldError className="text-xs text-[var(--color-critical)]">
          {errorMessage || 'Invalid option'}
        </RACFieldError>
      )}

      <RACPopover
        className={cn(
          'w-[--trigger-width] min-w-40 p-1 bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)]',
          'rounded-[var(--radius-md)] shadow-[var(--shadow-floating)] z-[var(--z-dropdown)] outline-none',
        )}
      >
        <RACListBox className="outline-none max-h-60 overflow-y-auto p-0 flex flex-col gap-0.5">
          {options
            ? options.map((opt) => (
                <RACListBoxItem
                  key={opt.id}
                  id={opt.id}
                  textValue={opt.label}
                  isDisabled={opt.isDisabled}
                  className={({ isFocused, isSelected, isDisabled }) =>
                    cn(
                      'flex items-center justify-between px-3 py-2 text-sm rounded-[var(--radius-xs)] outline-none cursor-pointer transition-colors duration-120',
                      'text-[var(--color-text-primary)]',
                      isFocused && 'bg-[var(--color-surface-hover)]',
                      isSelected &&
                        'font-medium text-[var(--color-accent-text)] bg-[var(--color-surface-pressed)]',
                      isDisabled && 'opacity-50 cursor-not-allowed',
                    )
                  }
                >
                  {({ isSelected }) => (
                    <>
                      <div>
                        <div>{opt.label}</div>
                        {opt.description && (
                          <div className="text-xs text-[var(--color-text-tertiary)]">
                            {opt.description}
                          </div>
                        )}
                      </div>
                      {isSelected && (
                        <Check
                          size={14}
                          strokeWidth={1.5}
                          className="text-[var(--color-accent)] shrink-0 ms-2"
                        />
                      )}
                    </>
                  )}
                </RACListBoxItem>
              ))
            : children}
        </RACListBox>
      </RACPopover>
    </RACComboBox>
  )
}
