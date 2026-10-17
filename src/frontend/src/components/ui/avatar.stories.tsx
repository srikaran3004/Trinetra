import type { Meta, StoryObj } from '@storybook/react'
import { Avatar } from './avatar'

const meta: Meta<typeof Avatar> = {
  title: 'Display/Avatar',
  component: Avatar,
  tags: ['autodocs'],
  args: {
    initials: 'TR',
  },
}

export default meta
type Story = StoryObj<typeof Avatar>

export const Initials: Story = {}

export const Agents: Story = {
  render: () => (
    <div className="flex items-center gap-3">
      <Avatar initials="CA" isAgent title="Coordinator Agent" />
      <Avatar initials="LA" isAgent title="Log Analyzer" />
      <Avatar initials="MQ" isAgent title="Metrics Querier" />
      <Avatar initials="RR" isAgent title="Runbook Retriever" />
      <Avatar initials="RC" isAgent title="Root Cause Analyzer" />
      <Avatar initials="RP" isAgent title="Remediation Proposer" />
      <Avatar initials="AE" isAgent title="Auto-Executor" />
      <Avatar initials="PM" isAgent title="Post-Mortem Generator" />
    </div>
  ),
}

export const Sizes: Story = {
  render: () => (
    <div className="flex items-center gap-3">
      <Avatar size="sm" initials="SM" />
      <Avatar size="md" initials="MD" />
      <Avatar size="lg" initials="LG" />
    </div>
  ),
}
