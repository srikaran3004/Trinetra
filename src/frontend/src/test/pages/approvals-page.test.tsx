import { describe, it, expect, beforeEach } from 'vitest'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ApprovalsPage } from '@/pages/approvals-page'
import { AuthProvider, type User } from '@/auth/auth-context'
import { RealtimeProvider } from '@/realtime/realtime-context'
import { resetMockDatabase } from '@/mocks/handlers'
import { mockSignalREmitter } from '@/realtime/mock-signalr-emitter'

const adminUser: User = {
  id: 'usr-admin-01',
  name: 'SRE Lead (Admin)',
  email: 'admin@trinetra.internal',
  role: 'Admin',
}

const operatorUser: User = {
  id: 'usr-operator-01',
  name: 'DevOps Operator',
  email: 'operator@trinetra.internal',
  role: 'Operator',
}

const viewerUser: User = {
  id: 'usr-viewer-01',
  name: 'Security Auditor (Viewer)',
  email: 'viewer@trinetra.internal',
  role: 'Viewer',
}

function renderApprovalsPage(currentUser: User = adminUser) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        staleTime: 0,
      },
    },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider initialUser={currentUser}>
        <RealtimeProvider>
          <ApprovalsPage />
        </RealtimeProvider>
      </AuthProvider>
    </QueryClientProvider>,
  )
}

