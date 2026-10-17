import { useState, useEffect } from 'react'

export function formatAge(isoDateString: string, nowMs: number = Date.now()): string {
  const targetMs = new Date(isoDateString).getTime()
  if (isNaN(targetMs)) return 'unknown'

  const elapsedSec = Math.max(0, Math.floor((nowMs - targetMs) / 1000))

  if (elapsedSec < 60) {
    return `${elapsedSec}s ago`
  }

  const elapsedMin = Math.floor(elapsedSec / 60)
  if (elapsedMin < 60) {
    return `${elapsedMin}m ago`
  }

  const elapsedHours = Math.floor(elapsedMin / 60)
  if (elapsedHours < 24) {
    const remainingMin = elapsedMin % 60
    return remainingMin > 0 ? `${elapsedHours}h ${remainingMin}m ago` : `${elapsedHours}h ago`
  }

  const elapsedDays = Math.floor(elapsedHours / 24)
  return `${elapsedDays}d ago`
}

export interface RelativeTimeProps extends React.HTMLAttributes<HTMLSpanElement> {
  date: string
  refreshIntervalMs?: number
}

/**
 * RelativeTime — Renders dynamic relative age that refreshes every 30 seconds
 * per Brief §7.3 specifications.
 */
export function RelativeTime({
  date,
  refreshIntervalMs = 30_000,
  className,
  ...props
}: RelativeTimeProps) {
  const [now, setNow] = useState(() => Date.now())

  useEffect(() => {
    const timer = setInterval(() => {
      setNow(Date.now())
    }, refreshIntervalMs)

    return () => {
      clearInterval(timer)
    }
  }, [refreshIntervalMs])

  const formatted = formatAge(date, now)
  const fullTimestamp = new Date(date).toLocaleString(undefined, {
    dateStyle: 'medium',
    timeStyle: 'medium',
  })

  return (
    <time
      dateTime={date}
      title={fullTimestamp}
      aria-label={`Triggered ${formatted}`}
      className={className}
      {...props}
    >
      {formatted}
    </time>
  )
}
