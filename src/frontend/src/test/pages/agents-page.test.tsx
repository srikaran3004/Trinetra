import { describe, it, expect, beforeEach } from 'vitest'
import { render, screen, waitFor, fireEvent, act } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AgentsPage } from '@/pages/agents-page'
import { AuthProvider } from '@/auth/auth-context'
import { RealtimeProvider } from '@/realtime/realtime-context'
import { mockSignalREmitter } from '@/realtime/mock-signalr-emitter'
import { resetMockDatabase } from '@/mocks/handlers'

function renderAgentsPage(initialRoute = '/agents') {
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
            <MemoryRouter initialEntries={[initialRoute]}>
              <Routes>
                <Route path="/agents" element={<AgentsPage />} />
                <Route
                  path="/incidents/:id"
                  element={<div data-testid="incident-target">Incident Detail</div>}
                />
              </Routes>
            </MemoryRouter>
          </RealtimeProvider>
        </AuthProvider>
      </QueryClientProvider>,
    ),
  }
}

describe('Agent Activity Monitor Page (Task F4-1)', () => {
  beforeEach(() => {
    resetMockDatabase()
    mockSignalREmitter.reset()
  })

  it('renders agent activity monitor with header, live badge, and default in-progress agents (§7.5)', async () => {
    renderAgentsPage()

    // Page heading and description
    expect(
      screen.getByRole('heading', { level: 2, name: /autonomous agent fleet/i }),
    ).toBeInTheDocument()
    expect(screen.getByText(/SIGNALR LIVE/i)).toBeInTheDocument()

    // By default, filters to in-progress agents (Incident Coordinator, Remediation Proposer, Audit Logger)
    await waitFor(() => {
      expect(screen.getByText('Incident Coordinator')).toBeInTheDocument()
      expect(screen.getByText('Remediation Proposer')).toBeInTheDocument()
      expect(screen.getByText('Audit Logger')).toBeInTheDocument()
    })

    // Indicator explains in-progress filter
    expect(screen.getByText(/showing in-progress incident agents/i)).toBeInTheDocument()

    // Idle agent 'Log Analyzer' should NOT be visible under in-progress filter
    expect(screen.queryByText('Log Analyzer')).not.toBeInTheDocument()
  })

  it('switches filter to view all 8 fleet agents including idle agents', async () => {
    renderAgentsPage()

    await waitFor(() => {
      expect(screen.getByText('Incident Coordinator')).toBeInTheDocument()
    })

    // Change status filter to 'all'
    const statusSelect = screen.getByLabelText(/filter agents by status/i)
    fireEvent.change(statusSelect, { target: { value: 'all' } })

    // All 8 seeded agents appear
    await waitFor(() => {
      expect(screen.getByText('Incident Coordinator')).toBeInTheDocument()
      expect(screen.getByText('Log Analyzer')).toBeInTheDocument()
      expect(screen.getByText('Metrics Querier')).toBeInTheDocument()
      expect(screen.getByText('Runbook Retriever')).toBeInTheDocument()
      expect(screen.getByText('Root Cause Analyzer')).toBeInTheDocument()
      expect(screen.getByText('Remediation Proposer')).toBeInTheDocument()
      expect(screen.getByText('Auto-Executor')).toBeInTheDocument()
      expect(screen.getByText('Audit Logger')).toBeInTheDocument()
    })

    expect(screen.getByText(/showing 8 of 8 agents/i)).toBeInTheDocument()
  })

  it('renders current tool call and elapsed time', async () => {
    renderAgentsPage()

    await waitFor(() => {
      expect(screen.getByText('Incident Coordinator')).toBeInTheDocument()
    })

    // Monospace tool call
    expect(screen.getByText('orchestrate_parallel_investigation')).toBeInTheDocument()

    // Elapsed time
    expect(screen.getByText(/elapsed: 2m 25s/i)).toBeInTheDocument()
  })

  it('renders guardrail violations count with distinct warning styling when > 0 (§7.5 & §10)', async () => {
    renderAgentsPage('/agents?status=all')

    await waitFor(() => {
      expect(screen.getByText('Auto-Executor')).toBeInTheDocument()
    })

    // Auto-Executor has 1 guardrail violation from INC-2024-8899
    const violationBadge = screen.getByLabelText(/1 guardrail violation intercepted/i)
    expect(violationBadge).toBeInTheDocument()
    expect(violationBadge).toHaveTextContent(/1 guardrail intercepted/i)
  })

  it('handles realtime AgentActionCompleted SignalR event', async () => {
    renderAgentsPage('/agents?status=all')

    await waitFor(() => {
      expect(screen.getByText('Log Analyzer')).toBeInTheDocument()
    })

    // Initially Log Analyzer is idle with no tool call
    expect(screen.getAllByText(/awaiting next autonomous dispatch/i).length).toBeGreaterThan(0)

    // Emit realtime AgentActionCompleted SignalR event
    act(() => {
      mockSignalREmitter.emit('AgentActionCompleted', {
        incidentId: 'INC-2024-8901',
        agentName: 'Log Analyzer',
        actionDescription: 'Scanned 14,000 log records in auth-api',
        toolCall: 'query_logs(service="auth-api", limit=1000)',
        status: 'working',
        timestamp: new Date().toISOString(),
      })
    })

    // Tool call and working status updated in real time
    await waitFor(() => {
      expect(screen.getByText('query_logs(service="auth-api", limit=1000)')).toBeInTheDocument()
    })
  })

  it('filters agents by search keyword across name and tool call', async () => {
    renderAgentsPage('/agents?status=all')

    await waitFor(() => {
      expect(screen.getByText('Incident Coordinator')).toBeInTheDocument()
    })

    // Search for 'runbook'
    const searchInput = screen.getByLabelText(/search agents/i)
    fireEvent.change(searchInput, { target: { value: 'runbook' } })

    // Wait for debounced search
    await waitFor(() => {
      expect(screen.getByText('Runbook Retriever')).toBeInTheDocument()
      expect(screen.queryByText('Incident Coordinator')).not.toBeInTheDocument()
    })
  })
})
