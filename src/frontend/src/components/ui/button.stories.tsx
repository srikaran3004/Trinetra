import type { Meta, StoryObj } from '@storybook/react'
import { Button } from './button'

const meta: Meta<typeof Button> = {
  title: 'Actions/Button',
  component: Button,
  tags: ['autodocs'],
  argTypes: {
    variant: {
      control: 'select',
      options: ['primary', 'secondary', 'ghost', 'danger'],
    },
    size: {
      control: 'select',
      options: ['sm', 'md', 'lg'],
    },
    isDisabled: { control: 'boolean' },
    isLoading: { control: 'boolean' },
  },
}

export default meta
type Story = StoryObj<typeof Button>

export const Primary: Story = {
  args: {
    variant: 'primary',
    children: 'Approve remediation',
  },
}

export const Secondary: Story = {
  args: {
    variant: 'secondary',
    children: 'View runbook',
  },
}

export const Ghost: Story = {
  args: {
    variant: 'ghost',
    children: 'Dismiss',
  },
}

export const Danger: Story = {
  args: {
    variant: 'danger',
    children: 'Abort incident',
  },
}

export const Small: Story = {
  args: {
    size: 'sm',
    variant: 'secondary',
    children: 'Filter',
  },
}

export const Large: Story = {
  args: {
    size: 'lg',
    variant: 'primary',
    children: 'Open console',
  },
}

export const Disabled: Story = {
  args: {
    isDisabled: true,
    children: 'Insufficient permissions',
  },
}

export const Loading: Story = {
  args: {
    isLoading: true,
    loadingText: 'Executing...',
    children: 'Execute',
  },
}
