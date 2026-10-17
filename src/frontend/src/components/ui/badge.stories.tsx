import type { Meta, StoryObj } from '@storybook/react'
import { Badge } from './badge'

const meta: Meta<typeof Badge> = {
  title: 'Display/Badge',
  component: Badge,
  tags: ['autodocs'],
  args: {
    children: 'Badge',
  },
}

export default meta
type Story = StoryObj<typeof Badge>

export const Severities: Story = {
  render: () => (
    <div className="flex flex-wrap gap-2 items-center">
      <Badge severity="critical" />
      <Badge severity="high" />
      <Badge severity="medium" />
      <Badge severity="low" />
    </div>
  ),
}

export const IncidentStates: Story = {
  render: () => (
    <div className="flex flex-wrap gap-2 items-center">
      <Badge incidentState="triggered" />
      <Badge incidentState="investigating" />
      <Badge incidentState="analysis_complete" />
      <Badge incidentState="awaiting_approval" />
      <Badge incidentState="executing_remediation" />
      <Badge incidentState="monitoring_recovery" />
      <Badge incidentState="resolved" />
      <Badge incidentState="escalated" />
    </div>
  ),
}

export const Environments: Story = {
  render: () => (
    <div className="flex flex-wrap gap-2 items-center">
      <Badge env="prod" />
      <Badge env="staging" />
      <Badge env="dev" />
    </div>
  ),
}

export const Sizes: Story = {
  render: () => (
    <div className="flex items-center gap-3">
      <Badge size="sm" severity="critical">
        Small (20px)
      </Badge>
      <Badge size="md" severity="critical">
        Medium (24px)
      </Badge>
    </div>
  ),
}
