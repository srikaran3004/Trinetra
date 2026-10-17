import { Card } from '@/components/ui/card'

export function RunbooksPage() {
  return (
    <div className="p-[var(--space-6)] max-w-7xl mx-auto space-y-[var(--space-6)]">
      <div>
        <h2 className="text-[var(--text-xl)] font-bold text-[var(--color-text-primary)] m-0">
          Runbooks & Playbooks Catalog
        </h2>
        <p className="text-[var(--text-sm)] text-[var(--color-text-secondary)] mt-1">
          Searchable catalog of incident response procedures with sanitized Markdown viewing.
        </p>
      </div>

      <Card className="p-[var(--space-4)]">
        <p className="text-[var(--text-sm)] text-[var(--color-text-secondary)]">
          Searchable runbooks viewer (Phase F5).
        </p>
      </Card>
    </div>
  )
}
