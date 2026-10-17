import { useEffect } from 'react'
import { RouterProvider } from 'react-router'
import { QueryClientProvider } from '@tanstack/react-query'
import { useThemeStore } from './store/theme'
import { AuthProvider } from './auth/auth-context'
import { RealtimeProvider } from './realtime/realtime-context'
import { queryClient } from './lib/query-client'
import { router } from './router'

/**
 * App — Root application component with QueryClientProvider, AuthProvider, RealtimeProvider, RouterProvider and theme lifecycle.
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
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <RealtimeProvider>
          <RouterProvider router={router} />
        </RealtimeProvider>
      </AuthProvider>
    </QueryClientProvider>
  )
}
