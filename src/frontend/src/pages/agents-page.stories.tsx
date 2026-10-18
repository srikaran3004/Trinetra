import type { Meta, StoryObj } from '@storybook/react'
import { MemoryRouter } from 'react-router'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { RealtimeProvider } from '@/realtime/realtime-context'
import { AgentsPage } from './agents-page'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
    },
  },
})

const meta = {
  title: 'Pages/AgentsPage',
  component: AgentsPage,
  decorators: [
    (Story) => (
      <QueryClientProvider client={queryClient}>
        <RealtimeProvider>
          <MemoryRouter initialEntries={['/agents']}>
            <Story />
          </MemoryRouter>
        </RealtimeProvider>
      </QueryClientProvider>
    ),
  ],
  tags: ['autodocs'],
} satisfies Meta<typeof AgentsPage>

export default meta
type Story = StoryObj<typeof meta>

export const DefaultInProgress: Story = {}

export const AllFleetAgents: Story = {
  decorators: [
    (Story) => (
      <QueryClientProvider client={queryClient}>
        <RealtimeProvider>
          <MemoryRouter initialEntries={['/agents?status=all']}>
            <Story />
          </MemoryRouter>
        </RealtimeProvider>
      </QueryClientProvider>
    ),
  ],
}
