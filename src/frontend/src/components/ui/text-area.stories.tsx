import type { Meta, StoryObj } from '@storybook/react'
import { TextArea } from './text-area'

const meta: Meta<typeof TextArea> = {
  title: 'Inputs/TextArea',
  component: TextArea,
  tags: ['autodocs'],
  args: {
    label: 'Denial Reason',
    placeholder: 'Explain why this remediation action is being denied...',
  },
}

export default meta
type Story = StoryObj<typeof TextArea>

export const Default: Story = {}

export const WithDescription: Story = {
  args: {
    label: 'Remediation Notes',
    description: 'Provide context for post-incident retrospective',
    rows: 4,
  },
}

export const WithError: Story = {
  args: {
    label: 'Denial Reason',
    isInvalid: true,
    errorMessage: 'Denial reason must be at least 10 characters',
    defaultValue: 'Too short',
  },
}

export const Disabled: Story = {
  args: {
    label: 'System Notes',
    defaultValue: 'Locked by automated coordinator agent',
    isDisabled: true,
  },
}
