import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'

export function AgentsPage() {
  return (
    <div className="p-[var(--space-6)] max-w-7xl mx-auto space-y-[var(--space-6)]">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-[var(--text-xl)] font-bold text-[var(--color-text-primary)] m-0">
            Autonomous Agent Fleet
          </h2>
          <p className="text-[var(--text-sm)] text-[var(--color-text-secondary)] mt-1">
            Real-time status, active tool calls, execution telemetry, and guardrail enforcement.
          </p>
        </div>
        <Badge variant="success" size="sm">
          8 Agents Active
        </Badge>
      </div>

      <Card className="p-[var(--space-4)]">
        <p className="text-[var(--text-sm)] text-[var(--color-text-secondary)]">
          Live agent activity monitor (Phase F4).
        </p>
      </Card>
    </div>
  )
}
