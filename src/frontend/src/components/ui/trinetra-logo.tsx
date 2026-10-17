interface TrinetraLogoProps {
  size?: number
  className?: string
  showText?: boolean
}

/**
 * TrinetraLogo — Original mark for Trinetra.
 * Built per brief §5: "three nested arcs that suggest an eye, 1.5px monoline, no gradient."
 */
export function TrinetraLogo({ size = 28, className, showText = false }: TrinetraLogoProps) {
  return (
    <div className={`inline-flex items-center gap-[var(--space-2-5)] ${className ?? ''}`}>
      <svg
        width={size}
        height={size}
        viewBox="0 0 64 64"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
        aria-label="Trinetra logo"
        role="img"
        className="shrink-0 text-[var(--color-text-primary)]"
      >
        {/* Outer arc — upper lid */}
        <path
          d="M 8 32 C 8 18 20 8 32 8 C 44 8 56 18 56 32"
          stroke="currentColor"
          strokeWidth="1.5"
          strokeLinecap="round"
          fill="none"
        />
        {/* Middle arc — lower lid */}
        <path
          d="M 12 36 C 12 46 21 54 32 54 C 43 54 52 46 52 36"
          stroke="currentColor"
          strokeWidth="1.5"
          strokeLinecap="round"
          fill="none"
        />
        {/* Inner arc — iris */}
        <circle cx="32" cy="32" r="8" stroke="var(--color-accent)" strokeWidth="1.5" fill="none" />
        {/* Pupil dot */}
        <circle cx="32" cy="32" r="2.5" fill="var(--color-accent)" />
      </svg>
      {showText && (
        <span className="font-semibold text-[var(--text-lg)] tracking-[var(--tracking-heading)] text-[var(--color-text-primary)] leading-none select-none">
          Trinetra
        </span>
      )}
    </div>
  )
}
