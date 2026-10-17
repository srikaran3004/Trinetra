import type { Meta, StoryObj } from '@storybook/react'
import { Select } from './select'

const sampleOptions = [
  { id: 'critical', label: 'Critical (P1)', description: 'Service outage or data corruption' },
  { id: 'high', label: 'High (P2)', description: 'Significant degradation with no workaround' },
  { id: 'medium', label: 'Medium (P3)', description: 'Partial loss of non-critical functionality' },
  { id: 'low', label: 'Low (P4)', description: 'Minor cosmetic or low impact issue' },
]

const meta: Meta<typeof Select> = {
  title: 'Inputs/Select',
  component: Select,
  tags: ['autodocs'],
  args: {
    label: 'Incident Severity',
    options: sampleOptions,
    placeholder: 'Select severity level...',
  },
}

export default meta
type Story = StoryObj<typeof Select>

export const Default: Story = {}

export const WithDescription: Story = {
  args: {
    label: 'Incident Severity',
    description: 'Determines escalation thresholds and on-call paging',
    options: sampleOptions,
  },
}

export const WithError: Story = {
  args: {
    label: 'Incident Severity',
    options: sampleOptions,
    isInvalid: true,
    errorMessage: 'Severity classification is required',
  },
}

export const Disabled: Story = {
  args: {
    label: 'Environment',
    options: [{ id: 'prod', label: 'Production (Locked)' }],
    defaultSelectedKey: 'prod',
    isDisabled: true,
  },
}
