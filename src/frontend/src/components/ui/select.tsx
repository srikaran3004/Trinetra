import type { ReactNode } from 'react'
import {
  Select as RACSelect,
  Label as RACLabel,
  Button as RACButton,
  SelectValue as RACSelectValue,
  Popover as RACPopover,
  ListBox as RACListBox,
  ListBoxItem as RACListBoxItem,
  FieldError as RACFieldError,
  Text as RACText,
  type SelectProps as RACSelectProps,
  type ListBoxItemProps as RACListBoxItemProps,
} from 'react-aria-components'
import { ChevronDown, Check } from 'lucide-react'
import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/lib/cn'

export const selectTriggerVariants = cva(
  [
    'flex items-center justify-between w-full bg-[var(--color-surface-raised)] text-[var(--color-text-primary)]',
    'border border-[var(--color-border-subtle)] rounded-[var(--radius-sm)]',
    'transition-colors duration-120 outline-none cursor-pointer',
    'hover:border-[var(--color-border-strong)]',
    'focus-visible:border-[var(--color-accent)] focus-visible:ring-1 focus-visible:ring-[var(--color-focus-ring)]',
    'disabled:opacity-50 disabled:cursor-not-allowed disabled:bg-[var(--color-canvas)]',
    'data-[invalid]:border-[var(--color-critical)]',
  ],
  {
    variants: {
      selectSize: {
        sm: 'h-8 px-2.5 text-xs',
        md: 'h-9 px-3 text-sm',
        lg: 'h-10 px-3.5 text-sm',
      },
    },
    defaultVariants: {
      selectSize: 'md',
    },
  },
)

export interface SelectOption {
  id: string | number
  label: string
  description?: string
  isDisabled?: boolean
}

export interface SelectProps<T extends object>
  extends Omit<RACSelectProps<T>, 'children'>, VariantProps<typeof selectTriggerVariants> {
  label?: string
  description?: string
  errorMessage?: string
  placeholder?: string
  options?: SelectOption[]
  className?: string
  triggerClassName?: string
  children?: ReactNode | ((item: T) => ReactNode)
}

export function SelectItem({
  className,
  children,
  ...props
}: RACListBoxItemProps & { children?: ReactNode }) {
  return (
    <RACListBoxItem
      className={({ isFocused, isSelected, isDisabled }) =>
        cn(
          'flex items-center justify-between px-3 py-2 text-sm rounded-[var(--radius-xs)] outline-none cursor-pointer transition-colors duration-120',
          'text-[var(--color-text-primary)]',
          isFocused && 'bg-[var(--color-surface-hover)]',
          isSelected &&
            'font-medium text-[var(--color-accent-text)] bg-[var(--color-surface-pressed)]',
          isDisabled && 'opacity-50 cursor-not-allowed',
          className,
        )
      }
      {...props}
    >
      {({ isSelected }) => (
        <>
          <span className="truncate">{children}</span>
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
  )
}

export function Select<T extends object>({
  label,
  description,
  errorMessage,
  placeholder = 'Select an option',
  options,
  className,
  triggerClassName,
  selectSize = 'md',
  isInvalid,
  isDisabled,
  isRequired,
  children,
  ...props
}: SelectProps<T>) {
  return (
    <RACSelect
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

      <RACButton className={cn(selectTriggerVariants({ selectSize }), triggerClassName)}>
        <RACSelectValue className="truncate text-start">
          {({ selectedText }) =>
            selectedText || <span className="text-[var(--color-text-tertiary)]">{placeholder}</span>
          }
        </RACSelectValue>
        <ChevronDown
          size={14}
          strokeWidth={1.5}
          className="text-[var(--color-text-tertiary)] shrink-0 ms-2"
        />
      </RACButton>

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

      <RACPopover
        className={cn(
          'w-[--trigger-width] min-w-40 p-1 bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)]',
          'rounded-[var(--radius-md)] shadow-[var(--shadow-floating)] z-[var(--z-dropdown)] outline-none',
        )}
      >
        <RACListBox className="outline-none max-h-60 overflow-y-auto p-0 flex flex-col gap-0.5">
          {options
            ? options.map((opt) => (
                <SelectItem
                  key={opt.id}
                  id={opt.id}
                  textValue={opt.label}
                  isDisabled={opt.isDisabled}
                >
                  <div>
                    <div className="font-normal">{opt.label}</div>
                    {opt.description && (
                      <div className="text-xs text-[var(--color-text-tertiary)]">
                        {opt.description}
                      </div>
                    )}
                  </div>
                </SelectItem>
              ))
            : children}
        </RACListBox>
      </RACPopover>
    </RACSelect>
  )
}
