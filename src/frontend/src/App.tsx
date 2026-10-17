import { useEffect } from 'react'
import { RouterProvider } from 'react-router'
import { useThemeStore } from './store/theme'
import { AuthProvider } from './auth/auth-context'
import { RealtimeProvider } from './realtime/realtime-context'
import { router } from './router'

/**
 * App — Root application component with AuthProvider, RealtimeProvider, RouterProvider and theme lifecycle.
 */
export default function App() {
  const { resolvedTheme, initTheme } = useThemeStore()

  // Initialize theme from system preference or persisted preference
  useEffect(() => {
    initTheme()
  }, [initTheme])

  // Apply resolved theme to <html> element
  useEffect(() => {
    document.documentElement.setAttribute('data-theme', resolvedTheme)
  }, [resolvedTheme])

  return (
    <AuthProvider>
      <RealtimeProvider>
        <RouterProvider router={router} />
      </RealtimeProvider>
    </AuthProvider>
  )
}
