import type { Meta, StoryObj } from '@storybook/react'
import { MemoryRouter } from 'react-router'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { IncidentsPage } from './incidents-page'
import { AuthProvider } from '@/auth/auth-context'
import { RealtimeProvider } from '@/realtime/realtime-context'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
    },
  },
})

const meta: Meta<typeof IncidentsPage> = {
  title: 'Pages/IncidentsPage',
  component: IncidentsPage,
  decorators: [
    (Story) => (
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <RealtimeProvider>
            <MemoryRouter initialEntries={['/incidents']}>
              <Story />
            </MemoryRouter>
          </RealtimeProvider>
        </AuthProvider>
      </QueryClientProvider>
    ),
  ],
}

export default meta
type Story = StoryObj<typeof IncidentsPage>

export const Default: Story = {}

export const FilteredByCritical: Story = {
  decorators: [
    (Story) => (
      <QueryClientProvider client={queryClient}>
        <AuthProvider>
          <RealtimeProvider>
            <MemoryRouter initialEntries={['/incidents?severity=critical']}>
              <Story />
            </MemoryRouter>
          </RealtimeProvider>
        </AuthProvider>
      </QueryClientProvider>
    ),
  ],
}
