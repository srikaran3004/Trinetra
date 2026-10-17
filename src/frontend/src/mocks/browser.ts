/**
 * Trinetra MSW Browser Worker
 *
 * Intercepts network requests in the browser during local development and E2E tests.
 */
import { setupWorker } from 'msw/browser'
import { handlers } from './handlers'

export const worker = setupWorker(...handlers)
