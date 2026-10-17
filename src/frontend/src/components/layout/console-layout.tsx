import { useState, useEffect } from 'react'
import { Outlet, useLocation } from 'react-router'
import { motion, AnimatePresence } from 'motion/react'
import { Sidebar } from './sidebar'
import { Topbar } from './topbar'
import { CommandPalette } from './command-palette'
import { KeyboardShortcutsDialog } from './keyboard-shortcuts-dialog'
import { useKeyboardShortcuts } from '@/hooks/use-keyboard-shortcuts'
import { useRealtime, useRealtimeSubscription } from '@/realtime/realtime-context'

export function ConsoleLayout() {
  const [isSearchOpen, setIsSearchOpen] = useState(false)
  const [isShortcutsOpen, setIsShortcutsOpen] = useState(false)
  const [prefersReducedMotion, setPrefersReducedMotion] = useState(false)
  const [announcement, setAnnouncement] = useState('')
  const location = useLocation()

  // Real-time SignalR connection status
  const { isConnected } = useRealtime()

  // Screen reader incident live announcements (§4.9)
  useRealtimeSubscription('IncidentCreated', (incident) => {
    setAnnouncement(`New ${incident.severity} incident created: ${incident.title}`)
  })

  useRealtimeSubscription('ApprovalRequired', (approval) => {
    setAnnouncement(`Approval required: ${approval.actionDescription}`)
  })

  // Detect user preference for reduced motion (Brief §8, §9)
  useEffect(() => {
    if (typeof window !== 'undefined') {
      const mediaQuery = window.matchMedia('(prefers-reduced-motion: reduce)')
      setPrefersReducedMotion(mediaQuery.matches)

      const handler = (event: MediaQueryListEvent) => {
        setPrefersReducedMotion(event.matches)
      }
      mediaQuery.addEventListener('change', handler)
      return () => {
        mediaQuery.removeEventListener('change', handler)
      }
    }
  }, [])

  // Connect global keyboard shortcuts
  useKeyboardShortcuts({
    onOpenSearch: () => {
      setIsSearchOpen(true)
    },
    onOpenShortcuts: () => {
      setIsShortcutsOpen(true)
    },
    onEscape: () => {
      setIsSearchOpen(false)
      setIsShortcutsOpen(false)
    },
  })

  return (
    <div className="flex h-screen w-screen overflow-hidden bg-[var(--color-canvas)] text-[var(--color-text-primary)] font-sans antialiased">
      {/* Skip to Content link for keyboard accessibility (Brief §4.9) */}
      <a href="#main-content" className="skip-link">
        Skip to content
      </a>

      {/* Screen Reader Live Region for Incident Announcements (Brief §4.9) */}
      <div
        id="incident-live-announcer"
        role="status"
        aria-live="polite"
        aria-atomic="true"
        className="sr-only"
      >
        {announcement}
      </div>

      {/* Persistent / Responsive Sidebar */}
      <Sidebar />

      {/* Main Content Column */}
      <div className="flex flex-col flex-1 min-w-0 h-screen overflow-hidden">
        {/* Sticky Topbar */}
        <Topbar
          onOpenSearch={() => {
            setIsSearchOpen(true)
          }}
          onOpenShortcuts={() => {
            setIsShortcutsOpen(true)
          }}
          isConnected={isConnected}
        />

        {/* Content View with Route Transitions */}
        <main
          id="main-content"
          tabIndex={-1}
          className="flex-1 overflow-y-auto min-w-0 focus:outline-none"
        >
          <AnimatePresence mode="wait">
            <motion.div
              key={location.pathname}
              initial={prefersReducedMotion ? false : { opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={prefersReducedMotion ? undefined : { opacity: 0 }}
              transition={{
                duration: prefersReducedMotion ? 0 : 0.16, // 160ms fade transition per brief §8
                ease: 'easeOut',
              }}
              className="h-full"
            >
              <Outlet />
            </motion.div>
          </AnimatePresence>
        </main>
      </div>

      {/* Command Palette Modal */}
      <CommandPalette
        isOpen={isSearchOpen}
        onOpenChange={setIsSearchOpen}
        onOpenShortcuts={() => {
          setIsShortcutsOpen(true)
        }}
      />

      {/* Keyboard Shortcuts Dialog */}
      <KeyboardShortcutsDialog isOpen={isShortcutsOpen} onOpenChange={setIsShortcutsOpen} />
    </div>
  )
}
