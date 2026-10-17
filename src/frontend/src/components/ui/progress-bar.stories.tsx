import type { Meta, StoryObj } from '@storybook/react'
import { ProgressBar } from './progress-bar'

const meta: Meta<typeof ProgressBar> = {
  title: 'Feedback/ProgressBar',
  component: ProgressBar,
  tags: ['autodocs'],
  args: {
    label: 'Investigation Analysis Progress',
    value: 65,
    showValue: true,
  },
}

export default meta
type Story = StoryObj<typeof ProgressBar>

export const Default: Story = {}

export const Variants: Story = {
  render: () => (
    <div className="flex flex-col gap-4 max-w-md">
      <ProgressBar label="Accent" variant="accent" value={75} showValue />
      <ProgressBar label="Healthy / Success" variant="success" value={100} showValue />
      <ProgressBar label="High Degradation" variant="high" value={60} showValue />
      <ProgressBar label="Critical Saturation" variant="critical" value={95} showValue />
    </div>
  ),
}

export const Indeterminate: Story = {
  args: {
    label: 'Fanning out parallel agent investigation...',
    isIndeterminate: true,
  },
}
