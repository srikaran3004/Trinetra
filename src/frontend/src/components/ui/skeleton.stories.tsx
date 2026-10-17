import type { Meta, StoryObj } from '@storybook/react'
import { Skeleton } from './skeleton'

const meta: Meta<typeof Skeleton> = {
  title: 'Feedback/Skeleton',
  component: Skeleton,
  tags: ['autodocs'],
}

export default meta
type Story = StoryObj<typeof Skeleton>

export const IncidentCardSkeleton: Story = {
  render: () => (
    <div className="flex flex-col gap-3 p-4 max-w-md border border-[var(--color-border-subtle)] rounded-[var(--radius-md)] bg-[var(--color-surface-raised)]">
      <div className="flex items-center gap-3">
        <Skeleton variant="circle" className="w-8 h-8" />
        <div className="flex flex-col gap-1.5 flex-1">
          <Skeleton variant="text" className="w-3/4 h-3.5" />
          <Skeleton variant="text" className="w-1/2 h-3" />
        </div>
      </div>
      <Skeleton variant="rect" className="w-full h-16" />
      <div className="flex justify-between items-center pt-2">
        <Skeleton variant="text" className="w-20 h-4" />
        <Skeleton variant="text" className="w-16 h-6" />
      </div>
    </div>
  ),
}
