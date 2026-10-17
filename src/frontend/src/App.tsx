import { useEffect } from 'react'
import { useThemeStore } from './store/theme'

/**
 * App — Root component for F0 scaffold.
 *
 * F0 renders a Trinetra placeholder in both dark and light themes.
 * Theme switching, routing, and full UI are built in subsequent phases.
 */
export default function App() {
  const { theme, resolvedTheme, initTheme, setTheme } = useThemeStore()

  // Initialize theme from system preference / persisted preference
  useEffect(() => {
    initTheme()
  }, [initTheme])

  // Apply theme to <html> as data-theme attribute
  useEffect(() => {
    document.documentElement.setAttribute('data-theme', resolvedTheme)
  }, [resolvedTheme])

  function toggleTheme() {
    setTheme(resolvedTheme === 'dark' ? 'light' : 'dark')
  }

  return (
    <>
      {/* Skip to content link — keyboard accessibility (brief §4.9) */}
      <a href="#main-content" className="skip-link">
        Skip to content
      </a>

      <div
        style={{
          minHeight: '100vh',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          gap: 'var(--space-8)',
          padding: 'var(--space-8)',
          backgroundColor: 'var(--color-canvas)',
          color: 'var(--color-text-primary)',
          fontFamily: 'var(--font-sans)',
        }}
      >
        {/* Trinetra SVG logo — three nested arcs suggesting an eye, 1.5px monoline */}
        <TrinetraLogo />

        <main id="main-content" aria-label="Trinetra placeholder">
          <h1
            style={{
              fontSize: 'var(--text-3xl)',
              fontWeight: 600,
              letterSpacing: 'var(--tracking-heading)',
              lineHeight: 'var(--leading-heading)',
              margin: 0,
              textAlign: 'center',
            }}
          >
            Trinetra
          </h1>
          <p
            style={{
              fontSize: 'var(--text-md)',
              color: 'var(--color-text-secondary)',
              marginBlockStart: 'var(--space-3)',
              textAlign: 'center',
              maxWidth: '480px',
              lineHeight: 'var(--leading-body)',
            }}
          >
            Autonomous IT Incident Response Platform.
            <br />
            React scaffold active — Phase F0 complete.
          </p>

          {/* Theme badge */}
          <div
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: 'var(--space-3)',
              justifyContent: 'center',
              marginBlockStart: 'var(--space-6)',
            }}
          >
            <span
              style={{
                fontSize: 'var(--text-xs)',
                fontFamily: 'var(--font-mono)',
                color: 'var(--color-text-tertiary)',
                letterSpacing: 'var(--tracking-label)',
                textTransform: 'uppercase',
              }}
            >
              Theme: {theme}
              {theme === 'system' ? ` (${resolvedTheme})` : ''}
            </span>
            <button
              type="button"
              onClick={toggleTheme}
              aria-label="Toggle theme"
              style={{
                padding: 'var(--space-1) var(--space-3)',
                fontSize: 'var(--text-xs)',
                fontFamily: 'var(--font-sans)',
                fontWeight: 500,
                background: 'var(--color-surface-raised)',
                color: 'var(--color-text-primary)',
                border: '1px solid var(--color-border-strong)',
                borderRadius: 'var(--radius-sm)',
                cursor: 'pointer',
                transition: `background var(--duration-fast) var(--ease-standard)`,
              }}
            >
              Toggle theme
            </button>
          </div>

          {/* Stack badge */}
          <div
            style={{
              display: 'flex',
              flexWrap: 'wrap',
              gap: 'var(--space-2)',
              justifyContent: 'center',
              marginBlockStart: 'var(--space-8)',
            }}
          >
            {['React 19', 'Vite', 'TypeScript strict', 'Tailwind v4', 'Motion'].map((tech) => (
              <span
                key={tech}
                style={{
                  padding: 'var(--space-1) var(--space-3)',
                  fontSize: 'var(--text-xs)',
                  fontFamily: 'var(--font-mono)',
                  background: 'var(--color-surface-raised)',
                  color: 'var(--color-accent-text)',
                  border: '1px solid var(--color-border-subtle)',
                  borderRadius: 'var(--radius-xs)',
                  letterSpacing: 'var(--tracking-label)',
                }}
              >
                {tech}
              </span>
            ))}
          </div>
        </main>
      </div>
    </>
  )
}

/**
 * Trinetra logo SVG — original mark.
 * Three nested arcs suggesting an eye. 1.5px monoline. No gradient.
 * Built per brief §5: "three nested arcs that suggest an eye, 1.5px monoline, no gradient."
 */
function TrinetraLogo() {
  return (
    <svg
      width="64"
      height="64"
      viewBox="0 0 64 64"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      aria-label="Trinetra logo"
      role="img"
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
  )
}
