import type { Meta, StoryObj } from '@storybook/react'
import { Panel } from './panel'
import { Button } from './button'

const meta: Meta<typeof Panel> = {
  title: 'Display/Panel',
  component: Panel,
  tags: ['autodocs'],
  args: {
    title: 'Postflight Verification Log',
    subtitle: 'Container health and TCP port checks',
  },
}

export default meta
type Story = StoryObj<typeof Panel>

export const Default: Story = {
  render: (args) => (
    <Panel
      {...args}
      actions={
        <Button size="sm" variant="ghost">
          Re-run
        </Button>
      }
      className="max-w-xl"
    >
      <div className="font-mono text-xs text-[var(--color-text-secondary)] space-y-1">
        <p className="text-[var(--color-success)]">[PASS] TCP port 5432 responding (1.2ms)</p>
        <p className="text-[var(--color-success)]">
          [PASS] Connection pool active count = 12 / 100
        </p>
        <p className="text-[var(--color-success)]">[PASS] HTTP /health returned 200 OK</p>
      </div>
    </Panel>
  ),
}
