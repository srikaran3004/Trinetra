import type { Meta, StoryObj } from '@storybook/react'
import { AgentCard } from './agent-card'
import { MemoryRouter } from 'react-router'
import type { Agent } from '@/api/types'

const sampleWorkingAgent: Agent = {
  id: 'agent-coordinator',
  name: 'Incident Coordinator',
  status: 'working',
  current_tool_call: 'orchestrate_parallel_investigation(INC-2024-8901)',
  elapsed_time_seconds: 145,
  guardrail_violations_count: 0,
  last_active_at: new Date(Date.now() - 30_000).toISOString(),
  current_incident_id: 'INC-2024-8901',
}

const sampleWaitingAgent: Agent = {
  id: 'agent-remediation-proposer',
  name: 'Remediation Proposer',
  status: 'waiting',
  current_tool_call: 'await_human_approval(appr-8901)',
  elapsed_time_seconds: 420,
  guardrail_violations_count: 0,
  last_active_at: new Date(Date.now() - 120_000).toISOString(),
  current_incident_id: 'INC-2024-8901',
}

const sampleIdleAgent: Agent = {
  id: 'agent-log-analyzer',
  name: 'Log Analyzer',
  status: 'idle',
  current_tool_call: null,
  elapsed_time_seconds: 0,
  guardrail_violations_count: 0,
  last_active_at: new Date(Date.now() - 600_000).toISOString(),
  current_incident_id: null,
}

const sampleViolatingAgent: Agent = {
  id: 'agent-auto-executor',
  name: 'Auto-Executor',
  status: 'failed',
  current_tool_call: 'intercept_command("rm -rf /var/log/orders/*")',
  elapsed_time_seconds: 15,
  guardrail_violations_count: 2,
  last_active_at: new Date(Date.now() - 45_000).toISOString(),
  current_incident_id: 'INC-2024-8899',
}

const meta = {
  title: 'Agents/AgentCard',
  component: AgentCard,
  decorators: [
    (Story) => (
      <MemoryRouter>
        <div className="max-w-md p-4">
          <Story />
        </div>
      </MemoryRouter>
    ),
  ],
  tags: ['autodocs'],
} satisfies Meta<typeof AgentCard>

export default meta
type Story = StoryObj<typeof meta>

export const Working: Story = {
  args: {
    agent: sampleWorkingAgent,
  },
}

export const WaitingOnGate: Story = {
  args: {
    agent: sampleWaitingAgent,
  },
}

export const Idle: Story = {
  args: {
    agent: sampleIdleAgent,
  },
}

export const GuardrailViolations: Story = {
  args: {
    agent: sampleViolatingAgent,
  },
}
