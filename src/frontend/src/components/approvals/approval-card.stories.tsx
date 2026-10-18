import type { Meta, StoryObj } from '@storybook/react'
import { ApprovalCard } from './approval-card'
import type { ApprovalRequest } from '@/api/types'
import { AuthProvider, type User } from '@/auth/auth-context'

const meta: Meta<typeof ApprovalCard> = {
  title: 'Components/Approvals/ApprovalCard',
  component: ApprovalCard,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
  },
}

export default meta
type Story = StoryObj<typeof ApprovalCard>

const mockAdminUser: User = {
  id: 'usr-admin-01',
  name: 'SRE Lead',
  email: 'admin@trinetra.internal',
  role: 'Admin',
}

const mockViewerUser: User = {
  id: 'usr-viewer-01',
  name: 'Security Auditor',
  email: 'viewer@trinetra.internal',
  role: 'Viewer',
}

const baseApproval: ApprovalRequest = {
  id: 'appr-story-01',
  incident_id: 'INC-2024-8901',
  action: 'Terminate idle connections and increase connection pool',
  command:
    "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE state = 'idle in transaction' AND state_change < current_timestamp - INTERVAL '5 minutes';",
  risk_level: 'high',
  approval_level: 'lead',
  timeout_at: new Date(Date.now() + 12 * 60 * 1000).toISOString(),
  created_at: new Date(Date.now() - 3 * 60 * 1000).toISOString(),
  status: 'pending',
  notes: 'Connection pool saturation causing 18% auth errors. Requires SRE Lead confirmation.',
  rollback_description:
    'Revert MaxPoolSize back to 100; reload auth-api configuration with default connection settings.',
  verdict: 'requires_approval',
}

export const HighRiskPending: Story = {
  render: () => (
    <AuthProvider initialUser={mockAdminUser}>
      <div className="max-w-2xl">
        <ApprovalCard approval={baseApproval} onApprove={async () => {}} onDeny={async () => {}} />
      </div>
    </AuthProvider>
  ),
}

export const ExpiringSoonCountdown: Story = {
  render: () => {
    const expiringApproval: ApprovalRequest = {
      ...baseApproval,
      id: 'appr-story-expiring',
      action: 'Drain and isolate compromised border gateway router',
      command: 'edge-router-cli --action drain --target bgp-gw-02 --grace-period 30s',
      timeout_at: new Date(Date.now() + 45 * 1000).toISOString(), // 45 seconds remaining
    }
    return (
      <AuthProvider initialUser={mockAdminUser}>
        <div className="max-w-2xl">
          <ApprovalCard
            approval={expiringApproval}
            onApprove={async () => {}}
            onDeny={async () => {}}
          />
        </div>
      </AuthProvider>
    )
  },
}

export const MediumRiskOperator: Story = {
  render: () => {
    const operatorApproval: ApprovalRequest = {
      ...baseApproval,
      id: 'appr-story-operator',
      action: 'Restart redis-cart cache cluster pods',
      command: 'kubectl rollout restart deployment/redis-cart -n cart-prod',
      risk_level: 'medium',
      approval_level: 'operator',
      rollback_description: 'kubectl rollout undo deployment/redis-cart -n cart-prod',
    }
    return (
      <AuthProvider initialUser={mockAdminUser}>
        <div className="max-w-2xl">
          <ApprovalCard
            approval={operatorApproval}
            onApprove={async () => {}}
            onDeny={async () => {}}
          />
        </div>
      </AuthProvider>
    )
  },
}

export const BlockedByGuardrail: Story = {
  render: () => {
    const blockedApproval: ApprovalRequest = {
      ...baseApproval,
      id: 'appr-story-blocked',
      action: 'Force delete all pods in namespace auth (BLOCKED BY GUARDRAIL)',
      command: 'kubectl delete pods --all -n auth --force --grace-period=0',
      verdict: 'blocked_by_guardrail',
      rollback_description: 'Cannot rollback service outage caused by simultaneous pod termination',
    }
    return (
      <AuthProvider initialUser={mockAdminUser}>
        <div className="max-w-2xl">
          <ApprovalCard
            approval={blockedApproval}
            onApprove={async () => {}}
            onDeny={async () => {}}
          />
        </div>
      </AuthProvider>
    )
  },
}

export const UnauthorizedViewerRole: Story = {
  render: () => (
    <AuthProvider initialUser={mockViewerUser}>
      <div className="max-w-2xl">
        <ApprovalCard approval={baseApproval} onApprove={async () => {}} onDeny={async () => {}} />
      </div>
    </AuthProvider>
  ),
}

export const ApprovedSuccessState: Story = {
  render: () => {
    const approvedItem: ApprovalRequest = {
      ...baseApproval,
      id: 'appr-story-approved',
      status: 'approved',
    }
    return (
      <AuthProvider initialUser={mockAdminUser}>
        <div className="max-w-2xl">
          <ApprovalCard
            approval={approvedItem}
            onApprove={async () => {}}
            onDeny={async () => {}}
          />
        </div>
      </AuthProvider>
    )
  },
}
