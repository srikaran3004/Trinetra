import { Card } from '@/components/ui/card'
import { useThemeStore } from '@/store/theme'
import { SegmentedControl } from '@/components/ui/segmented-control'

export function SettingsPage() {
  const { theme, setTheme } = useThemeStore()

  return (
    <div className="p-[var(--space-6)] max-w-7xl mx-auto space-y-[var(--space-6)]">
      <div>
        <h2 className="text-[var(--text-xl)] font-bold text-[var(--color-text-primary)] m-0">
          Settings & Preferences
        </h2>
        <p className="text-[var(--text-sm)] text-[var(--color-text-secondary)] mt-1">
          Configure interface theme, accessibility, and platform preferences.
        </p>
      </div>

      <div className="space-y-[var(--space-4)] max-w-xl">
        <Card className="p-[var(--space-4)] space-y-[var(--space-3)]">
          <h3 className="text-[var(--text-sm)] font-semibold text-[var(--color-text-primary)]">
            Interface Theme
          </h3>
          <p className="text-[var(--text-xs)] text-[var(--color-text-secondary)]">
            Choose your preferred color theme or synchronize with your operating system settings.
          </p>
          <SegmentedControl
            aria-label="Interface Theme"
            options={[
              { id: 'dark', label: 'Dark' },
              { id: 'light', label: 'Light' },
              { id: 'system', label: 'System' },
            ]}
            value={theme}
            onChange={(val) => {
              setTheme(val as 'dark' | 'light' | 'system')
            }}
          />
        </Card>
      </div>
    </div>
  )
}
