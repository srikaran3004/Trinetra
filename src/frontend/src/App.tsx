import { useEffect } from 'react'
import { RouterProvider } from 'react-router'
import { useThemeStore } from './store/theme'
import { router } from './router'

/**
 * App — Root application component with RouterProvider and theme lifecycle.
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

  return <RouterProvider router={router} />
}
