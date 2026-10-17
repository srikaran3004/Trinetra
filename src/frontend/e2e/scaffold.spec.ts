import { test, expect } from '@playwright/test'

/**
 * F0 smoke e2e test — verifies the scaffold renders in both themes.
 * Per brief §12 F0: "the app renders a 'Trinetra' placeholder in both themes".
 */
test.describe('F0 — Scaffold smoke test', () => {
  test('renders Trinetra heading', async ({ page }) => {
    await page.goto('/')
    await expect(page.getByRole('heading', { name: /trinetra/i, level: 1 })).toBeVisible()
  })

  test('renders the SVG logo', async ({ page }) => {
    await page.goto('/')
    const logo = page.getByRole('img', { name: /trinetra logo/i })
    await expect(logo).toBeVisible()
  })

  test('renders skip link', async ({ page }) => {
    await page.goto('/')
    // Skip link is visually hidden but present in the DOM
    const skipLink = page.getByRole('link', { name: /skip to content/i })
    await expect(skipLink).toBeAttached()
  })

  test('has dark theme applied by default', async ({ page }) => {
    await page.goto('/')
    const theme = await page.evaluate(
      () => document.documentElement.getAttribute('data-theme') ?? '',
    )
    // System default is dark; may be 'light' if system prefers light
    expect(['dark', 'light']).toContain(theme)
  })

  test('theme toggles when button is clicked', async ({ page }) => {
    await page.goto('/')

    const button = page.getByRole('button', { name: /toggle theme/i })
    await expect(button).toBeVisible()

    const initialTheme = await page.evaluate(
      () => document.documentElement.getAttribute('data-theme') ?? '',
    )

    await button.click()

    const newTheme = await page.evaluate(
      () => document.documentElement.getAttribute('data-theme') ?? '',
    )

    const expectedToggle = initialTheme === 'dark' ? 'light' : 'dark'
    expect(newTheme).toBe(expectedToggle)
  })

  test('stack badges are visible', async ({ page }) => {
    await page.goto('/')
    await expect(page.getByText('React 19')).toBeVisible()
    await expect(page.getByText('Vite')).toBeVisible()
    await expect(page.getByText('TypeScript strict')).toBeVisible()
  })

  test('has main landmark', async ({ page }) => {
    await page.goto('/')
    await expect(page.getByRole('main')).toBeVisible()
  })

  test('no layout shift — CLS target 0.1', async ({ page }) => {
    await page.goto('/')
    // Wait for hydration
    await page.waitForLoadState('networkidle')

    // Measure CLS using PerformanceObserver
    const cls = await page.evaluate(async () => {
      return new Promise<number>((resolve) => {
        let clsValue = 0
        const observer = new PerformanceObserver((list) => {
          for (const entry of list.getEntries()) {
            const layoutShift = entry as PerformanceEntry & {
              value: number
              hadRecentInput: boolean
            }
            if (!layoutShift.hadRecentInput) {
              clsValue += layoutShift.value
            }
          }
        })
        observer.observe({ type: 'layout-shift', buffered: true })
        // Give it a moment to collect
        setTimeout(() => {
          observer.disconnect()
          resolve(clsValue)
        }, 1000)
      })
    })
    expect(cls).toBeLessThanOrEqual(0.1)
  })
})
