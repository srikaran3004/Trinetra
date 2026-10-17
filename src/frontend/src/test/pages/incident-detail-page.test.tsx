import { describe, it, expect, beforeEach } from 'vitest'
import { render, screen, waitFor, fireEvent, act, within } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { IncidentDetailPage } from '@/pages/incident-detail-page'
import { AuthProvider } from '@/auth/auth-context'
import { RealtimeProvider } from '@/realtime/realtime-context'
import { mockSignalREmitter } from '@/realtime/mock-signalr-emitter'
import { resetMockDatabase } from '@/mocks/handlers'

function renderIncidentDetailPage(incidentId = 'INC-2024-8901') {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        staleTime: 0,
      },
    },
  })

  return {
    queryClient,
    ...render(
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <RealtimeProvider>
            <MemoryRouter initialEntries={[`/incidents/${incidentId}`]}>
              <Routes>
                <Route path="/incidents/:id" element={<IncidentDetailPage />} />
                <Route
                  path="/incidents"
                  element={<div data-testid="incidents-list-target">Incidents List</div>}
                />
                <Route
                  path="/approvals"
                  element={<div data-testid="approvals-queue-target">Approvals Queue</div>}
                />
              </Routes>
            </MemoryRouter>
          </RealtimeProvider>
        </AuthProvider>
      </QueryClientProvider>,
    ),
  }
}

describe('Incident Detail Page (Task F3-2)', () => {
  beforeEach(() => {
    resetMockDatabase()
    mockSignalREmitter.reset()
  })

  it('renders critical incident awaiting approval with StateProgress, root cause, and remediation options', async () => {
    renderIncidentDetailPage('INC-2024-8901')

    // Header title and badges
    await waitFor(() => {
      expect(
        screen.getByRole('heading', {
          level: 2,
          name: /database connection pool exhaustion in auth-api/i,
        }),
      ).toBeInTheDocument()
    })

    expect(screen.getByText('INC-2024-8901')).toBeInTheDocument()
    expect(screen.getByText('CRITICAL')).toBeInTheDocument()
    expect(screen.getAllByText('Awaiting Approval').length).toBeGreaterThan(0)

    // StateProgress indicates awaiting_approval step
    const progressRegion = screen.getByRole('region', { name: /incident state progress/i })
    expect(progressRegion).toBeInTheDocument()
    const currentStep = screen.getByRole('listitem', { current: 'step' })
    expect(currentStep).toHaveTextContent(/awaiting approval/i)

    // Root Cause Analysis Card with 0.80 threshold
    expect(screen.getByText(/primary diagnostic verdict/i)).toBeInTheDocument()
    expect(screen.getAllByText(/42 leaked idle transactions/i).length).toBeGreaterThan(0)
    expect(screen.getByText('92%')).toBeInTheDocument()
    expect(screen.getByText(/threshold: 80%/i)).toBeInTheDocument()

    // Proposed Remediation Plans with Backend Verdicts
    expect(screen.getByText(/proposed remediation plans/i)).toBeInTheDocument()
    expect(screen.getByText(/terminate idle in transaction connections/i)).toBeInTheDocument()
    expect(screen.getByText(/requires human approval/i)).toBeInTheDocument()

    // Blocked remediation option displayed verbatim (§7.4/§10)
    expect(
      screen.getByText(/restart all auth-api pods simultaneously \(blocked\)/i),
    ).toBeInTheDocument()
    expect(screen.getByText(/blocked by guardrail/i)).toBeInTheDocument()

    // Review CTA navigates to approval queue
    const reviewBtn = screen.getByRole('button', { name: /review approval gate/i })
    fireEvent.click(reviewBtn)
    await waitFor(() => {
      expect(screen.getByTestId('approvals-queue-target')).toBeInTheDocument()
    })
  })

  it('renders escalated incident with StateProgress escalated branch', async () => {
    renderIncidentDetailPage('INC-2024-8902')

    await waitFor(() => {
      expect(
        screen.getByRole('heading', {
          level: 2,
          name: /upstream payment gateway timeout surge/i,
        }),
      ).toBeInTheDocument()
    })

    // Escalated banner alert is visible
    expect(screen.getByRole('alert')).toHaveTextContent(/escalated to human on-call/i)
    expect(screen.getByText(/escalated branch/i)).toBeInTheDocument()
  })

  it('renders guardrail-triggered timeline entry with distinct visual styling and explicit text label (§7.4 & §10)', async () => {
    renderIncidentDetailPage('INC-2024-8899')

    await waitFor(() => {
      expect(
        screen.getByRole('heading', {
          level: 2,
          name: /memory leak and unhandled exception in order-processor worker/i,
        }),
      ).toBeInTheDocument()
    })

    // Verify distinct guardrail text label within guardrail event card
    const guardrailItem = screen.getByTestId('timeline-event-evt-8899-04')
    expect(within(guardrailItem).getByText('GUARDRAIL TRIGGERED')).toBeInTheDocument()

    // Action description of the guardrail interception
    expect(
      within(guardrailItem).getByText(
        /mandatory guardrail interception: blocked destructive command pattern/i,
      ),
    ).toBeInTheDocument()

    // Expand payload to inspect non-bypassable policy interception log
    const inspectBtn = within(guardrailItem).getByRole('button', { name: /inspect payload/i })
    fireEvent.click(inspectBtn)

    await waitFor(() => {
      expect(
        within(guardrailItem).getByText(/BLOCKED by Non-Bypassable Policy RG-01 & RG-04/i),
      ).toBeInTheDocument()
      expect(
        within(guardrailItem).getByText(/rm -rf \/var\/log\/orders\/\* && kill -9 1/i),
      ).toBeInTheDocument()
    })
  })

  it('updates state dynamically when receiving SignalR IncidentStatusChanged event', async () => {
    renderIncidentDetailPage('INC-2024-8901')

    await waitFor(() => {
      expect(screen.getByText('INC-2024-8901')).toBeInTheDocument()
    })

    expect(screen.getAllByText('Awaiting Approval').length).toBeGreaterThan(0)

    // Dispatch realtime status change event
    act(() => {
      mockSignalREmitter.emit('IncidentStatusChanged', {
        incidentId: 'INC-2024-8901',
        status: 'executing_remediation',
        updatedAt: new Date().toISOString(),
      })
    })

    await waitFor(() => {
      expect(screen.getAllByText('Executing').length).toBeGreaterThan(0)
    })

    // StateProgress step updated
    const currentStep = screen.getByRole('listitem', { current: 'step' })
    expect(currentStep).toHaveTextContent(/executing remediation/i)
  })

  it('renders diagnostic snapshots with telemetry, Geist Mono log excerpt, and linked runbooks', async () => {
    renderIncidentDetailPage('INC-2024-8901')

    await waitFor(() => {
      expect(screen.getByText('INC-2024-8901')).toBeInTheDocument()
    })

    // Telemetry Snapshot
    expect(screen.getByText(/telemetry snapshot at trigger time/i)).toBeInTheDocument()
    expect(screen.getByText('18.4%')).toBeInTheDocument()
    expect(screen.getByText('4,210ms')).toBeInTheDocument()

    // Correlated log excerpt
    expect(screen.getByText(/correlated log excerpt \(geist mono\)/i)).toBeInTheDocument()
    expect(screen.getByText(/ConnectionPoolTimeoutException/i)).toBeInTheDocument()

    // Linked Runbooks with pgvector similarity
    expect(screen.getByText(/linked runbooks \(pgvector vector search\)/i)).toBeInTheDocument()
    expect(screen.getByText(/94% match/i)).toBeInTheDocument()
  })
})
