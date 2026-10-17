import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './styles/tokens.css'
import App from './App.tsx'

async function enableMocking() {
  // MSW started in main.tsx when VITE_MOCK_API is true (default in dev per Brief §10 & F2.3)
  const shouldMock =
    import.meta.env.VITE_MOCK_API === 'true' ||
    (import.meta.env.DEV && import.meta.env.VITE_MOCK_API !== 'false')

  if (!shouldMock) {
    return
  }

  const { worker } = await import('./mocks/browser')
  await worker.start({
    onUnhandledRequest: 'bypass',
    serviceWorker: {
      url: '/mockServiceWorker.js',
    },
  })
}

void enableMocking().finally(() => {
  const root = document.getElementById('root')
  if (!root) {
    throw new Error('Root element not found. Make sure index.html has <div id="root">.')
  }

  createRoot(root).render(
    <StrictMode>
      <App />
    </StrictMode>,
  )
})
