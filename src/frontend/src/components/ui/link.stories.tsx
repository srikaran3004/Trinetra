import type { Meta, StoryObj } from '@storybook/react'
import { Link } from './link'

const meta: Meta<typeof Link> = {
  title: 'Actions/Link',
  component: Link,
  tags: ['autodocs'],
  argTypes: {
    variant: {
      control: 'select',
      options: ['default', 'subtle', 'muted'],
    },
    isExternal: { control: 'boolean' },
  },
}

export default meta
type Story = StoryObj<typeof Link>

export const Default: Story = {
  args: {
    href: '#',
    children: 'View post-mortem report',
    variant: 'default',
  },
}

export const Subtle: Story = {
  args: {
    href: '#',
    children: 'Documentation',
    variant: 'subtle',
  },
}

export const External: Story = {
  args: {
    href: 'https://github.com/srikaran3004/Trinetra',
    children: 'GitHub repository',
    isExternal: true,
  },
}
