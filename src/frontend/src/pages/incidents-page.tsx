import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'

export function IncidentsPage() {
  return (
    <div className="p-[var(--space-6)] max-w-7xl mx-auto space-y-[var(--space-6)]">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-[var(--text-xl)] font-bold text-[var(--color-text-primary)] m-0">
            Active Incidents
          </h2>
          <p className="text-[var(--text-sm)] text-[var(--color-text-secondary)] mt-1">
            Real-time feed of active system incidents under autonomous investigation and mitigation.
          </p>
        </div>
        <div className="flex items-center gap-[var(--space-2)]">
          <Badge variant="critical" size="sm">
            1 Critical
          </Badge>
          <Badge variant="high" size="sm">
            1 High
          </Badge>
        </div>
      </div>

      <div className="grid gap-[var(--space-4)]">
        <Card className="p-[var(--space-4)]">
          <div className="flex items-start justify-between">
            <div className="space-y-1">
              <div className="flex items-center gap-2">
                <Badge variant="critical" size="sm">
                  CRITICAL
                </Badge>
                <span className="font-mono text-xs text-[var(--color-text-tertiary)]">
                  INC-2024-8901
                </span>
              </div>
              <h3 className="text-[var(--text-base)] font-semibold text-[var(--color-text-primary)]">
                Database connection pool exhaustion on payment-db-primary
              </h3>
              <p className="text-[var(--text-xs)] text-[var(--color-text-secondary)]">
                Root cause identified with 89% confidence. Remediation awaiting approval.
              </p>
            </div>
          </div>
        </Card>
      </div>
    </div>
  )
}
