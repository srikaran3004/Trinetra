# Trinetra Frontend

React 19 + Vite + TypeScript frontend for the Trinetra autonomous IT incident response platform.

> **Note:** This README is a stub completed in Phase F7. The full documentation will be added then.

## Quick Start

```bash
cd src/frontend
npm install
npm run dev      # http://localhost:5173
```

## Scripts

| Command             | Description                  |
| ------------------- | ---------------------------- |
| `npm run dev`       | Start Vite dev server        |
| `npm run build`     | Production build             |
| `npm run lint`      | ESLint (zero warnings)       |
| `npm run format`    | Prettier format              |
| `npm run test`      | Vitest unit tests            |
| `npm run e2e`       | Playwright e2e tests         |
| `npm run storybook` | Storybook component explorer |

## Stack

- **Framework:** React 19, TypeScript strict, Vite
- **Styling:** Tailwind CSS v4 with CSS custom-property tokens
- **Accessible primitives:** react-aria-components
- **Animation:** Motion (motion/react)
- **Routing:** React Router v7
- **Server state:** TanStack Query
- **UI state:** Zustand
- **Real-time:** @microsoft/signalr
- **Tests:** Vitest + Testing Library, Playwright + axe-core
- **Docs:** Storybook + addon-a11y

## Phase

This is Phase **F0** — scaffold complete. See [HANDOFF.md](../../HANDOFF.md) for the full plan.
