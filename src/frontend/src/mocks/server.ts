/**
 * Trinetra MSW Node Server
 *
 * Intercepts network requests during Vitest unit tests in Node.js / JSDOM.
 */
import { setupServer } from 'msw/node'
import { handlers } from './handlers'

export const server = setupServer(...handlers)
