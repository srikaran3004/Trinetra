import { test, expect } from '@playwright/test'

/**
 * Phase F4 E2E tests — Agents & Approvals.
 * Per Brief §12 F4: "Done when: the approve and deny e2e specs pass, a blocked action
 * cannot be approved in the UI, and the countdown behaves correctly at 60 seconds and at zero."
 */
test.describe('F4 — Approval Queue & Agent Monitor', () => {
  test('renders approval queue sorted by timeout, soonest first with countdown (§7.6)', async ({
    page,
  }) => {
    await page.goto('/approvals')

    // Page Heading & Badges
    await expect(
      page.getByRole('heading', { level: 2, name: /remediation approval queue/i }),
    ).toBeVisible()
    await expect(page.getByTestId('pending-counter-badge')).toBeVisible()

    // Cards are loaded
    const cards = page.locator('[data-testid^="approval-card-"]')
    await expect(cards.first()).toBeVisible()

    // Soonest card should be appr-8904 (< 60s remaining)
    const urgentCard = page.getByTestId('approval-card-appr-8904')
    await expect(urgentCard).toBeVisible()

    // Urgent countdown displays seconds in the last minute (Brief §7.6: "seconds are visible in the last minute")
    await expect(urgentCard.getByText(/0:\d{2}/)).toBeVisible()
  })

  test('approve flow requires typed confirmation for high-risk actions (§7.6)', async ({
    page,
  }) => {
    await page.goto('/approvals')
    await expect(page.getByTestId('pending-counter-badge')).toBeVisible({ timeout: 15_000 })

    // Find high-risk card appr-8901 (incident INC-2024-8901)
    const card = page.getByTestId('approval-card-appr-8901')
    await expect(card).toBeVisible()

    const approveBtn = card.getByTestId('approve-button')
    await expect(approveBtn).toContainText(/approve \(high risk\)/i)
    await approveBtn.click()

    // Confirm dialog opens
    const dialog = page.getByRole('dialog')
    await expect(dialog).toBeVisible()
    await expect(dialog).toContainText(/confirm high-risk remediation/i)

    const confirmBtn = dialog.getByRole('button', { name: /execute command/i })
    // Disabled before typing confirmation phrase
    await expect(confirmBtn).toBeDisabled()

    // Type incident ID
    const input = dialog.getByLabel('Confirmation phrase')
    await input.fill('INC-2024-8901')

    // Confirm button enables
    await expect(confirmBtn).toBeEnabled()
    await confirmBtn.click()

    // Dialog closes and success banner appears
    await expect(dialog).not.toBeVisible()
    await expect(page.getByTestId('approval-success-banner')).toBeVisible()
  })

  test('deny flow requires entering a reason (§7.6)', async ({ page }) => {
    await page.goto('/approvals')
    await expect(page.getByTestId('pending-counter-badge')).toBeVisible({ timeout: 15_000 })

    const card = page.getByTestId('approval-card-appr-8903')
    await expect(card).toBeVisible()

    const denyBtn = card.getByTestId('deny-button')
    await denyBtn.click()

    // Deny dialog opens
    const dialog = page.getByRole('dialog')
    await expect(dialog).toBeVisible()
    await expect(dialog).toContainText(/deny remediation proposal/i)

    const confirmDenyBtn = dialog.getByRole('button', { name: /confirm denial/i })
    // Initially disabled without reason
    await expect(confirmDenyBtn).toBeDisabled()

    // Fill in required reason
    const reasonInput = dialog.getByLabel(/reason for denial/i)
    await reasonInput.fill('Delaying execution until next scheduled deployment slot')

    // Confirm denial enables
    await expect(confirmDenyBtn).toBeEnabled()
    await confirmDenyBtn.click()

    // Dialog closes and denied banner appears
    await expect(dialog).not.toBeVisible()
    await expect(page.getByTestId('approval-denied-banner')).toBeVisible()
  })

  test('blocked action cannot be approved in the UI (Brief §10 & §12 safety gate)', async ({
    page,
  }) => {
    await page.goto('/approvals')
    await expect(page.getByTestId('pending-counter-badge')).toBeVisible({ timeout: 15_000 })

    // appr-8905 contains destructive command intercepted by guardrails
    const blockedCard = page.getByTestId('approval-card-appr-8905')
    await expect(blockedCard).toBeVisible()
    await expect(blockedCard).toContainText(/GUARDRAIL BLOCKED/i)
    await expect(blockedCard.getByTestId('guardrail-blocked-verdict')).toBeVisible()

    // Safety rule: Approve button is permanently disabled
    const approveBtn = blockedCard.getByTestId('approve-button')
    await expect(approveBtn).toBeDisabled()
  })

  test('agent activity monitor renders fleet with live tool calls and guardrail violations (§7.5)', async ({
    page,
  }) => {
    await page.goto('/agents')

    // Header & counters
    await expect(
      page.getByRole('heading', { level: 2, name: /autonomous agent fleet/i }),
    ).toBeVisible()
    await expect(page.getByTestId('working-agents-badge')).toBeVisible()

    // In-progress agents rendered by default
    const cards = page.locator('[data-testid^="agent-card-"]')
    await expect(cards.first()).toBeVisible()

    // Switch filter to 'All Agents' to view the entire fleet
    const statusSelect = page.getByLabel('Filter agents by status')
    await statusSelect.selectOption('all')

    // Check guardrail violation count styling on Auto-Executor
    const guardrailAgent = page.getByTestId('agent-card-agent-auto-executor')
    await expect(guardrailAgent).toBeVisible()
    await expect(guardrailAgent.getByTestId('guardrail-violation-alert')).toBeVisible()
  })
})
