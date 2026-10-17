import type { Meta, StoryObj } from '@storybook/react'
import { CodeBlock } from './code-block'

const meta: Meta<typeof CodeBlock> = {
  title: 'Display/CodeBlock',
  component: CodeBlock,
  tags: ['autodocs'],
  args: {
    code: 'kubectl rollout restart deployment/payment-gateway -n production',
    language: 'bash',
  },
}

export default meta
type Story = StoryObj<typeof CodeBlock>

export const SingleLineCommand: Story = {}

export const WithTitleAndLineNumbers: Story = {
  args: {
    title: 'remediation-step-1.sh',
    showLineNumbers: true,
    code: `#!/usr/bin/env bash
# Step 1: Pre-flight check
pg_isready -h postgres.internal -p 5432 || exit 1

# Step 2: Terminate idle pool connections
psql -h postgres.internal -U sre -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE state = 'idle';"

# Step 3: Rolling restart of affected consumers
kubectl rollout restart deployment/auth-service -n production`,
  },
}

export const LongScrollableLog: Story = {
  args: {
    title: 'auth-service.log',
    code: `2026-10-17T14:32:01.402Z [ERROR] [auth-service] System.TimeoutException: The operation has timed out while waiting for connection from pool: NpgsqlConnectionPool.GetConnectionAsync()
2026-10-17T14:32:01.455Z [WARN]  [auth-service] Connection pool saturated: 100/100 connections in use. Queued requests: 432.
2026-10-17T14:32:01.512Z [FATAL] [coordinator] Incident triggered: INC-8492 [P1 Critical] Service degradation detected across auth-service.`,
  },
}
