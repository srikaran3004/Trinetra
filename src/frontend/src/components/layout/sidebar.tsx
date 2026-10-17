import { NavLink } from 'react-router'
import {
  AlertTriangle,
  Cpu,
  CheckSquare,
  BookOpen,
  Settings,
  PanelLeftClose,
  PanelLeftOpen,
  LogOut,
  X,
} from 'lucide-react'
import { useSidebarStore } from '@/store/sidebar'
import { TrinetraLogo } from '@/components/ui/trinetra-logo'
import { Avatar } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import { Tooltip } from '@/components/ui/tooltip'
import { cn } from '@/lib/cn'

interface NavItem {
  name: string
  to: string
  icon: typeof AlertTriangle
  shortcut: string
  badge?: {
    count: number
    variant: 'critical' | 'high' | 'neutral'
  }
}

const navItems: NavItem[] = [
  {
    name: 'Incidents',
    to: '/incidents',
    icon: AlertTriangle,
    shortcut: 'g then i',
    badge: { count: 2, variant: 'critical' },
  },
  {
    name: 'Agents',
    to: '/agents',
    icon: Cpu,
    shortcut: 'g then a',
  },
  {
    name: 'Approvals',
    to: '/approvals',
    icon: CheckSquare,
    shortcut: 'g then p',
    badge: { count: 1, variant: 'high' },
  },
  {
    name: 'Runbooks',
    to: '/runbooks',
    icon: BookOpen,
    shortcut: 'g then r',
  },
  {
    name: 'Settings',
    to: '/settings',
    icon: Settings,
    shortcut: 'g then s',
  },
]

