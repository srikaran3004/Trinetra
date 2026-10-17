import { useState, useId } from 'react'
import { Calendar } from 'lucide-react'
import { cn } from '@/lib/cn'

export type DateRangePreset = '7d' | '30d' | '90d' | 'custom'

export interface DateRangeValue {
  preset: DateRangePreset
  start: string
  end: string
}

export interface DateRangeProps {
  value?: DateRangeValue
  defaultValue?: DateRangeValue
  onChange?: (value: DateRangeValue) => void
  className?: string
  isDisabled?: boolean
}

function formatDate(d: Date): string {
  return d.toISOString().split('T')[0] ?? ''
}

function getPresetRange(preset: DateRangePreset): { start: string; end: string } {
  const end = new Date()
  const start = new Date()
  if (preset === '7d') {
    start.setDate(end.getDate() - 7)
  } else if (preset === '30d') {
    start.setDate(end.getDate() - 30)
  } else if (preset === '90d') {
    start.setDate(end.getDate() - 90)
  }
  return { start: formatDate(start), end: formatDate(end) }
}

export function DateRange({
  value,
  defaultValue,
  onChange,
  className,
  isDisabled = false,
}: DateRangeProps) {
  const startId = useId()
  const endId = useId()

  const [internalValue, setInternalValue] = useState<DateRangeValue>(() => {
    if (defaultValue) return defaultValue
    const { start, end } = getPresetRange('30d')
    return { preset: '30d', start, end }
  })

  const currentValue = value ?? internalValue

  const handlePresetChange = (preset: DateRangePreset) => {
    if (isDisabled) return
    if (preset === 'custom') {
      const next: DateRangeValue = { ...currentValue, preset: 'custom' }
      if (!value) setInternalValue(next)
      onChange?.(next)
    } else {
      const { start, end } = getPresetRange(preset)
      const next: DateRangeValue = { preset, start, end }
      if (!value) setInternalValue(next)
      onChange?.(next)
    }
  }

  const handleDateChange = (type: 'start' | 'end', dateStr: string) => {
    if (isDisabled) return
    const next: DateRangeValue = {
      ...currentValue,
      preset: 'custom',
      [type]: dateStr,
    }
    if (!value) setInternalValue(next)
    onChange?.(next)
  }

  return (
    <div
      role="group"
      aria-label="Date range selector"
      className={cn(
        'inline-flex flex-wrap items-center gap-2 p-1 bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)] rounded-[var(--radius-sm)]',
        isDisabled && 'opacity-50 pointer-events-none',
        className,
      )}
    >
      <div className="flex items-center gap-1.5 px-2 text-[var(--color-text-secondary)]">
        <Calendar size={14} strokeWidth={1.5} className="shrink-0" />
        <span className="text-xs font-medium">Range:</span>
      </div>

      {/* Preset pills */}
      <div className="flex items-center gap-0.5 bg-[var(--color-canvas)] p-0.5 rounded-[var(--radius-xs)]">
        {(['7d', '30d', '90d'] as const).map((preset) => (
          <button
            key={preset}
            type="button"
            onClick={() => {
              handlePresetChange(preset)
            }}
            className={cn(
              'px-2 py-1 text-xs font-medium rounded-[var(--radius-xs)] transition-colors duration-120 cursor-pointer',
              currentValue.preset === preset
                ? 'bg-[var(--color-surface-raised)] text-[var(--color-text-primary)] shadow-sm'
                : 'text-[var(--color-text-tertiary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-surface-hover)]',
            )}
          >
            {preset === '7d' ? '7 days' : preset === '30d' ? '30 days' : '90 days'}
          </button>
        ))}
      </div>

      {/* Custom date range pickers */}
      <div className="flex items-center gap-1.5 ms-1">
        <div className="flex items-center gap-1">
          <label htmlFor={startId} className="sr-only">
            Start Date
          </label>
          <input
            id={startId}
            type="date"
            value={currentValue.start}
            onChange={(e) => {
              handleDateChange('start', e.target.value)
            }}
            disabled={isDisabled}
            className={cn(
              'h-7 px-2 text-xs bg-[var(--color-canvas)] text-[var(--color-text-primary)] border border-[var(--color-border-subtle)]',
              'rounded-[var(--radius-xs)] outline-none transition-colors duration-120',
              'hover:border-[var(--color-border-strong)] focus:border-[var(--color-accent)] focus:ring-1 focus:ring-[var(--color-focus-ring)]',
            )}
          />
        </div>
        <span className="text-xs text-[var(--color-text-tertiary)]">—</span>
        <div className="flex items-center gap-1">
          <label htmlFor={endId} className="sr-only">
            End Date
          </label>
          <input
            id={endId}
            type="date"
            value={currentValue.end}
            onChange={(e) => {
              handleDateChange('end', e.target.value)
            }}
            disabled={isDisabled}
            className={cn(
              'h-7 px-2 text-xs bg-[var(--color-canvas)] text-[var(--color-text-primary)] border border-[var(--color-border-subtle)]',
              'rounded-[var(--radius-xs)] outline-none transition-colors duration-120',
              'hover:border-[var(--color-border-strong)] focus:border-[var(--color-accent)] focus:ring-1 focus:ring-[var(--color-focus-ring)]',
            )}
          />
        </div>
      </div>
    </div>
  )
}
