import { describe, it, expect, beforeEach } from 'vitest'
import { render, screen, waitFor, fireEvent, act } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { IncidentsPage } from '@/pages/incidents-page'
import { AuthProvider } from '@/auth/auth-context'
import { RealtimeProvider } from '@/realtime/realtime-context'
import { mockSignalREmitter } from '@/realtime/mock-signalr-emitter'
import { resetMockDatabase } from '@/mocks/handlers'

function renderIncidentsPage(initialRoute = '/incidents') {
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
              <div id="incident-live-announcer" aria-live="polite" className="sr-only" />
              <Routes>
                <Route path="/incidents" element={<IncidentsPage />} />
                <Route
                  path="/incidents/:id"
                  element={<div data-testid="incident-detail-target">Incident Detail View</div>}
                />
              </Routes>
            </MemoryRouter>
          </RealtimeProvider>
        </AuthProvider>
      </QueryClientProvider>,
    ),
  }
}

describe('Active Incidents List Page (Task F3-1)', () => {
  beforeEach(() => {
    resetMockDatabase()
    mockSignalREmitter.reset()
  })

  it('renders active incidents page with header, summary counters, and seeded table data', async () => {
    renderIncidentsPage()

    // Section header title (h2)
    expect(screen.getByRole('heading', { level: 2, name: /active incidents/i })).toBeInTheDocument()

    // Wait for MSW query response to render seeded incidents
    await waitFor(() => {
      expect(
        screen.getAllByText(/database connection pool exhaustion in auth-api/i).length,
      ).toBeGreaterThan(0)
    })

    // Seeded incidents rendered
    expect(screen.getAllByText('INC-2024-8901').length).toBeGreaterThan(0)
    expect(screen.getAllByText('INC-2024-8902').length).toBeGreaterThan(0)
    expect(screen.getAllByText('INC-2024-8899').length).toBeGreaterThan(0)

    // Affected services rendered
    expect(screen.getAllByText('auth-api').length).toBeGreaterThan(0)
    expect(screen.getAllByText('payment-gateway').length).toBeGreaterThan(0)

    // Summary counts rendered
    expect(screen.getByText(/1 Critical/i)).toBeInTheDocument()
    expect(screen.getByText(/2 Active Total/i)).toBeInTheDocument()
  })

  it('filters incidents by severity using select dropdown', async () => {
    renderIncidentsPage()

    await waitFor(() => {
      expect(screen.getAllByText('INC-2024-8901').length).toBeGreaterThan(0)
    })

    const severitySelect = screen.getByLabelText(/filter by severity/i)
    fireEvent.change(severitySelect, { target: { value: 'critical' } })

    await waitFor(() => {
      // Critical incident still visible
      expect(screen.getAllByText('INC-2024-8901').length).toBeGreaterThan(0)
      // High incident filtered out
      expect(screen.queryAllByText('INC-2024-8902').length).toBe(0)
      // Medium incident filtered out
      expect(screen.queryAllByText('INC-2024-8899').length).toBe(0)
    })
  })

  it('filters incidents by status using select dropdown', async () => {
    renderIncidentsPage()

    await waitFor(() => {
      expect(screen.getAllByText('INC-2024-8901').length).toBeGreaterThan(0)
    })

    const statusSelect = screen.getByLabelText(/filter by status/i)
    fireEvent.change(statusSelect, { target: { value: 'escalated' } })

    await waitFor(() => {
      // Escalated incident INC-2024-8902 should be present
      expect(screen.getAllByText('INC-2024-8902').length).toBeGreaterThan(0)
      // Awaiting approval incident INC-2024-8901 filtered out
      expect(screen.queryAllByText('INC-2024-8901').length).toBe(0)
    })
  })

  it('filters incidents by text search across title and ID', async () => {
    renderIncidentsPage()

    await waitFor(() => {
      expect(screen.getAllByText('INC-2024-8901').length).toBeGreaterThan(0)
    })

    const searchInput = screen.getByLabelText(/search incidents/i)
    fireEvent.change(searchInput, { target: { value: 'gateway' } })

    await waitFor(() => {
      // Payment gateway incident matches
      expect(screen.getAllByText('INC-2024-8902').length).toBeGreaterThan(0)
      // Other incidents filtered out
      expect(screen.queryAllByText('INC-2024-8901').length).toBe(0)
    })
  })

  it('clears all filters when clicking Clear button', async () => {
    renderIncidentsPage('/incidents?severity=critical')

    await waitFor(() => {
      expect(screen.getAllByText('INC-2024-8901').length).toBeGreaterThan(0)
    })

    expect(screen.queryAllByText('INC-2024-8902').length).toBe(0)

    const clearBtn = screen.getByRole('button', { name: /clear/i })
    fireEvent.click(clearBtn)

    await waitFor(() => {
      // Both incidents now visible
      expect(screen.getAllByText('INC-2024-8901').length).toBeGreaterThan(0)
      expect(screen.getAllByText('INC-2024-8902').length).toBeGreaterThan(0)
    })
  })

  it('shows EmptyState when no incidents match query', async () => {
    renderIncidentsPage()

    await waitFor(() => {
      expect(screen.getAllByText('INC-2024-8901').length).toBeGreaterThan(0)
    })

    const searchInput = screen.getByLabelText(/search incidents/i)
    fireEvent.change(searchInput, { target: { value: 'non-existent-service-12345' } })

    await waitFor(() => {
      expect(screen.getByText(/no incidents found/i)).toBeInTheDocument()
    })
  })

  it('supports roving keyboard navigation with j, k, and Enter (§7.3)', async () => {
    renderIncidentsPage()

    await waitFor(() => {
      expect(screen.getAllByText('INC-2024-8901').length).toBeGreaterThan(0)
    })

    const tableRows = screen.getAllByRole('row').filter((r) => r.getAttribute('tabindex') === '0')
    expect(tableRows.length).toBeGreaterThan(0)

    // First row is selected initially (index 0)
    expect(tableRows[0]).toHaveAttribute('aria-selected', 'true')

    // Press 'j' to select second row
    fireEvent.keyDown(window, { key: 'j' })
    expect(tableRows[1]).toHaveAttribute('aria-selected', 'true')

    // Press 'k' to move back to first row
    fireEvent.keyDown(window, { key: 'k' })
    expect(tableRows[0]).toHaveAttribute('aria-selected', 'true')

    // Press 'Enter' to open detail view
    fireEvent.keyDown(window, { key: 'Enter' })
    await waitFor(() => {
      expect(screen.getByTestId('incident-detail-target')).toBeInTheDocument()
    })
  })

  it('handles realtime IncidentCreated SignalR event with 1.2s highlight animation (§8)', async () => {
    renderIncidentsPage()

    await waitFor(() => {
      expect(screen.getAllByText('INC-2024-8901').length).toBeGreaterThan(0)
    })

    // Dispatch realtime SignalR event inside act
    act(() => {
      mockSignalREmitter.emit('IncidentCreated', {
        id: 'INC-2024-9999',
        incidentId: 'INC-2024-9999',
        title: 'Kafka broker partition leader lost',
        severity: 'critical',
        status: 'triggered',
        triggeredAt: new Date().toISOString(),
      })
    })

    // Incident arrives at the top of the list
    await waitFor(() => {
      expect(screen.getAllByText('INC-2024-9999').length).toBeGreaterThan(0)
      expect(screen.getAllByText(/kafka broker partition leader lost/i).length).toBeGreaterThan(0)
    })

    // Highlight class applied per Brief §8 (1.2s background tint)
    const newRow = screen.getAllByRole('row').find((r) => r.textContent.includes('INC-2024-9999'))
    expect(newRow).toHaveClass('highlight-incident')

    // Accessibility live region announced arrival
    const liveAnnouncer = document.getElementById('incident-live-announcer')
    expect(liveAnnouncer?.textContent).toContain('INC-2024-9999')
  })

  it('renders both table layout for desktop and cards layout for mobile (§7.3)', async () => {
    renderIncidentsPage()

    await waitFor(() => {
      expect(screen.getAllByText('INC-2024-8901').length).toBeGreaterThan(0)
    })

    // Desktop table exists
    const table = screen.getByRole('table')
    expect(table).toBeInTheDocument()

    // Multiple elements for the same incident exist across desktop table & mobile cards
    const idBadges = screen.getAllByText('INC-2024-8901')
    expect(idBadges.length).toBeGreaterThanOrEqual(2)
  })
})
