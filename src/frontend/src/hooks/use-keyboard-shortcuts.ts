import { useEffect, useRef } from 'react'
import { useNavigate } from 'react-router'

interface KeyboardShortcutsOptions {
  onOpenSearch?: () => void
  onOpenShortcuts?: () => void
  onEscape?: () => void
}

/**
 * useKeyboardShortcuts — Global keyboard navigation hook conforming to §4.9 (GitHub reference).
 *
 * Shortcuts:
 * - `/` or `Ctrl+K` / `Cmd+K`: Open search / Command Palette
 * - `?`: Open keyboard shortcuts dialog
 * - `g` then `i`: Go to Incidents (`/incidents`)
 * - `g` then `a`: Go to Agents (`/agents`)
 * - `g` then `p`: Go to Approvals (`/approvals`)
 * - `g` then `r`: Go to Runbooks (`/runbooks`)
 * - `g` then `s`: Go to Settings (`/settings`)
 * - `Escape`: Close overlays / escape
 */
export function useKeyboardShortcuts({
  onOpenSearch,
  onOpenShortcuts,
  onEscape,
}: KeyboardShortcutsOptions = {}) {
  const navigate = useNavigate()
  const pendingPrefixRef = useRef<string | null>(null)
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  useEffect(() => {
    function handleKeyDown(event: KeyboardEvent) {
      // Allow Escape even inside form fields to dismiss modals
      if (event.key === 'Escape') {
        if (onEscape) {
          onEscape()
        }
        return
      }

      // Check if target is an interactive editable element
      const target = event.target as HTMLElement | null
      const isEditable =
        target &&
        (target.tagName === 'INPUT' ||
          target.tagName === 'TEXTAREA' ||
          target.tagName === 'SELECT' ||
          target.isContentEditable)

      if (isEditable) {
        return
      }

      // Command + K or Ctrl + K -> Open search / Command Palette
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault()
        onOpenSearch?.()
        return
      }

      // Slash `/` -> Open search
      if (event.key === '/' && !event.shiftKey && !event.metaKey && !event.ctrlKey) {
        event.preventDefault()
        onOpenSearch?.()
        return
      }

      // Question mark `?` (Shift + /) -> Open shortcuts dialog
      if (event.key === '?' || (event.key === '/' && event.shiftKey)) {
        event.preventDefault()
        onOpenShortcuts?.()
        return
      }

      // Two-key chord sequences starting with `g` (GitHub pattern)
      const key = event.key.toLowerCase()

      if (pendingPrefixRef.current === 'g') {
        // Clear pending prefix
        pendingPrefixRef.current = null
        if (timerRef.current) {
          clearTimeout(timerRef.current)
          timerRef.current = null
        }

        switch (key) {
          case 'i':
            event.preventDefault()
            void navigate('/incidents')
            break
          case 'a':
            event.preventDefault()
            void navigate('/agents')
            break
          case 'p':
            event.preventDefault()
            void navigate('/approvals')
            break
          case 'r':
            event.preventDefault()
            void navigate('/runbooks')
            break
          case 's':
            event.preventDefault()
            void navigate('/settings')
            break
          default:
            break
        }
        return
      }

      // Start sequence when `g` is pressed alone
      if (key === 'g' && !event.metaKey && !event.ctrlKey && !event.altKey) {
        pendingPrefixRef.current = 'g'
        timerRef.current = setTimeout(() => {
          pendingPrefixRef.current = null
        }, 1200)
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => {
      window.removeEventListener('keydown', handleKeyDown)
      if (timerRef.current) {
        clearTimeout(timerRef.current)
      }
    }
  }, [navigate, onOpenSearch, onOpenShortcuts, onEscape])
}
