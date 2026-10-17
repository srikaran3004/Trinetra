import * as React from 'react'
import { CheckCircle2, AlertCircle } from 'lucide-react'
import { cn } from '@/lib/cn'

export interface ConfidenceBarProps extends React.HTMLAttributes<HTMLDivElement> {
  value: number | null | undefined
  threshold?: number
  showLabel?: boolean
  showThresholdLine?: boolean
}

/**
 * ConfidenceBar — Visualizes model/agent root cause confidence from 0 to 1.
 * Features a distinct 0.80 threshold marker for autonomous remediation per Brief §6 & §7.4.
 */
export function ConfidenceBar({
  value,
  threshold = 0.8,
  showLabel = true,
  showThresholdLine = true,
  className,
  ...props
}: ConfidenceBarProps) {
  const numericVal = value != null && !isNaN(value) ? Math.min(1, Math.max(0, value)) : 0
  const percentage = Math.round(numericVal * 100)
  const thresholdPercentage = Math.round(threshold * 100)
  const isAboveThreshold = numericVal >= threshold

  const fillColor = isAboveThreshold ? 'bg-[var(--color-success)]' : 'bg-[var(--color-medium)]'

  return (
    <div
      className={cn('w-full space-y-1.5 select-none', className)}
      role="meter"
      aria-label="Root cause confidence"
      aria-valuenow={numericVal}
      aria-valuemin={0}
      aria-valuemax={1}
      aria-valuetext={`${percentage}% confidence (${isAboveThreshold ? 'Above' : 'Below'} 0.80 auto-remediation threshold)`}
      {...props}
    >
      {/* Header with values */}
      {showLabel && (
        <div className="flex items-center justify-between text-[var(--text-xs)]">
          <div className="flex items-center gap-1.5">
            {isAboveThreshold ? (
              <CheckCircle2 size={13} className="text-[var(--color-success)]" />
            ) : (
              <AlertCircle size={13} className="text-[var(--color-medium)]" />
            )}
            <span className="font-semibold text-[var(--color-text-primary)] font-mono">
              {percentage}%
            </span>
            <span className="text-[var(--color-text-secondary)] text-[11px]">
              {isAboveThreshold
                ? 'High Confidence (Auto-eligible)'
                : 'Moderate Confidence (Manual Review)'}
            </span>
          </div>
          <span className="text-[var(--color-text-tertiary)] text-[11px] font-mono">
            Threshold: {thresholdPercentage}%
          </span>
        </div>
      )}

      {/* Progress Track */}
      <div className="relative h-3 w-full rounded-[var(--radius-full)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] overflow-hidden">
        {/* Fill Bar */}
        <div
          className={cn(
            'h-full transition-all duration-300 ease-out rounded-[var(--radius-full)]',
            fillColor,
          )}
          style={{ width: `${percentage}%` }}
        />

        {/* 0.80 Threshold Marker Line */}
        {showThresholdLine && (
          <div
            className="absolute top-0 bottom-0 w-[2px] bg-[var(--color-text-primary)]/80 pointer-events-none z-10 shadow-[0_0_2px_rgba(0,0,0,0.5)]"
            style={{ left: `${thresholdPercentage}%` }}
            title={`Auto-remediation threshold (${thresholdPercentage}%)`}
          />
        )}
      </div>

      {/* Threshold marker label underneath */}
      {showThresholdLine && (
        <div className="flex justify-end text-[10px] text-[var(--color-text-tertiary)] font-mono">
          <span style={{ marginRight: `${Math.max(0, 100 - thresholdPercentage - 8)}%` }}>
            ▲ 0.80 threshold
          </span>
        </div>
      )}
    </div>
  )
}
