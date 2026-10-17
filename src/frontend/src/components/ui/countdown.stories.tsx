import type { Meta, StoryObj } from '@storybook/react'
import { Countdown } from './countdown'

const meta: Meta<typeof Countdown> = {
  title: 'Display/Countdown',
  component: Countdown,
  tags: ['autodocs'],
  args: {
    initialSeconds: 900, // 15 mins
  },
}

export default meta
type Story = StoryObj<typeof Countdown>

export const FullFifteenMinutes: Story = {
  args: {
    initialSeconds: 900,
  },
}

export const TwoMinutesWarning: Story = {
  args: {
    initialSeconds: 120,
  },
}

export const LastMinuteWithSeconds: Story = {
  args: {
    initialSeconds: 45,
  },
}

export const TimedOut: Story = {
  args: {
    initialSeconds: 0,
  },
}
