import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'

export function ApprovalsPage() {
  return (
    <div className="p-[var(--space-6)] max-w-7xl mx-auto space-y-[var(--space-6)]">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-[var(--text-xl)] font-bold text-[var(--color-text-primary)] m-0">
            Remediation Approval Queue
          </h2>
          <p className="text-[var(--text-sm)] text-[var(--color-text-secondary)] mt-1">
            Human-in-the-loop review for high and medium-risk autonomous remediation plans.
          </p>
        </div>
        <Badge variant="high" size="sm">
          1 Action Pending
        </Badge>
      </div>

      <Card className="p-[var(--space-4)]">
        <p className="text-[var(--text-sm)] text-[var(--color-text-secondary)]">
          Approval queue with Countdown, typed confirmation, and optimistic updates (Phase F4).
        </p>
      </Card>
    </div>
  )
}
