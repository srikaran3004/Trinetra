import { Card } from '@/components/ui/card'

export function IncidentHistoryPage() {
  return (
    <div className="p-[var(--space-6)] max-w-7xl mx-auto space-y-[var(--space-6)]">
      <div>
        <h2 className="text-[var(--text-xl)] font-bold text-[var(--color-text-primary)] m-0">
          Incident History & Metrics
        </h2>
        <p className="text-[var(--text-sm)] text-[var(--color-text-secondary)] mt-1">
          Historical post-mortems, resolution timelines, and autonomous MTTR analytics.
        </p>
      </div>

      <Card className="p-[var(--space-4)]">
        <p className="text-[var(--text-sm)] text-[var(--color-text-secondary)]">
          Historical charts and post-mortem browser (Phase F5).
        </p>
      </Card>
    </div>
  )
}
