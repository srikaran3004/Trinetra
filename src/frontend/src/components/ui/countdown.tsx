import { useState, useEffect, useRef } from 'react'
import { Clock, AlertTriangle } from 'lucide-react'
import { cn } from '@/lib/cn'

export interface CountdownProps {
  initialSeconds?: number
  targetDate?: Date | number
  onTimeout?: () => void
  className?: string
  showIcon?: boolean
}

export function Countdown({
  initialSeconds = 15 * 60, // 15 minutes default per brief §7.6
  targetDate,
  onTimeout,
  className,
  showIcon = true,
}: CountdownProps) {
  const [secondsRemaining, setSecondsRemaining] = useState<number>(() => {
    if (targetDate) {
      const targetMs = typeof targetDate === 'number' ? targetDate : targetDate.getTime()
      return Math.max(0, Math.floor((targetMs - Date.now()) / 1000))
    }
    return initialSeconds
  })

  const [announcedMessage, setAnnouncedMessage] = useState<string>('')
  const lastAnnouncedMinute = useRef<number>(-1)
  const timedOutRef = useRef(false)

  useEffect(() => {
    if (secondsRemaining <= 0) {
      if (!timedOutRef.current) {
        timedOutRef.current = true
        onTimeout?.()
        setAnnouncedMessage('Timeout reached. Action expired.')
      }
      return
    }

    const currentMinute = Math.ceil(secondsRemaining / 60)
    // Screen reader announcement at minute boundaries or at 30 seconds
    if (currentMinute !== lastAnnouncedMinute.current && secondsRemaining > 60) {
      lastAnnouncedMinute.current = currentMinute
      setAnnouncedMessage(`${currentMinute} minutes remaining`)
    } else if (secondsRemaining === 60) {
      lastAnnouncedMinute.current = 1
      setAnnouncedMessage('1 minute remaining')
    } else if (secondsRemaining === 30) {
      setAnnouncedMessage('30 seconds remaining')
    }

    const timer = setInterval(() => {
      setSecondsRemaining((prev) => Math.max(0, prev - 1))
    }, 1000)

    return () => {
      clearInterval(timer)
    }
  }, [secondsRemaining, onTimeout])

  const isLastMinute = secondsRemaining <= 60
  const isTimedOut = secondsRemaining === 0
  const isCritical = secondsRemaining <= 120 // last 2 minutes warning

  const minutes = Math.floor(secondsRemaining / 60)
  const seconds = secondsRemaining % 60

  // Per §7.6: "seconds are visible in the last minute"
  const formattedDisplay = isTimedOut
    ? '0:00'
    : isLastMinute
      ? `0:${seconds.toString().padStart(2, '0')}`
      : `${minutes}m`

  return (
    <div
      className={cn(
        'inline-flex items-center gap-1.5 font-mono text-xs font-semibold px-2 py-1 rounded-[var(--radius-xs)] border transition-colors duration-120',
        isTimedOut
          ? 'bg-[color-mix(in_srgb,var(--color-critical)_15%,transparent)] text-[var(--color-critical)] border-[var(--color-critical)]'
          : isCritical
            ? 'bg-[color-mix(in_srgb,var(--color-high)_12%,transparent)] text-[var(--color-high)] border-[color-mix(in_srgb,var(--color-high)_30%,transparent)]'
            : 'bg-[var(--color-surface-hover)] text-[var(--color-text-secondary)] border-[var(--color-border-subtle)]',
        className,
      )}
    >
      {showIcon &&
        (isCritical ? (
          <AlertTriangle size={12} strokeWidth={1.5} className="shrink-0" />
        ) : (
          <Clock size={12} strokeWidth={1.5} className="shrink-0" />
        ))}
      <span aria-hidden="true">{formattedDisplay}</span>

      {/* Screen reader live region updating at minute boundaries */}
      <span className="sr-only" aria-live="polite" aria-atomic="true">
        {announcedMessage || `${Math.ceil(secondsRemaining / 60)} minutes remaining`}
      </span>
    </div>
  )
}