describe('Approvals Queue Page (Task F4-2 & Brief §7.6)', () => {
  beforeEach(() => {
    resetMockDatabase()
  })

  it('renders queue sorted by timeout, soonest first (Brief §7.6)', async () => {
    renderApprovalsPage(adminUser)

    // Wait for approvals list to load
    await waitFor(() => {
      expect(screen.queryByTestId('approvals-skeleton')).not.toBeInTheDocument()
    })

    const cards = screen.getAllByTestId(/^approval-card-/)
    expect(cards.length).toBeGreaterThanOrEqual(3)

    // First card should be the soonest timeout: appr-8904 (50 seconds)
    expect(cards[0]).toHaveAttribute('data-approval-id', 'appr-8904')

    // Second card: appr-8903 (8 minutes)
    expect(cards[1]).toHaveAttribute('data-approval-id', 'appr-8903')

    // Third card: appr-8901 (12 minutes)
    expect(cards[2]).toHaveAttribute('data-approval-id', 'appr-8901')
  })

  it('displays countdown with visible seconds for items under 60 seconds (Brief §7.6)', async () => {
    renderApprovalsPage(adminUser)

    await waitFor(() => {
      expect(screen.queryByTestId('approvals-skeleton')).not.toBeInTheDocument()
    })

    const urgentCard = screen.getByTestId('approval-card-appr-8904')
    // appr-8904 has 50 seconds remaining (< 60s), so formattedDisplay is 0:XX with colon
    expect(urgentCard.textContent).toMatch(/0:\d{2}/)
  })

  it('enforces role checks and disables action buttons with reasons for unauthorized users (Brief §7.6)', async () => {
    // Render as Operator: can approve operator-level (appr-8903), but cannot approve lead/admin (appr-8901, appr-8904)
    const { unmount } = renderApprovalsPage(operatorUser)

    await waitFor(() => {
      expect(screen.queryByTestId('approvals-skeleton')).not.toBeInTheDocument()
    })

    // On appr-8901 (requires Lead): buttons should be disabled with reason shown
    const leadCard = screen.getByTestId('approval-card-appr-8901')
    expect(leadCard).toHaveTextContent(/Requires SRE Lead or Admin role/i)

    const leadApproveBtn = within(leadCard).getByTestId('approve-button')
    const leadDenyBtn = within(leadCard).getByTestId('deny-button')
    expect(leadApproveBtn).toBeDisabled()
    expect(leadDenyBtn).toBeDisabled()

    // On appr-8903 (requires Operator): buttons should be ENABLED
    const operatorCard = screen.getByTestId('approval-card-appr-8903')
    const operatorApproveBtn = within(operatorCard).getByTestId('approve-button')
    const operatorDenyBtn = within(operatorCard).getByTestId('deny-button')
    expect(operatorApproveBtn).not.toBeDisabled()
    expect(operatorDenyBtn).not.toBeDisabled()

    unmount()

    // Render as Viewer: all buttons disabled
    renderApprovalsPage(viewerUser)

    await waitFor(() => {
      expect(screen.queryByTestId('approvals-skeleton')).not.toBeInTheDocument()
    })

    const allApproveBtns = screen.getAllByTestId('approve-button')
    allApproveBtns.forEach((btn) => {
      expect(btn).toBeDisabled()
    })
    expect(screen.getAllByText(/Viewer role has read-only access/i).length).toBeGreaterThan(0)
  })

  it('requires typing incident ID for high-risk approvals before enabling confirm (Brief §7.6)', async () => {
    const user = userEvent.setup()
    renderApprovalsPage(adminUser)

    await waitFor(() => {
      expect(screen.queryByTestId('approvals-skeleton')).not.toBeInTheDocument()
    })

    // Find appr-8901 (high risk, incident INC-2024-8901)
    const card = screen.getByTestId('approval-card-appr-8901')
    const approveBtn = within(card).getByTestId('approve-button')
    expect(approveBtn).toHaveTextContent(/Approve \(High Risk\)/i)

    await user.click(approveBtn)

    // Confirm dialog should open
    const dialog = screen.getByRole('dialog')
    expect(dialog).toBeInTheDocument()
    expect(dialog).toHaveTextContent(/Confirm High-Risk Remediation/i)

    const confirmBtn = within(dialog).getByRole('button', { name: /Execute Command/i })
    // Initially disabled because typed phrase does not match INC-2024-8901
    expect(confirmBtn).toBeDisabled()

    const input = within(dialog).getByLabelText(/Confirmation phrase/i)
    // Type partial or wrong phrase
    await user.type(input, 'WRONG-ID')
    expect(confirmBtn).toBeDisabled()

    // Type exact incident ID
    await user.clear(input)
    await user.type(input, 'INC-2024-8901')
    expect(confirmBtn).not.toBeDisabled()

    // Click confirm
    await user.click(confirmBtn)

    // Card should show success banner with animated checkmark
    await waitFor(() => {
      expect(screen.getByTestId('approval-success-banner')).toBeInTheDocument()
    })
  })

  it('requires entering a reason to deny an approval (Brief §7.6)', async () => {
    const user = userEvent.setup()
    renderApprovalsPage(adminUser)

    await waitFor(() => {
      expect(screen.queryByTestId('approvals-skeleton')).not.toBeInTheDocument()
    })

    const card = screen.getByTestId('approval-card-appr-8903')
    const denyBtn = within(card).getByTestId('deny-button')
    await user.click(denyBtn)

    const dialog = screen.getByRole('dialog')
    expect(dialog).toBeInTheDocument()
    expect(dialog).toHaveTextContent(/Deny Remediation Proposal/i)

    const confirmDenyBtn = within(dialog).getByRole('button', { name: /Confirm Denial/i })
    // Disabled when reason is empty
    expect(confirmDenyBtn).toBeDisabled()

    const reasonInput = within(dialog).getByLabelText(/Reason for Denial \(Required\)/i)
    await user.type(reasonInput, 'Postponed until after maintenance window')
    expect(confirmDenyBtn).not.toBeDisabled()

    await user.click(confirmDenyBtn)

    // Card shows denied banner
    await waitFor(() => {
      expect(screen.getByTestId('approval-denied-banner')).toBeInTheDocument()
    })
  })

  it('permanently disables approve button for actions blocked by guardrail policy (Brief §10 & §12)', async () => {
    renderApprovalsPage(adminUser)

    await waitFor(() => {
      expect(screen.queryByTestId('approvals-skeleton')).not.toBeInTheDocument()
    })

    // appr-8905 has verdict 'blocked_by_guardrail'
    const blockedCard = screen.getByTestId('approval-card-appr-8905')
    expect(blockedCard).toHaveTextContent(/GUARDRAIL BLOCKED/i)
    expect(within(blockedCard).getByTestId('guardrail-blocked-verdict')).toBeInTheDocument()

    const approveBtn = within(blockedCard).getByTestId('approve-button')
    // Safety requirement: Cannot be approved in UI
    expect(approveBtn).toBeDisabled()
  })

  it('performs optimistic execution and rolls back with InlineAlert on API failure (Brief §7.6)', async () => {
    const user = userEvent.setup()
    renderApprovalsPage(adminUser)

    await waitFor(() => {
      expect(screen.queryByTestId('approvals-skeleton')).not.toBeInTheDocument()
    })

    // Toggle simulation to force failure
    const failureToggle = screen.getByTestId('simulate-failure-toggle')
    await user.click(failureToggle)
    expect(failureToggle).toHaveTextContent(/Rollback Sim: ON/i)

    // Attempt to approve appr-8903 (medium risk, no typed confirmation)
    const card = screen.getByTestId('approval-card-appr-8903')
    const approveBtn = within(card).getByTestId('approve-button')
    await user.click(approveBtn)

    const dialog = screen.getByRole('dialog')
    const confirmBtn = within(dialog).getByRole('button', { name: /Execute Command/i })
    await user.click(confirmBtn)

    // Expect rollback: InlineAlert appears explaining the failure
    await waitFor(() => {
      expect(screen.getByText(/Optimistic Action Failed/i)).toBeInTheDocument()
      expect(
        screen.getByText(/Simulated network failure: Unable to reach approval gateway/i),
      ).toBeInTheDocument()
    })

    // Verify card is still in pending state (buttons still present)
    expect(within(card).getByTestId('approve-button')).toBeInTheDocument()
  })

  it('updates queue when receiving SignalR ApprovalRequired event (Brief §7.6 & §10)', async () => {
    renderApprovalsPage(adminUser)

    await waitFor(() => {
      expect(screen.queryByTestId('approvals-skeleton')).not.toBeInTheDocument()
    })

    // Emit live SignalR event
    mockSignalREmitter.emit('ApprovalRequired', {
      incidentId: 'INC-2024-9999',
      eventId: 'evt-live-01',
      actionDescription: 'Drain and recycle frontend gateway node pool',
      toolCall: 'drain_node_pool',
      timeoutMinutes: 15,
      timestamp: new Date().toISOString(),
    })

    // Queue query is invalidated and refetched
    await waitFor(() => {
      expect(screen.getByTestId('pending-counter-badge')).toBeInTheDocument()
    })
  })
})
