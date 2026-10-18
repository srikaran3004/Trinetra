import type { Meta, StoryObj } from '@storybook/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ApprovalsPage } from './approvals-page'
import { AuthProvider, type User } from '@/auth/auth-context'
import { RealtimeProvider } from '@/realtime/realtime-context'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
    },
  },
})

const meta: Meta<typeof ApprovalsPage> = {
  title: 'Pages/ApprovalsPage',
  component: ApprovalsPage,
  tags: ['autodocs'],
  parameters: {
    layout: 'fullscreen',
  },
}

export default meta
type Story = StoryObj<typeof ApprovalsPage>

const mockAdminUser: User = {
  id: 'usr-admin-01',
  name: 'SRE Lead (Admin)',
  email: 'admin@trinetra.internal',
  role: 'Admin',
}

const mockViewerUser: User = {
  id: 'usr-viewer-01',
  name: 'Security Auditor (Viewer)',
  email: 'viewer@trinetra.internal',
  role: 'Viewer',
}

export const DefaultAdminView: Story = {
  render: () => (
    <QueryClientProvider client={queryClient}>
      <AuthProvider initialUser={mockAdminUser}>
        <RealtimeProvider>
          <div className="min-h-screen bg-[var(--color-canvas)]">
            <ApprovalsPage />
          </div>
        </RealtimeProvider>
      </AuthProvider>
    </QueryClientProvider>
  ),
}

export const ViewerReadOnlyView: Story = {
  render: () => (
    <QueryClientProvider client={queryClient}>
      <AuthProvider initialUser={mockViewerUser}>
        <RealtimeProvider>
          <div className="min-h-screen bg-[var(--color-canvas)]">
            <ApprovalsPage />
          </div>
        </RealtimeProvider>
      </AuthProvider>
    </QueryClientProvider>
  ),
}
