import type { Meta, StoryObj } from '@storybook/react'
import { MarkdownView } from './markdown-view'

const sampleRunbook = `
# Runbook: PostgreSQL Connection Pool Saturation (RB-041)

> **Severity:** Critical (P1) | **Service:** Auth & Session Store

### Diagnostic Steps
- Check current active connections against \`max_connections\`.
- Inspect long-running transactions:
\`\`\`sql
SELECT pid, now() - xact_start AS duration, query 
FROM pg_stat_activity 
WHERE state = 'active' ORDER BY duration DESC;
\`\`\`

### Remediation Policy
1. Run preflight health check.
2. Terminate idle-in-transaction connections exceeding 120s.
3. Scale connection pooler deployment.
`

const meta: Meta<typeof MarkdownView> = {
  title: 'Display/MarkdownView',
  component: MarkdownView,
  tags: ['autodocs'],
  args: {
    content: sampleRunbook,
  },
}

export default meta
type Story = StoryObj<typeof MarkdownView>

export const Runbook: Story = {}

export const XSSSanitizationDemo: Story = {
  args: {
    content: `
# Safe Content
This contains an attempted malicious payload:
<script>alert('XSS Attack');</script>
<img src="x" onerror="alert('XSS')" />
<iframe src="https://evil.com"></iframe>

The malicious tags are safely stripped by DOMPurify.
`,
  },
}
