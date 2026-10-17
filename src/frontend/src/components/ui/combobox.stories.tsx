import type { Meta, StoryObj } from '@storybook/react'
import { Combobox } from './combobox'

const sampleServices = [
  { id: 'auth-service', label: 'auth-service', description: 'OAuth & session management' },
  { id: 'payment-gateway', label: 'payment-gateway', description: 'Stripe & PayPal processor' },
  {
    id: 'notification-worker',
    label: 'notification-worker',
    description: 'Email and push dispatch',
  },
  { id: 'database-primary', label: 'database-primary', description: 'Postgres primary cluster' },
  { id: 'redis-cache', label: 'redis-cache', description: 'Shared session store' },
]

const meta: Meta<typeof Combobox> = {
  title: 'Inputs/Combobox',
  component: Combobox,
  tags: ['autodocs'],
  args: {
    label: 'Affected Service',
    options: sampleServices,
    placeholder: 'Filter by service name...',
  },
}

export default meta
type Story = StoryObj<typeof Combobox>

export const Default: Story = {}

export const WithDescription: Story = {
  args: {
    label: 'Target Microservice',
    description: 'Type to filter services across clusters',
    options: sampleServices,
  },
}

export const WithError: Story = {
  args: {
    label: 'Target Microservice',
    options: sampleServices,
    isInvalid: true,
    errorMessage: 'Unknown service name',
  },
}

export const Disabled: Story = {
  args: {
    label: 'Target Microservice',
    options: sampleServices,
    defaultInputValue: 'auth-service',
    isDisabled: true,
  },
}
