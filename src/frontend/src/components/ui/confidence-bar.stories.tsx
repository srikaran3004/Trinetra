import type { Meta, StoryObj } from '@storybook/react'
import { ConfidenceBar } from './confidence-bar'

const meta: Meta<typeof ConfidenceBar> = {
  title: 'Data/ConfidenceBar',
  component: ConfidenceBar,
  tags: ['autodocs'],
}

export default meta
type Story = StoryObj<typeof ConfidenceBar>

export const HighConfidence: Story = {
  args: {
    value: 0.92,
    threshold: 0.8,
  },
}

export const ExactlyThreshold: Story = {
  args: {
    value: 0.8,
    threshold: 0.8,
  },
}

export const ModerateConfidence: Story = {
  args: {
    value: 0.65,
    threshold: 0.8,
  },
}

export const LowConfidence: Story = {
  args: {
    value: 0.35,
    threshold: 0.8,
  },
}
