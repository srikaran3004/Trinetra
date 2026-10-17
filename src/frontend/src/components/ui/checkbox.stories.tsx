import type { Meta, StoryObj } from '@storybook/react'
import { Checkbox, CheckboxGroup } from './checkbox'

const meta: Meta<typeof Checkbox> = {
  title: 'Inputs/Checkbox',
  component: Checkbox,
  tags: ['autodocs'],
  args: {
    children: 'Auto-rollback on failure',
    description: 'Trigger rollback script if postflight check fails',
  },
}

export default meta
type Story = StoryObj<typeof Checkbox>

export const Default: Story = {}

export const Checked: Story = {
  args: {
    defaultSelected: true,
    children: 'Pre-flight checks enabled',
  },
}

export const Indeterminate: Story = {
  args: {
    isIndeterminate: true,
    children: 'Select all affected nodes (3 of 8 active)',
  },
}

export const Disabled: Story = {
  args: {
    isDisabled: true,
    defaultSelected: true,
    children: 'Immutable security guardrail enforcement',
    description: 'This guardrail cannot be disabled in production',
  },
}

export const Group: Story = {
  render: () => (
    <CheckboxGroup label="Agent Investigation Sources" description="Select which collectors to run">
      <Checkbox defaultSelected value="logs">
        Elasticsearch Logs
      </Checkbox>
      <Checkbox defaultSelected value="metrics">
        Prometheus Metrics
      </Checkbox>
      <Checkbox value="runbooks">RAG Runbooks</Checkbox>
    </CheckboxGroup>
  ),
}
