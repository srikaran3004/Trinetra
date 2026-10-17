import { useState, useEffect, useRef } from 'react'
import { useNavigate } from 'react-router'
import {
  ModalOverlay as RACModalOverlay,
  Modal as RACModal,
  Dialog as RACDialog,
} from 'react-aria-components'
import {
  Search,
  AlertTriangle,
  Cpu,
  CheckSquare,
  BookOpen,
  Settings,
  Sun,
  Moon,
  HelpCircle,
  ArrowRight,
} from 'lucide-react'
import { useThemeStore } from '@/store/theme'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/cn'

interface CommandPaletteProps {
  isOpen: boolean
  onOpenChange: (open: boolean) => void
  onOpenShortcuts: () => void
}

interface PaletteItem {
  id: string
  title: string
  subtitle?: string
  category: 'Navigation' | 'Incidents' | 'Actions'
  icon: typeof Search
  action: () => void
  badge?: {
    text: string
    variant: 'critical' | 'high' | 'neutral'
  }
}

export function CommandPalette({ isOpen, onOpenChange, onOpenShortcuts }: CommandPaletteProps) {
  const navigate = useNavigate()
  const { resolvedTheme, setTheme } = useThemeStore()
  const [query, setQuery] = useState('')
  const [selectedIndex, setSelectedIndex] = useState(0)
  const inputRef = useRef<HTMLInputElement>(null)

  // Clear query on open
  useEffect(() => {
    if (isOpen) {
      setQuery('')
      setSelectedIndex(0)
      setTimeout(() => inputRef.current?.focus(), 50)
    }
  }, [isOpen])

  // Build items list
  const allItems: PaletteItem[] = [
    // Navigation
    {
      id: 'nav-incidents',
      title: 'Active Incidents',
      subtitle: 'View active alerts and running investigations',
      category: 'Navigation',
      icon: AlertTriangle,
      action: () => {
        void navigate('/incidents')
        onOpenChange(false)
      },
    },
    {
      id: 'nav-history',
      title: 'Incident History',
      subtitle: 'Past incident post-mortems and MTTR metrics',
      category: 'Navigation',
      icon: AlertTriangle,
      action: () => {
        void navigate('/incidents/history')
        onOpenChange(false)
      },
    },
    {
      id: 'nav-agents',
      title: 'Agent Activity Monitor',
      subtitle: 'Inspect live tools, telemetry, and guardrail enforcement',
      category: 'Navigation',
      icon: Cpu,
      action: () => {
        void navigate('/agents')
        onOpenChange(false)
      },
    },
    {
      id: 'nav-approvals',
      title: 'Approval Queue',
      subtitle: 'Review pending high and medium-risk remediation steps',
      category: 'Navigation',
      icon: CheckSquare,
      action: () => {
        void navigate('/approvals')
        onOpenChange(false)
      },
      badge: { text: '1 Pending', variant: 'high' },
    },
    {
      id: 'nav-runbooks',
      title: 'Runbooks Catalog',
      subtitle: 'Browse automated incident playbooks and procedures',
      category: 'Navigation',
      icon: BookOpen,
      action: () => {
        void navigate('/runbooks')
        onOpenChange(false)
      },
    },
    {
      id: 'nav-settings',
      title: 'Settings',
      subtitle: 'Preferences, theme, and keyboard shortcuts',
      category: 'Navigation',
      icon: Settings,
      action: () => {
        void navigate('/settings')
        onOpenChange(false)
      },
    },
    // Seed Incidents
    {
      id: 'inc-8901',
      title: 'INC-2024-8901: Database connection pool exhaustion',
      subtitle: 'Critical incident awaiting remediation approval',
      category: 'Incidents',
      icon: AlertTriangle,
      action: () => {
        void navigate('/incidents/INC-2024-8901')
        onOpenChange(false)
      },
      badge: { text: 'CRITICAL', variant: 'critical' },
    },
    {
      id: 'inc-8902',
      title: 'INC-2024-8902: Kafka consumer lag surge on payment-svc',
      subtitle: 'High severity incident under parallel investigation',
      category: 'Incidents',
      icon: AlertTriangle,
      action: () => {
        void navigate('/incidents/INC-2024-8902')
        onOpenChange(false)
      },
      badge: { text: 'HIGH', variant: 'high' },
    },
    // Actions
    {
      id: 'action-theme',
      title: `Switch to ${resolvedTheme === 'dark' ? 'Light' : 'Dark'} Theme`,
      subtitle: 'Toggle console appearance',
      category: 'Actions',
      icon: resolvedTheme === 'dark' ? Sun : Moon,
      action: () => {
        setTheme(resolvedTheme === 'dark' ? 'light' : 'dark')
        onOpenChange(false)
      },
    },
    {
      id: 'action-shortcuts',
      title: 'View Keyboard Shortcuts',
      subtitle: 'Display cheat sheet of shortcuts and chords',
      category: 'Actions',
      icon: HelpCircle,
      action: () => {
        onOpenChange(false)
        onOpenShortcuts()
      },
    },
  ]

  // Filter items based on user query
  const filteredItems = allItems.filter((item) => {
    const q = query.toLowerCase().trim()
    if (!q) return true
    return (
      item.title.toLowerCase().includes(q) ||
      item.subtitle?.toLowerCase().includes(q) ||
      item.category.toLowerCase().includes(q)
    )
  })

  // Keyboard navigation within list
  function handleKeyDown(e: React.KeyboardEvent) {
    if (e.key === 'ArrowDown') {
      e.preventDefault()
      setSelectedIndex((prev) => (prev + 1 < filteredItems.length ? prev + 1 : 0))
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      setSelectedIndex((prev) => (prev - 1 >= 0 ? prev - 1 : filteredItems.length - 1))
    } else if (e.key === 'Enter') {
      e.preventDefault()
      const selected = filteredItems[selectedIndex]
      if (selected) {
        selected.action()
      }
    }
  }

  return (
    <RACModalOverlay
      isOpen={isOpen}
      onOpenChange={onOpenChange}
      isDismissable
      className={({ isEntering, isExiting }) =>
        cn(
          'fixed inset-0 z-[var(--z-overlay)] bg-black/60 backdrop-blur-xs flex items-start justify-center pt-[12vh] p-4',
          'transition-opacity duration-150',
          isEntering && 'opacity-100',
          isExiting && 'opacity-0',
        )
      }
    >
      <RACModal
        className={({ isEntering, isExiting }) =>
          cn(
            'w-full max-w-xl bg-[var(--color-surface-raised)] border border-[var(--color-border-strong)]',
            'rounded-[var(--radius-lg)] shadow-[var(--shadow-modal)] outline-none overflow-hidden',
            'transition-transform duration-150',
            isEntering && 'scale-100',
            isExiting && 'scale-95',
          )
        }
      >
        <RACDialog className="relative outline-none flex flex-col" aria-label="Command Palette">
          {/* Search Header */}
          <div className="flex items-center gap-[var(--space-3)] px-[var(--space-4)] py-[var(--space-3)] border-b border-[var(--color-border-subtle)]">
            <Search size={18} className="text-[var(--color-text-tertiary)] shrink-0" />
            <input
              ref={inputRef}
              type="text"
              value={query}
              onChange={(e) => {
                setQuery(e.target.value)
                setSelectedIndex(0)
              }}
              onKeyDown={handleKeyDown}
              placeholder="Type a command, incident ID, or section..."
              className="flex-1 bg-transparent text-[var(--text-sm)] text-[var(--color-text-primary)] placeholder:text-[var(--color-text-tertiary)] outline-none border-none font-sans"
              aria-autocomplete="list"
              aria-controls="command-palette-results"
            />
            <kbd className="px-1.5 py-0.5 font-mono text-[10px] text-[var(--color-text-tertiary)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] rounded-[var(--radius-xs)]">
              ESC
            </kbd>
          </div>

          {/* Results List */}
          <div
            id="command-palette-results"
            role="listbox"
            className="max-h-[360px] overflow-y-auto p-[var(--space-2)] space-y-[var(--space-1)]"
          >
            {filteredItems.length === 0 ? (
              <div className="py-8 text-center text-[var(--text-sm)] text-[var(--color-text-tertiary)]">
                No matching results found for &ldquo;{query}&rdquo;
              </div>
            ) : (
              filteredItems.map((item, index) => {
                const isSelected = index === selectedIndex
                const Icon = item.icon

                return (
                  <button
                    type="button"
                    key={item.id}
                    role="option"
                    tabIndex={isSelected ? 0 : -1}
                    aria-selected={isSelected}
                    onClick={() => {
                      item.action()
                    }}
                    onMouseEnter={() => {
                      setSelectedIndex(index)
                    }}
                    className={cn(
                      'w-full text-start flex items-center justify-between gap-[var(--space-3)] px-[var(--space-3)] py-[var(--space-2)]',
                      'rounded-[var(--radius-xs)] cursor-pointer select-none transition-colors duration-[var(--duration-fast)] border-none outline-none',
                      isSelected
                        ? 'bg-[var(--color-surface-hover)] text-[var(--color-text-primary)]'
                        : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-hover)] bg-transparent',
                    )}
                  >
                    <div className="flex items-center gap-[var(--space-3)] min-w-0">
                      <div
                        className={cn(
                          'p-1.5 rounded-[var(--radius-xs)] shrink-0',
                          isSelected
                            ? 'bg-[var(--color-surface-raised)] text-[var(--color-accent)]'
                            : 'text-[var(--color-text-tertiary)]',
                        )}
                      >
                        <Icon size={16} />
                      </div>
                      <div className="min-w-0">
                        <div className="text-[var(--text-sm)] font-medium text-[var(--color-text-primary)] truncate">
                          {item.title}
                        </div>
                        {item.subtitle && (
                          <div className="text-[var(--text-xs)] text-[var(--color-text-tertiary)] truncate">
                            {item.subtitle}
                          </div>
                        )}
                      </div>
                    </div>

                    <div className="flex items-center gap-2 shrink-0">
                      {item.badge && (
                        <Badge
                          variant={item.badge.variant}
                          size="sm"
                          className="text-[10px] uppercase"
                        >
                          {item.badge.text}
                        </Badge>
                      )}
                      {isSelected && (
                        <ArrowRight size={14} className="text-[var(--color-text-tertiary)]" />
                      )}
                    </div>
                  </button>
                )
              })
            )}
          </div>

          {/* Footer Bar */}
          <div className="flex items-center justify-between px-[var(--space-4)] py-[var(--space-2)] border-t border-[var(--color-border-subtle)] text-[11px] text-[var(--color-text-tertiary)] bg-[var(--color-surface-raised)]">
            <div className="flex items-center gap-[var(--space-3)]">
              <span>
                Navigate <kbd className="font-mono">↑↓</kbd>
              </span>
              <span>
                Select <kbd className="font-mono">↵</kbd>
              </span>
            </div>
            <span>Trinetra Command Palette</span>
          </div>
        </RACDialog>
      </RACModal>
    </RACModalOverlay>
  )
}
