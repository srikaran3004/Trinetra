import type { Meta, StoryObj } from '@storybook/react'
import { KeyboardShortcut } from './keyboard-shortcut'

const meta: Meta<typeof KeyboardShortcut> = {
  title: 'Actions/KeyboardShortcut',
  component: KeyboardShortcut,
  tags: ['autodocs'],
}

export default meta
type Story = StoryObj<typeof KeyboardShortcut>

export const SingleKey: Story = {
  args: {
    keys: ['/'],
  },
}

export const KeyCombination: Story = {
  args: {
    keys: ['⌘', 'K'],
  },
}

export const SequentialShortcut: Story = {
  args: {
    keys: ['g', 'i'],
  },
}

export const EscapeKey: Story = {
  args: {
    keys: ['Esc'],
  },
}
