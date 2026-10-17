import type { Meta, StoryObj } from '@storybook/react'
import { Dialog } from './dialog'
import { Button } from './button'

const meta: Meta<typeof Dialog> = {
  title: 'Overlays/Dialog',
  component: Dialog,
  tags: ['autodocs'],
}

export default meta
type Story = StoryObj<typeof Dialog>

export const SimpleConfirm: Story = {
  render: () => (
    <Dialog
      title="Approve Remediation Plan"
      description="This will execute the rolling restart on payment-gateway pods."
      confirmLabel="Approve & Execute"
      trigger={<Button size="sm">Approve Plan</Button>}
    >
      <p>Preflight checks have passed. Target cluster: us-east-prod.</p>
    </Dialog>
  ),
}

export const HighRiskTypedConfirmation: Story = {
  render: () => (
    <Dialog
      title="Execute Database Failover (High Risk)"
      description="This will promote read replica to primary and terminate active connections."
      confirmLabel="Confirm Failover"
      confirmVariant="danger"
      confirmPhrase="INC-8492"
      trigger={
        <Button size="sm" variant="danger">
          Execute High-Risk Action
        </Button>
      }
    >
      <p className="text-[var(--color-critical)] font-medium">
        Warning: This action requires typing the Incident ID before execution can proceed.
      </p>
    </Dialog>
  ),
}
