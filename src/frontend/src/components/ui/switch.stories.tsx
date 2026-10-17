import type { Meta, StoryObj } from '@storybook/react'
import { Switch } from './switch'

const meta: Meta<typeof Switch> = {
  title: 'Inputs/Switch',
  component: Switch,
  tags: ['autodocs'],
  args: {
    children: 'Reduced motion override',
    description: 'Disable non-essential animations across the console',
  },
}

export default meta
type Story = StoryObj<typeof Switch>

export const Default: Story = {}

export const Checked: Story = {
  args: {
    defaultSelected: true,
    children: 'Live SignalR streaming',
    description: 'Stream incident telemetry and agent events in real time',
  },
}

export const Disabled: Story = {
  args: {
    isDisabled: true,
    defaultSelected: true,
    children: 'Mandatory command interceptor',
    description: 'System-enforced guardrail cannot be toggled off',
  },
}
