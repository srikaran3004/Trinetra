import type { Meta, StoryObj } from '@storybook/react'
import { RadioGroup, Radio } from './radio-group'

const meta: Meta<typeof RadioGroup> = {
  title: 'Inputs/RadioGroup',
  component: RadioGroup,
  tags: ['autodocs'],
  args: {
    label: 'Incident State Filter',
    description: 'Filter active incident feed by investigation status',
    defaultValue: 'active',
  },
}

export default meta
type Story = StoryObj<typeof RadioGroup>

export const Vertical: Story = {
  render: (args) => (
    <RadioGroup {...args}>
      <Radio value="active" description="Investigating or awaiting approval">
        Active incidents
      </Radio>
      <Radio value="resolved" description="Auto-remediation completed">
        Resolved incidents
      </Radio>
      <Radio value="escalated" description="Requires manual SRE escalation">
        Escalated incidents
      </Radio>
    </RadioGroup>
  ),
}

export const Horizontal: Story = {
  render: () => (
    <RadioGroup label="Theme Mode" orientation="horizontal" defaultValue="system">
      <Radio value="system">System</Radio>
      <Radio value="dark">Dark</Radio>
      <Radio value="light">Light</Radio>
    </RadioGroup>
  ),
}

export const WithError: Story = {
  render: () => (
    <RadioGroup label="Approval Decision" isInvalid errorMessage="A decision selection is required">
      <Radio value="approve">Approve action</Radio>
      <Radio value="deny">Deny action</Radio>
    </RadioGroup>
  ),
}
