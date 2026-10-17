import type { Meta, StoryObj } from '@storybook/react'
import { Bell, RefreshCw, X } from 'lucide-react'
import { IconButton } from './icon-button'

const meta: Meta<typeof IconButton> = {
  title: 'Actions/IconButton',
  component: IconButton,
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
type Story = StoryObj<typeof IconButton>

export const Ghost: Story = {
  args: {
    'aria-label': 'Refresh active incidents',
    icon: <RefreshCw size={16} strokeWidth={1.5} />,
    variant: 'ghost',
  },
}

export const Secondary: Story = {
  args: {
    'aria-label': 'View notifications',
    icon: <Bell size={16} strokeWidth={1.5} />,
    variant: 'secondary',
  },
}

export const Danger: Story = {
  args: {
    'aria-label': 'Close dialog',
    icon: <X size={16} strokeWidth={1.5} />,
    variant: 'danger',
  },
}

export const TouchTargetLg: Story = {
  args: {
    'aria-label': 'Refresh feed (44px touch target)',
    icon: <RefreshCw size={20} strokeWidth={1.5} />,
    variant: 'secondary',
    size: 'lg',
  },
}
