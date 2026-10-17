import '@testing-library/jest-dom'
import { afterEach } from 'vitest'

// Mock matchMedia for JSDOM
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  }),
})

// Reset all mocks after each test
afterEach(() => {
  // MSW will be configured in src/mocks/server.ts (added in F2)
})
