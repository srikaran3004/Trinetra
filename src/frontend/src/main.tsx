import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './styles/tokens.css'
import App from './App.tsx'

const root = document.getElementById('root')
if (!root) {
  throw new Error('Root element not found. Make sure index.html has <div id="root">.')
}

createRoot(root).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
