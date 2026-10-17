import type { Meta, StoryObj } from '@storybook/react'
import { MemoryRouter, Routes, Route } from 'react-router'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { IncidentDetailPage } from './incident-detail-page'
import { AuthProvider } from '@/auth/auth-context'
import { RealtimeProvider } from '@/realtime/realtime-context'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
    },
  },
})

const meta: Meta<typeof IncidentDetailPage> = {
  title: 'Pages/IncidentDetailPage',
  component: IncidentDetailPage,
  decorators: [
    (Story) => (
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <RealtimeProvider>
            <MemoryRouter initialEntries={['/incidents/INC-2024-8901']}>
              <Routes>
                <Route path="/incidents/:id" element={<Story />} />
              </Routes>
            </MemoryRouter>
          </RealtimeProvider>
        </AuthProvider>
      </QueryClientProvider>
    ),
  ],
}

export default meta
type Story = StoryObj<typeof IncidentDetailPage>

export const CriticalAwaitingApproval: Story = {}

export const EscalatedIncident: Story = {
  decorators: [
    (Story) => (
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <RealtimeProvider>
            <MemoryRouter initialEntries={['/incidents/INC-2024-8902']}>
              <Routes>
                <Route path="/incidents/:id" element={<Story />} />
              </Routes>
            </MemoryRouter>
          </RealtimeProvider>
        </AuthProvider>
      </QueryClientProvider>
    ),
  ],
}

export const ResolvedWithGuardrailInterception: Story = {
  decorators: [
    (Story) => (
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <RealtimeProvider>
            <MemoryRouter initialEntries={['/incidents/INC-2024-8899']}>
              <Routes>
                <Route path="/incidents/:id" element={<Story />} />
              </Routes>
            </MemoryRouter>
          </RealtimeProvider>
        </AuthProvider>
      </QueryClientProvider>
    ),
  ],
}
