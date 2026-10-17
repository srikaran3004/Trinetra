import type { Meta, StoryObj } from '@storybook/react'
import { DateRange } from './date-range'

const meta: Meta<typeof DateRange> = {
  title: 'Inputs/DateRange',
  component: DateRange,
  tags: ['autodocs'],
  args: {
    defaultValue: {
      preset: '30d',
      start: '2026-09-17',
      end: '2026-10-17',
    },
  },
}

export default meta
type Story = StoryObj<typeof DateRange>

export const Default: Story = {}

export const Preset7Days: Story = {
  args: {
    defaultValue: {
      preset: '7d',
      start: '2026-10-10',
      end: '2026-10-17',
    },
  },
}

export const Preset90Days: Story = {
  args: {
    defaultValue: {
      preset: '90d',
      start: '2026-07-19',
      end: '2026-10-17',
    },
  },
}

export const Disabled: Story = {
  args: {
    isDisabled: true,
  },
}
