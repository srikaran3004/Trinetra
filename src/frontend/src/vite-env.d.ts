/**
 * vite-env.d.ts — Type declarations for Vite environment variables.
 * All VITE_* vars used in the app must be declared here.
 */

/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string
  readonly VITE_MSW_ENABLED: string
  readonly VITE_DEV_BANNER: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
