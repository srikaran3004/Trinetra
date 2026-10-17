import { test, expect } from '@playwright/test'

/**
 * Phase F3 E2E test — Incidents active list & detail views.
 * Per Brief §12 F3: "Done when: the seeded incidents render, the guardrail-triggered timeline
 * entry is distinct and labeled, and the detail e2e spec passes."
 */
test.describe('F3 — Incidents & Detail View', () => {
  test('renders active incidents list with seeded data and filters', async ({ page }) => {
    await page.goto('/incidents')

    // Page heading
    await expect(page.getByRole('heading', { level: 2, name: /active incidents/i })).toBeVisible()

    // Seeded incident INC-2024-8901 is visible
    const incidentItem = page.locator('[aria-label*="INC-2024-8901"]:visible')
    await expect(incidentItem).toBeVisible()
    await expect(incidentItem).toContainText(/database connection pool exhaustion in auth-api/i)

    // Test severity filtering
    const severitySelect = page.getByLabel('Filter by severity')
    await severitySelect.selectOption('critical')

    // URL is updated with search param
    await expect(page).toHaveURL(/severity=critical/)

    // Critical incident remains visible
    await expect(incidentItem).toBeVisible()
  })

  test('navigates from incidents list to incident detail page', async ({ page }) => {
    await page.goto('/incidents')

    // Click on the first incident row/card
    const firstIncident = page.locator('[aria-label*="INC-2024-8901"]:visible')
    await expect(firstIncident).toBeVisible()
    await firstIncident.click()

    // Detail route opened
    await expect(page).toHaveURL(/\/incidents\/INC-2024-8901/)

    // Detail view elements rendered
    await expect(
      page.getByRole('heading', {
        level: 2,
        name: /database connection pool exhaustion in auth-api/i,
      }),
    ).toBeVisible()

    // StateProgress rendered
    await expect(page.getByRole('region', { name: /incident state progress/i })).toBeVisible()

    // Root cause and 0.80 threshold
    await expect(page.getByText(/primary diagnostic verdict/i)).toBeVisible()
    await expect(page.getByText('92%')).toBeVisible()

    // Remediation options with backend verdict displayed verbatim
    await expect(page.getByText(/proposed remediation plans/i)).toBeVisible()
    await expect(page.getByText(/requires human approval/i).first()).toBeVisible()
    await expect(page.getByText(/blocked by guardrail/i).first()).toBeVisible()
  })

  test('renders guardrail-triggered timeline entry with distinct visual styling and text label (§7.4 & §10)', async ({
    page,
  }) => {
    await page.goto('/incidents/INC-2024-8899')

    // Verify incident heading
    await expect(
      page.getByRole('heading', {
        level: 2,
        name: /memory leak and unhandled exception in order-processor worker/i,
      }),
    ).toBeVisible()

    // Verify distinct guardrail text badge
    const guardrailBadge = page.getByText('GUARDRAIL TRIGGERED')
    await expect(guardrailBadge).toBeVisible()

    // Verify action description
    await expect(
      page.getByText(/mandatory guardrail interception: blocked destructive command pattern/i),
    ).toBeVisible()

    // Inspect payload
    const inspectBtn = page
      .getByTestId('timeline-event-evt-8899-04')
      .getByRole('button', { name: /inspect payload/i })
    await inspectBtn.click()

    // Interception explanation and blocked command pattern visible
    await expect(page.getByText(/BLOCKED by Non-Bypassable Policy RG-01 & RG-04/i)).toBeVisible()
    await expect(page.getByText(/rm -rf \/var\/log\/orders\/\* && kill -9 1/i)).toBeVisible()
  })

  test('renders escalated branch in StateProgress when incident is escalated', async ({ page }) => {
    await page.goto('/incidents/INC-2024-8902')

    // Escalated banner alert is visible
    await expect(page.getByRole('alert')).toBeVisible()
    await expect(page.getByText(/escalated to human on-call/i)).toBeVisible()
    await expect(page.getByText('ESCALATED BRANCH')).toBeVisible()
  })
})
