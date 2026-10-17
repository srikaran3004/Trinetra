import type { Meta, StoryObj } from '@storybook/react'
import { StatusDot } from './status-dot'

const meta: Meta<typeof StatusDot> = {
  title: 'Display/StatusDot',
  component: StatusDot,
  tags: ['autodocs'],
  args: {
    status: 'success',
    label: 'Online',
    showLabel: true,
  },
}

export default meta
type Story = StoryObj<typeof StatusDot>

export const Default: Story = {}

export const AllStatuses: Story = {
  render: () => (
    <div className="flex flex-col gap-2">
      <StatusDot status="critical" label="Critical Outage" showLabel />
      <StatusDot status="high" label="High Degradation" showLabel />
      <StatusDot status="medium" label="Medium Warning" showLabel />
      <StatusDot status="low" label="Low Priority" showLabel />
      <StatusDot status="success" label="Healthy / Operational" showLabel />
      <StatusDot status="accent" label="Active Agent" showLabel />
      <StatusDot status="neutral" label="Idle / Offline" showLabel />
    </div>
  ),
}

export const Pulsing: Story = {
  render: () => (
    <div className="flex items-center gap-4">
      <StatusDot status="critical" label="Live Critical Incident" isPulsing showLabel />
      <StatusDot status="accent" label="Agent Executing" isPulsing showLabel />
    </div>
  ),
}

export const Sizes: Story = {
  render: () => (
    <div className="flex items-center gap-4">
      <StatusDot size="sm" status="success" label="Small" showLabel />
      <StatusDot size="md" status="success" label="Medium" showLabel />
      <StatusDot size="lg" status="success" label="Large" showLabel />
    </div>
  ),
}
