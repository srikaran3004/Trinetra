import { useParams } from 'react-router'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'

export function IncidentDetailPage() {
  const { id } = useParams()

  return (
    <div className="p-[var(--space-6)] max-w-7xl mx-auto space-y-[var(--space-6)]">
      <div className="flex items-center gap-[var(--space-3)]">
        <Badge variant="critical">CRITICAL</Badge>
        <h2 className="text-[var(--text-xl)] font-bold text-[var(--color-text-primary)] m-0">
          Incident {id}
        </h2>
      </div>

      <Card className="p-[var(--space-4)]">
        <p className="text-[var(--text-sm)] text-[var(--color-text-secondary)]">
          Incident detail view with StateProgress, root cause, remediation options, and agent
          timeline (Phase F3).
        </p>
      </Card>
    </div>
  )
}
