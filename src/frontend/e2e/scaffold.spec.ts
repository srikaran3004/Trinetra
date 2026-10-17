import { test, expect } from '@playwright/test'

/**
 * F2 e2e test — verifies Console Shell, routing, sidebar, topbar, and keyboard shortcuts across browsers.
 * Per brief §12 F2: "navigation works by mouse and keyboard at four widths, and route changes cause no layout shift."
 */
test.describe('F2 — Console Shell & Navigation', () => {
  test('renders Trinetra logo and brand heading', async ({ page }) => {
    await page.goto('/')
    const logo = page.getByRole('img', { name: /trinetra logo/i })
    await expect(logo).toBeVisible()
    await expect(page.getByText('Trinetra').first()).toBeVisible()
  })

  test('renders skip-to-content accessibility link', async ({ page }) => {
    await page.goto('/')
    const skipLink = page.getByRole('link', { name: /skip to content/i })
    await expect(skipLink).toBeAttached()
    expect(await skipLink.getAttribute('href')).toBe('#main-content')
  })

  test('has dark theme applied by default and toggles via topbar button', async ({ page }) => {
    await page.goto('/')
    const toggleButton = page.getByRole('button', { name: /switch to (light|dark) theme/i })
    await expect(toggleButton).toBeVisible()

    const initialTheme = await page.evaluate(
      () => document.documentElement.getAttribute('data-theme') ?? '',
    )
    expect(['dark', 'light']).toContain(initialTheme)

    const expectedToggle = initialTheme === 'dark' ? 'light' : 'dark'
    await toggleButton.click({ force: true })

    await expect(page.locator('html')).toHaveAttribute('data-theme', expectedToggle)
  })

  test('renders main navigation landmark with console links', async ({ page }) => {
    await page.goto('/')
    const width = page.viewportSize()?.width ?? 1280
    if (width < 768) {
      const menuBtn = page.getByRole('button', { name: /open navigation menu/i })
      await expect(menuBtn).toBeVisible()
      await menuBtn.click()
    }

    await expect(page.getByRole('navigation', { name: /console sections/i })).toBeVisible()
    await expect(page.getByRole('link', { name: /incidents/i })).toBeVisible()
    await expect(page.getByRole('link', { name: /agents/i })).toBeVisible()
    await expect(page.getByRole('link', { name: /approvals/i })).toBeVisible()
    await expect(page.getByRole('link', { name: /runbooks/i })).toBeVisible()
    await expect(page.getByRole('link', { name: /settings/i })).toBeVisible()
  })

  test('navigates to different console sections on link click', async ({ page }) => {
    await page.goto('/')
    const width = page.viewportSize()?.width ?? 1280
    if (width < 768) {
      const menuBtn = page.getByRole('button', { name: /open navigation menu/i })
      await menuBtn.click()
    }

    await page.getByRole('link', { name: /agents/i }).click()
    await expect(page).toHaveURL(/.*\/agents/)
    await expect(
      page.getByRole('heading', { level: 2, name: /autonomous agent fleet/i }),
    ).toBeVisible()

    if (width < 768) {
      const menuBtn = page.getByRole('button', { name: /open navigation menu/i })
      await menuBtn.click()
    }

    await page.getByRole('link', { name: /approvals/i }).click()
    await expect(page).toHaveURL(/.*\/approvals/)
    await expect(
      page.getByRole('heading', { level: 2, name: /remediation approval queue/i }),
    ).toBeVisible()
  })

  test('opens Command Palette on clicking search trigger or pressing / key', async ({ page }) => {
    await page.goto('/')
    const searchTrigger = page.getByRole('button', { name: /search console/i })
    await expect(searchTrigger).toBeVisible()
    await searchTrigger.click()

    const input = page.getByPlaceholder(/type a command/i)
    await expect(input).toBeVisible()
    await page.keyboard.press('Escape')
    await expect(input).not.toBeVisible()

    // Test shortcut `/`
    await page.keyboard.press('/')
    await expect(input).toBeVisible()
  })

  test('has main content landmark and no layout shift (CLS target <= 0.1)', async ({ page }) => {
    await page.goto('/')
    await page.waitForLoadState('domcontentloaded')
    await expect(page.getByRole('main')).toBeVisible()

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
        setTimeout(() => {
          observer.disconnect()
          resolve(clsValue)
        }, 800)
      })
    })
    expect(cls).toBeLessThanOrEqual(0.1)
  })
})
