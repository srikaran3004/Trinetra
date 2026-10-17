import type { Meta, StoryObj } from '@storybook/react'
import { Popover } from './popover'
import { Button } from './button'

const meta: Meta<typeof Popover> = {
  title: 'Overlays/Popover',
  component: Popover,
  tags: ['autodocs'],
  args: {
    title: 'Incident Telemetry',
  },
}

export default meta
type Story = StoryObj<typeof Popover>

export const Default: Story = {
  render: (args) => (
    <Popover
      {...args}
      trigger={
        <Button size="sm" variant="secondary">
          View Cluster Info
        </Button>
      }
    >
      <div className="flex flex-col gap-1.5 text-xs text-[var(--color-text-secondary)]">
        <p>
          <strong>Cluster:</strong> us-east-prod-k8s
        </p>
        <p>
          <strong>Region:</strong> AWS us-east-1
        </p>
        <p>
          <strong>Node Count:</strong> 24 active nodes
        </p>
      </div>
    </Popover>
  ),
}
