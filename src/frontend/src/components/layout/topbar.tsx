import { useLocation } from 'react-router'
import { Menu, Search, HelpCircle, Sun, Moon } from 'lucide-react'
import { useSidebarStore } from '@/store/sidebar'
import { useThemeStore } from '@/store/theme'
import { StatusDot } from '@/components/ui/status-dot'
import { Badge } from '@/components/ui/badge'
import { IconButton } from '@/components/ui/icon-button'
import { Tooltip } from '@/components/ui/tooltip'
import { cn } from '@/lib/cn'

interface TopbarProps {
  onOpenSearch: () => void
  onOpenShortcuts: () => void
  isConnected?: boolean
}

// Map pathnames to readable section titles
function getBreadcrumbs(pathname: string): { title: string; section?: string } {
  if (pathname.startsWith('/incidents/history')) {
    return { section: 'Incidents', title: 'History' }
  }
  if (pathname.startsWith('/incidents/')) {
    const id = pathname.split('/')[2]
    return { section: 'Incidents', title: id || 'Detail' }
  }
  if (pathname.startsWith('/incidents')) {
    return { section: 'Incidents', title: 'Active Incidents' }
  }
  if (pathname.startsWith('/agents')) {
    return { section: 'Console', title: 'Agent Activity' }
  }
  if (pathname.startsWith('/approvals')) {
    return { section: 'Console', title: 'Approval Queue' }
  }
  if (pathname.startsWith('/runbooks')) {
    return { section: 'Console', title: 'Runbooks' }
  }
  if (pathname.startsWith('/settings')) {
    return { section: 'Console', title: 'Settings' }
  }
  return { section: 'Console', title: 'Overview' }
}

export function Topbar({ onOpenSearch, onOpenShortcuts, isConnected = true }: TopbarProps) {
  const { toggleMobileOpen } = useSidebarStore()
  const { resolvedTheme, setTheme } = useThemeStore()
  const location = useLocation()

  const { title, section } = getBreadcrumbs(location.pathname)

  function toggleTheme() {
    setTheme(resolvedTheme === 'dark' ? 'light' : 'dark')
  }

  return (
    <header
      role="banner"
      className={cn(
        'sticky top-0 z-[var(--z-sticky)] flex items-center justify-between',
        'h-[56px] px-[var(--space-4)] md:px-[var(--space-6)]',
        'bg-[var(--color-surface-raised)] border-b border-[var(--color-border-subtle)]',
        'backdrop-blur-md select-none',
      )}
    >
      {/* Left: Mobile menu toggle + Breadcrumbs + Demo badge */}
      <div className="flex items-center gap-[var(--space-3)]">
        {/* Mobile menu hamburger button */}
        <button
          type="button"
          onClick={toggleMobileOpen}
          aria-label="Open navigation menu"
          className="p-[var(--space-1-5)] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] rounded-[var(--radius-xs)] md:hidden cursor-pointer"
        >
          <Menu size={20} />
        </button>

        {/* Section & Breadcrumb Title */}
        <div className="flex items-center gap-[var(--space-2)] text-[var(--text-sm)]">
          {section && (
            <>
              <span className="text-[var(--color-text-tertiary)] hidden sm:inline">{section}</span>
              <span
                className="text-[var(--color-text-tertiary)] hidden sm:inline"
                aria-hidden="true"
              >
                /
              </span>
            </>
          )}
          <h1 className="font-semibold text-[var(--color-text-primary)] text-[var(--text-sm)] md:text-[var(--text-base)] m-0 leading-none">
            {title}
          </h1>
        </div>

        {/* Demo Data Environment Badge (Brief §10) */}
        <Badge
          variant="neutral"
          size="sm"
          className="bg-[var(--color-surface-hover)] text-[var(--color-text-tertiary)] border-[var(--color-border-subtle)] uppercase tracking-wider text-[10px] hidden lg:inline-flex"
        >
          Demo Data
        </Badge>
      </div>

      {/* Center / Right Controls */}
      <div className="flex items-center gap-[var(--space-2-5)]">
        {/* Global Search / Command Palette Trigger */}
        <button
          type="button"
          onClick={onOpenSearch}
          aria-label="Search console (Press / or Ctrl+K)"
          className={cn(
            'flex items-center gap-[var(--space-2)] px-[var(--space-2-5)] py-[var(--space-1-5)]',
            'bg-[var(--color-canvas)] hover:bg-[var(--color-surface-hover)]',
            'text-[var(--color-text-tertiary)] hover:text-[var(--color-text-secondary)]',
            'border border-[var(--color-border-subtle)] rounded-[var(--radius-xs)]',
            'text-[var(--text-xs)] transition-colors cursor-pointer outline-none',
            'focus-visible:ring-2 focus-visible:ring-[var(--color-focus-ring)]',
            'w-[180px] sm:w-[240px]',
          )}
        >
          <Search size={14} className="shrink-0 text-[var(--color-text-tertiary)]" />
          <span className="flex-1 text-start truncate">Search incidents, agents...</span>
          <kbd
            className={cn(
              'hidden sm:inline-block px-1.5 py-0.5 font-mono text-[10px] font-semibold',
              'text-[var(--color-text-tertiary)] bg-[var(--color-surface-raised)]',
              'border border-[var(--color-border-subtle)] rounded-[var(--radius-xs)]',
            )}
          >
            /
          </kbd>
        </button>

        {/* Real-time Connection Status Indicator */}
        <Tooltip
          content={
            isConnected
              ? 'SignalR: Connected to /hubs/incidents'
              : 'SignalR: Disconnected (reconnecting...)'
          }
        >
          <button
            type="button"
            aria-label={isConnected ? 'SignalR status: Connected' : 'SignalR status: Disconnected'}
            className="flex items-center gap-1.5 px-2 py-1 rounded-[var(--radius-xs)] cursor-help focus:outline-none focus-visible:ring-1 focus-visible:ring-[var(--color-focus-ring)] bg-transparent border-none"
          >
            <StatusDot
              status={isConnected ? 'success' : 'neutral'}
              size="sm"
              isPulsing={isConnected}
              label={isConnected ? 'SignalR connected' : 'SignalR disconnected'}
            />
            <span className="font-mono text-[11px] text-[var(--color-text-tertiary)] hidden xl:inline">
              {isConnected ? 'LIVE' : 'OFFLINE'}
            </span>
          </button>
        </Tooltip>

        {/* Keyboard Shortcuts Trigger Button */}
        <Tooltip content="Keyboard shortcuts (?)">
          <IconButton
            icon={HelpCircle}
            aria-label="Keyboard shortcuts (press ?)"
            variant="ghost"
            size="sm"
            onPress={onOpenShortcuts}
            className="text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
          />
        </Tooltip>

        {/* Theme Toggle Button */}
        <Tooltip
          content={resolvedTheme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
        >
          <IconButton
            icon={resolvedTheme === 'dark' ? Sun : Moon}
            aria-label={resolvedTheme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'}
            variant="ghost"
            size="sm"
            onPress={toggleTheme}
            className="text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]"
          />
        </Tooltip>
      </div>
    </header>
  )
}
