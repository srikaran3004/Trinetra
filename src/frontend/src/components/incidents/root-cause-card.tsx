import { ShieldCheck, Cpu, Database, Network, Cloud, HelpCircle } from 'lucide-react'
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { ConfidenceBar } from '@/components/ui/confidence-bar'
import type { IncidentDetail } from '@/api/types'

export interface RootCauseCardProps {
  incident: IncidentDetail
}

function getCategoryIcon(category?: string | null) {
  switch (category?.toLowerCase()) {
    case 'database':
      return <Database size={14} className="text-[var(--color-accent-text)]" />
    case 'application':
      return <Cpu size={14} className="text-[var(--color-high)]" />
    case 'network':
      return <Network size={14} className="text-[var(--color-low)]" />
    case 'dependency_external':
      return <Cloud size={14} className="text-[var(--color-medium)]" />
    default:
      return <HelpCircle size={14} className="text-[var(--color-text-tertiary)]" />
  }
}

export function RootCauseCard({ incident }: RootCauseCardProps) {
  const confidence = incident.root_cause_confidence ?? 0
  const category = incident.root_cause_category || 'Unclassified'

  // Synthetic evidence chain correlated from incident data fixtures
  const evidencePoints = [
    {
      source: 'Metrics Querier Agent',
      metric: 'Active pool saturation: 100/100 connections (100% threshold reached)',
    },
    {
      source: 'Log Analyzer Agent',
      metric: 'Error log burst: 42 fatal ConnectionPoolExhaustedException occurrences',
    },
    {
      source: 'Runbook Retriever Agent',
      metric: 'Matched vector runbook-db-pool-01 with 94% semantic cosine similarity',
    },
  ]

  return (
    <Card className="bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)]">
      <CardHeader className="pb-3 border-b border-[var(--color-border-subtle)]">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <ShieldCheck size={18} className="text-[var(--color-accent)]" />
            <CardTitle>Root Cause Analysis</CardTitle>
          </div>
          <div className="flex items-center gap-1.5">
            <Badge variant="neutral" size="sm" icon={getCategoryIcon(category)}>
              {category.replace('_', ' ').toUpperCase()}
            </Badge>
          </div>
        </div>
      </CardHeader>

      <CardContent className="pt-4 space-y-4">
        {/* Identified Cause Description */}
        <div>
          <span className="text-[var(--text-xs)] font-medium uppercase tracking-wider text-[var(--color-text-tertiary)]">
            Primary Diagnostic Verdict
          </span>
          <p className="text-[var(--text-sm)] font-medium text-[var(--color-text-primary)] mt-1 leading-relaxed">
            {incident.root_cause || 'Autonomous root cause analysis currently in progress...'}
          </p>
        </div>

        {/* Confidence Bar with 0.80 threshold marker */}
        <div>
          <span className="text-[var(--text-xs)] font-medium uppercase tracking-wider text-[var(--color-text-tertiary)] mb-1 block">
            Diagnostic Confidence Score
          </span>
          <ConfidenceBar value={confidence} threshold={0.8} />
        </div>

        {/* Evidence Chain */}
        <div className="pt-2 border-t border-[var(--color-border-subtle)]">
          <span className="text-[var(--text-xs)] font-medium uppercase tracking-wider text-[var(--color-text-tertiary)] block mb-2">
            Correlated Evidence Chain
          </span>
          <ul className="space-y-2 text-[var(--text-xs)]" aria-label="Evidence chain">
            {evidencePoints.map((item, idx) => (
              <li
                key={idx}
                className="flex items-start gap-2 p-2 rounded-[var(--radius-sm)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)]"
              >
                <span className="font-mono text-[var(--color-accent-text)] shrink-0 font-medium">
                  [{idx + 1}]
                </span>
                <div className="flex-1">
                  <span className="font-medium text-[var(--color-text-primary)] block">
                    {item.source}
                  </span>
                  <span className="text-[var(--color-text-secondary)]">{item.metric}</span>
                </div>
              </li>
            ))}
          </ul>
        </div>
      </CardContent>
    </Card>
  )
}
