import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export type Theme = 'system' | 'dark' | 'light'
export type ResolvedTheme = 'dark' | 'light'

interface ThemeState {
  /** User's explicit preference */
  theme: Theme
  /** Resolved actual theme (system preference resolved) */
  resolvedTheme: ResolvedTheme
  /** Initialize theme from persisted preference and system preference */
  initTheme: () => void
  /** Set explicit theme preference */
  setTheme: (theme: Theme) => void
}

function getSystemTheme(): ResolvedTheme {
  if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') return 'dark'
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

function resolveTheme(theme: Theme): ResolvedTheme {
  if (theme === 'system') return getSystemTheme()
  return theme
}

export const useThemeStore = create<ThemeState>()(
  persist(
    (set, get) => ({
      theme: 'system',
      resolvedTheme: 'dark',

      initTheme() {
        const current = get().theme
        const resolved = resolveTheme(current)
        set({ resolvedTheme: resolved })

        // Listen for system theme changes if user chose 'system'
        if (typeof window !== 'undefined' && typeof window.matchMedia === 'function') {
          const mq = window.matchMedia('(prefers-color-scheme: dark)')
          mq.addEventListener('change', () => {
            if (get().theme === 'system') {
              set({ resolvedTheme: getSystemTheme() })
            }
          })
        }
      },

      setTheme(theme) {
        const resolved = resolveTheme(theme)
        set({ theme, resolvedTheme: resolved })
        document.documentElement.setAttribute('data-theme', resolved)
      },
    }),
    {
      name: 'trinetra-theme',
      // Persist only the user's explicit preference, not the resolved theme
      partialize: (state) => ({ theme: state.theme }),
    },
  ),
)
