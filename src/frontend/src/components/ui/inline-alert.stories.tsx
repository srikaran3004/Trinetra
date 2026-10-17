import type { Meta, StoryObj } from '@storybook/react'
import { InlineAlert } from './inline-alert'
import { Button } from './button'

const meta: Meta<typeof InlineAlert> = {
  title: 'Feedback/InlineAlert',
  component: InlineAlert,
  tags: ['autodocs'],
}

export default meta
type Story = StoryObj<typeof InlineAlert>

export const CriticalRollback: Story = {
  args: {
    variant: 'critical',
    title: 'Remediation Step Failed — Rolled Back',
    children:
      'Container restart returned non-zero exit code (137 OOMKilled). The system initiated auto-rollback to baseline image.',
    action: (
      <Button size="sm" variant="danger">
        Escalate to On-Call
      </Button>
    ),
  },
}

export const Warning: Story = {
  args: {
    variant: 'warning',
    title: 'Approval Approaching Timeout',
    children: 'Action INC-8492 will be automatically rejected in 2 minutes if unreviewed.',
  },
}

export const Success: Story = {
  args: {
    variant: 'success',
    title: 'Post-Remediation Verification Passed',
    children: 'Service health check passed. HTTP error rate returned to 0.02% (< 1% threshold).',
  },
}

export const Info: Story = {
  args: {
    variant: 'info',
    title: 'Guardrail Interception Logged',
    children: 'Non-whitelisted command intercepted. Audit event written to incident_events table.',
  },
}
