import type { Meta, StoryObj } from '@storybook/react'
import { TextField } from './text-field'
import { Search } from 'lucide-react'

const meta: Meta<typeof TextField> = {
  title: 'Inputs/TextField',
  component: TextField,
  tags: ['autodocs'],
  args: {
    label: 'Incident Title',
    placeholder: 'Enter incident title...',
  },
}

export default meta
type Story = StoryObj<typeof TextField>

export const Default: Story = {}

export const WithDescription: Story = {
  args: {
    label: 'Service Name',
    description: 'The microservice reporting anomalous behavior',
    placeholder: 'e.g. payment-service',
  },
}

export const WithError: Story = {
  args: {
    label: 'Incident Title',
    defaultValue: '',
    isInvalid: true,
    errorMessage: 'Incident title is required',
  },
}

export const WithIcons: Story = {
  args: {
    label: 'Search Logs',
    placeholder: 'Search traces or stack traces...',
    prefixIcon: <Search size={14} strokeWidth={1.5} />,
  },
}

export const Sizes: Story = {
  render: () => (
    <div className="flex flex-col gap-4 max-w-sm">
      <TextField inputSize="sm" label="Small (32px)" placeholder="Small input..." />
      <TextField inputSize="md" label="Medium (36px)" placeholder="Medium input..." />
      <TextField inputSize="lg" label="Large (40px)" placeholder="Large input..." />
    </div>
  ),
}

export const Disabled: Story = {
  args: {
    label: 'System ID',
    defaultValue: 'SYS-9841',
    isDisabled: true,
  },
}
