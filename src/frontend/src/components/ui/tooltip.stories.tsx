import type { Meta, StoryObj } from '@storybook/react'
import { Tooltip } from './tooltip'
import { Button } from './button'

const meta: Meta<typeof Tooltip> = {
  title: 'Overlays/Tooltip',
  component: Tooltip,
  tags: ['autodocs'],
  args: {
    content: 'Paging SRE on-call team',
  },
}

export default meta
type Story = StoryObj<typeof Tooltip>

export const Default: Story = {
  render: (args) => (
    <Tooltip {...args}>
      <Button size="sm">Escalate Incident</Button>
    </Tooltip>
  ),
}

export const RichContent: Story = {
  render: () => (
    <Tooltip
      content={
        <div className="flex flex-col gap-0.5">
          <span className="font-semibold text-white">PostgreSQL Connection Pool</span>
          <span className="text-[11px] text-[var(--color-text-tertiary)]">
            84 active / 100 max connections
          </span>
        </div>
      }
    >
      <Button variant="secondary" size="sm">
        Pool Metrics
      </Button>
    </Tooltip>
  ),
}