export function Sidebar() {
  const { isCollapsed, isMobileOpen, toggleCollapse, setMobileOpen } = useSidebarStore()

  return (
    <>
      {/* Mobile Backdrop Overlay */}
      {isMobileOpen && (
        <div
          role="presentation"
          aria-hidden="true"
          onClick={() => {
            setMobileOpen(false)
          }}
          className="fixed inset-0 z-[var(--z-overlay)] bg-[var(--color-surface-overlay)] backdrop-blur-sm md:hidden"
        />
      )}

      {/* Main Sidebar Shell */}
      <aside
        aria-label="Main Navigation"
        className={cn(
          'fixed inset-y-0 start-0 z-[var(--z-overlay)] md:static md:z-auto flex flex-col',
          'h-screen bg-[var(--color-surface-raised)] border-inline-end border-[var(--color-border-subtle)]',
          'transition-all duration-[var(--duration-normal)] ease-[var(--ease-standard)] select-none',
          // Desktop widths
          isCollapsed ? 'md:w-[64px]' : 'md:w-[240px]',
          // Mobile open/close transition
          isMobileOpen ? 'translate-x-0 w-[240px]' : '-translate-x-full md:translate-x-0',
        )}
      >
        {/* Brand Header */}
        <div
          className={cn(
            'flex items-center h-[56px] border-b border-[var(--color-border-subtle)] shrink-0',
            isCollapsed
              ? 'md:justify-center md:px-0 px-[var(--space-4)]'
              : 'justify-between px-[var(--space-4)]',
          )}
        >
          <div className="flex items-center gap-[var(--space-2)] overflow-hidden">
            <TrinetraLogo size={24} showText={!isCollapsed} />
          </div>

          {/* Mobile close button */}
          <button
            type="button"
            onClick={() => {
              setMobileOpen(false)
            }}
            aria-label="Close navigation"
            className="p-[var(--space-1-5)] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] rounded-[var(--radius-xs)] md:hidden cursor-pointer"
          >
            <X size={18} />
          </button>
        </div>

        {/* Navigation Items */}
        <nav
          aria-label="Console Sections"
          className="flex-1 py-[var(--space-3)] px-[var(--space-2)] space-y-[var(--space-1)] overflow-y-auto overflow-x-hidden"
        >
          {navItems.map((item) => {
            const Icon = item.icon

            const linkContent = (
              <NavLink
                key={item.to}
                to={item.to}
                onClick={() => {
                  setMobileOpen(false)
                }}
                className={({ isActive }) =>
                  cn(
                    'group relative flex items-center gap-[var(--space-3)] px-[var(--space-2-5)] py-[var(--space-2)]',
                    'rounded-[var(--radius-xs)] text-[var(--text-sm)] font-medium leading-none',
                    'transition-colors duration-[var(--duration-fast)] outline-none',
                    'focus-visible:ring-2 focus-visible:ring-[var(--color-focus-ring)]',
                    isActive
                      ? 'bg-[var(--color-surface-hover)] text-[var(--color-text-primary)] font-semibold border-inline-start-[3px] border-[var(--color-accent)]'
                      : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-hover)] hover:text-[var(--color-text-primary)] border-inline-start-[3px] border-transparent',
                    isCollapsed && 'md:justify-center md:px-0',
                  )
                }
              >
                {({ isActive }) => (
                  <>
                    <Icon
                      size={18}
                      className={cn(
                        'shrink-0 transition-colors',
                        isActive
                          ? 'text-[var(--color-accent)]'
                          : 'text-[var(--color-text-secondary)] group-hover:text-[var(--color-text-primary)]',
                      )}
                      aria-hidden="true"
                    />

                    {/* Label and Badge on expanded view */}
                    {(!isCollapsed || isMobileOpen) && (
                      <span className="flex-1 flex items-center justify-between overflow-hidden text-ellipsis whitespace-nowrap">
                        <span>{item.name}</span>
                        {item.badge && (
                          <Badge
                            variant={item.badge.variant}
                            size="sm"
                            className="font-mono text-[var(--text-xs)] px-1.5 py-0"
                          >
                            {item.badge.count}
                          </Badge>
                        )}
                      </span>
                    )}

                    {/* Dot indicator when collapsed if badge exists */}
                    {isCollapsed && !isMobileOpen && item.badge && (
                      <span
                        className={cn(
                          'hidden md:block absolute top-1.5 end-1.5 w-2 h-2 rounded-[var(--radius-full)]',
                          item.badge.variant === 'critical' && 'bg-[var(--color-critical)]',
                          item.badge.variant === 'high' && 'bg-[var(--color-high)]',
                          item.badge.variant === 'neutral' && 'bg-[var(--color-accent)]',
                        )}
                        aria-hidden="true"
                      />
                    )}
                  </>
                )}
              </NavLink>
            )

            // When collapsed on desktop, wrap in Tooltip for accessibility and discovery
            if (isCollapsed) {
              return (
                <div key={item.to} className="w-full">
                  <Tooltip
                    content={
                      <div className="flex items-center gap-[var(--space-2)]">
                        <span>{item.name}</span>
                        <kbd className="font-mono text-[var(--text-xs)] text-[var(--color-text-tertiary)]">
                          {item.shortcut}
                        </kbd>
                      </div>
                    }
                  >
                    {linkContent}
                  </Tooltip>
                </div>
              )
            }

            return linkContent
          })}
        </nav>

        {/* Footer: User profile and collapse toggle */}
        <div className="shrink-0 p-[var(--space-2)] border-t border-[var(--color-border-subtle)] bg-[var(--color-surface-raised)] space-y-[var(--space-2)]">
          {/* User Account Info */}
          <div
            className={cn(
              'flex items-center gap-[var(--space-2-5)] p-[var(--space-1-5)] rounded-[var(--radius-xs)]',
              isCollapsed && 'md:justify-center md:p-0',
            )}
          >
            <Avatar initials="SR" alt="Srikaran R" size="sm" />
            {(!isCollapsed || isMobileOpen) && (
              <div className="flex-1 min-w-0">
                <div className="text-[var(--text-xs)] font-semibold text-[var(--color-text-primary)] truncate">
                  Srikaran R
                </div>
                <div className="text-[var(--text-xs)] text-[var(--color-text-tertiary)] truncate">
                  SRE Lead
                </div>
              </div>
            )}
            {(!isCollapsed || isMobileOpen) && (
              <NavLink
                to="/login"
                aria-label="Sign out"
                className="p-1 text-[var(--color-text-tertiary)] hover:text-[var(--color-critical)] transition-colors rounded-[var(--radius-xs)]"
              >
                <LogOut size={16} />
              </NavLink>
            )}
          </div>

          {/* Desktop Collapse / Expand Toggle Button */}
          <div className="hidden md:flex justify-end">
            <button
              type="button"
              onClick={toggleCollapse}
              aria-label={isCollapsed ? 'Expand sidebar' : 'Collapse sidebar'}
              className={cn(
                'flex items-center gap-[var(--space-2)] w-full py-[var(--space-1-5)] px-[var(--space-2)]',
                'text-[var(--text-xs)] text-[var(--color-text-tertiary)] hover:text-[var(--color-text-primary)]',
                'hover:bg-[var(--color-surface-hover)] rounded-[var(--radius-xs)] transition-colors cursor-pointer',
                isCollapsed && 'justify-center px-0',
              )}
            >
              {isCollapsed ? (
                <PanelLeftOpen size={16} aria-hidden="true" />
              ) : (
                <>
                  <PanelLeftClose size={16} aria-hidden="true" />
                  <span className="flex-1 text-start">Collapse</span>
                </>
              )}
            </button>
          </div>
        </div>
      </aside>
    </>
  )
}
