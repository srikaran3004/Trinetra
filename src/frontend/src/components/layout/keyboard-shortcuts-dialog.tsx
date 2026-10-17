import { Dialog } from '@/components/ui/dialog'
import { KeyboardShortcut } from '@/components/ui/keyboard-shortcut'

interface KeyboardShortcutsDialogProps {
  isOpen: boolean
  onOpenChange: (open: boolean) => void
}

interface ShortcutItem {
  keys: string[]
  description: string
  sequence?: boolean
}

interface ShortcutGroup {
  category: string
  items: ShortcutItem[]
}

const shortcutGroups: ShortcutGroup[] = [
  {
    category: 'Navigation (Chords)',
    items: [
      { keys: ['g', 'i'], sequence: true, description: 'Go to Incidents' },
      { keys: ['g', 'a'], sequence: true, description: 'Go to Agents' },
      { keys: ['g', 'p'], sequence: true, description: 'Go to Approvals' },
      { keys: ['g', 'r'], sequence: true, description: 'Go to Runbooks' },
      { keys: ['g', 's'], sequence: true, description: 'Go to Settings' },
    ],
  },
  {
    category: 'Search & Palette',
    items: [
      { keys: ['/'], description: 'Focus search' },
      { keys: ['Ctrl', 'K'], description: 'Open Command Palette' },
      { keys: ['Esc'], description: 'Close search or modal' },
    ],
  },
  {
    category: 'General',
    items: [
      { keys: ['?'], description: 'Open this shortcuts helper' },
      { keys: ['Tab'], description: 'Navigate through focusable elements' },
      { keys: ['Enter'], description: 'Select / Confirm action' },
    ],
  },
]

export function KeyboardShortcutsDialog({ isOpen, onOpenChange }: KeyboardShortcutsDialogProps) {
  return (
    <Dialog
      isOpen={isOpen}
      onOpenChange={onOpenChange}
      title="Keyboard Shortcuts"
      description="Navigate Trinetra using keyboard shortcuts and chord sequences (GitHub pattern)."
      cancelLabel="Close"
      confirmLabel="Done"
    >
      <div className="space-y-[var(--space-4)] pt-[var(--space-2)]">
        {shortcutGroups.map((group) => (
          <div key={group.category} className="space-y-[var(--space-2)]">
            <h3 className="text-[var(--text-xs)] font-semibold uppercase tracking-[var(--tracking-label)] text-[var(--color-text-tertiary)] m-0">
              {group.category}
            </h3>
            <div className="divide-y divide-[var(--color-border-subtle)] rounded-[var(--radius-sm)] border border-[var(--color-border-subtle)] bg-[var(--color-canvas)]">
              {group.items.map((item, idx) => (
                <div
                  key={idx}
                  className="flex items-center justify-between px-[var(--space-3)] py-[var(--space-2)] text-[var(--text-xs)]"
                >
                  <span className="text-[var(--color-text-secondary)]">{item.description}</span>
                  <div className="flex items-center gap-[var(--space-1)]">
                    {item.sequence && item.keys[0] && item.keys[1] ? (
                      <span className="flex items-center gap-[var(--space-1)]">
                        <KeyboardShortcut keys={[item.keys[0]]} />
                        <span className="text-[10px] text-[var(--color-text-tertiary)]">then</span>
                        <KeyboardShortcut keys={[item.keys[1]]} />
                      </span>
                    ) : (
                      <KeyboardShortcut keys={item.keys} />
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>
    </Dialog>
  )
}
