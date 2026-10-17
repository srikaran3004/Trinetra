import type { Meta, StoryObj } from '@storybook/react'
import { StateProgress } from './state-progress'

const meta: Meta<typeof StateProgress> = {
  title: 'Data/StateProgress',
  component: StateProgress,
  tags: ['autodocs'],
}

export default meta
type Story = StoryObj<typeof StateProgress>

export const Triggered: Story = {
  args: {
    currentStatus: 'triggered',
  },
}

export const Investigating: Story = {
  args: {
    currentStatus: 'investigating',
  },
}

export const AnalysisComplete: Story = {
  args: {
    currentStatus: 'analysis_complete',
  },
}

export const AwaitingApproval: Story = {
  args: {
    currentStatus: 'awaiting_approval',
  },
}

export const ExecutingRemediation: Story = {
  args: {
    currentStatus: 'executing_remediation',
  },
}

export const MonitoringRecovery: Story = {
  args: {
    currentStatus: 'monitoring_recovery',
  },
}

export const Resolved: Story = {
  args: {
    currentStatus: 'resolved',
  },
}

export const EscalatedBranch: Story = {
  args: {
    currentStatus: 'escalated',
  },
}
