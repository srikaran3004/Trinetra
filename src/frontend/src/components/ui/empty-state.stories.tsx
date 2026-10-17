import type { Meta, StoryObj } from '@storybook/react'
import { EmptyState } from './empty-state'
import { Button } from './button'
import { ShieldCheck } from 'lucide-react'

const meta: Meta<typeof EmptyState> = {
  title: 'Feedback/EmptyState',
  component: EmptyState,
  tags: ['autodocs'],
}

export default meta
type Story = StoryObj<typeof EmptyState>

export const NoIncidents: Story = {
  args: {
    icon: <ShieldCheck size={24} strokeWidth={1.5} className="text-[var(--color-success)]" />,
    title: 'No active incidents',
    description:
      'All 8 autonomous monitoring agents report healthy systems across production clusters.',
    action: (
      <Button size="sm" variant="secondary">
        View Incident History
      </Button>
    ),
  },
}

export const NoApprovals: Story = {
  args: {
    title: 'No pending approvals',
    description: 'No remediation actions currently require human approval gates.',
  },
}
