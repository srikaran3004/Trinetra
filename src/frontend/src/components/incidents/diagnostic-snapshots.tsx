import { BarChart3, FileText, BookOpen, ExternalLink } from 'lucide-react'
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { CodeBlock } from '@/components/ui/code-block'
import { Link } from 'react-router'
import type { IncidentDetail } from '@/api/types'

export interface DiagnosticSnapshotsProps {
  incident: IncidentDetail
}

export function DiagnosticSnapshots({ incident }: DiagnosticSnapshotsProps) {
  // Realistic log excerpt correlated for the incident
  const sampleLogExcerpt = `2026-10-17T16:30:12.451Z [ERROR] [auth-api] ConnectionPoolTimeoutException: Timeout waiting for connection from pool (10000ms)
  at Npgsql.NpgsqlConnector.Open(NpgsqlTimeout timeout, Boolean async, CancellationToken cancellationToken)
  at Npgsql.ConnectorPool.AllocateLong(NpgsqlTimeout timeout, Boolean async, CancellationToken cancellationToken)
  at Npgsql.NpgsqlConnection.Open()
  at Trinetra.Infrastructure.Data.PostgresConnectionFactory.CreateConnectionAsync() in /src/Infrastructure/Data/PostgresConnectionFactory.cs:line 48
  at Trinetra.API.Controllers.AuthController.Login(LoginRequest request) in /src/API/Controllers/AuthController.cs:line 72
2026-10-17T16:30:14.882Z [WARN]  [auth-api] Active connections count reached max_limit (100/100). Eviction rate 0 req/sec.`

  const linkedRunbooks = [
    {
      id: 'runbook-db-pool-01',
      title: 'PostgreSQL Connection Pool Exhaustion Mitigation',
      similarityScore: 0.94,
      service: 'postgres',
    },
    {
      id: 'runbook-pod-oom-02',
      title: 'Kubernetes Pod Out-Of-Memory Cascade Remediation',
      similarityScore: 0.78,
      service: 'kubernetes',
    },
  ]

  return (
    <div className="space-y-6">
      {/* 1. Telemetry Metrics Summary */}
      <Card className="bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)]">
        <CardHeader className="pb-3 border-b border-[var(--color-border-subtle)]">
          <div className="flex items-center gap-2">
            <BarChart3 size={18} className="text-[var(--color-accent)]" />
            <CardTitle>Telemetry Snapshot at Trigger Time ({incident.id})</CardTitle>
          </div>
        </CardHeader>
        <CardContent className="pt-4">
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
            <div className="p-3 rounded-[var(--radius-sm)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)]">
              <span className="text-[11px] font-mono uppercase tracking-wider text-[var(--color-text-tertiary)] block">
                Error Rate (P99)
              </span>
              <span className="text-[var(--text-lg)] font-bold text-[var(--color-critical)] font-mono mt-1 block">
                18.4%
              </span>
              <span className="text-[10px] text-[var(--color-text-secondary)]">
                Baseline &lt; 0.5%
              </span>
            </div>

            <div className="p-3 rounded-[var(--radius-sm)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)]">
              <span className="text-[11px] font-mono uppercase tracking-wider text-[var(--color-text-tertiary)] block">
                P99 Latency
              </span>
              <span className="text-[var(--text-lg)] font-bold text-[var(--color-high)] font-mono mt-1 block">
                4,210ms
              </span>
              <span className="text-[10px] text-[var(--color-text-secondary)]">
                SLA target 350ms
              </span>
            </div>

            <div className="p-3 rounded-[var(--radius-sm)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)]">
              <span className="text-[11px] font-mono uppercase tracking-wider text-[var(--color-text-tertiary)] block">
                Pool Saturation
              </span>
              <span className="text-[var(--text-lg)] font-bold text-[var(--color-critical)] font-mono mt-1 block">
                100%
              </span>
              <span className="text-[10px] text-[var(--color-text-secondary)]">
                100 / 100 conns
              </span>
            </div>

            <div className="p-3 rounded-[var(--radius-sm)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)]">
              <span className="text-[11px] font-mono uppercase tracking-wider text-[var(--color-text-tertiary)] block">
                CPU Utilization
              </span>
              <span className="text-[var(--text-lg)] font-bold text-[var(--color-success)] font-mono mt-1 block">
                34.2%
              </span>
              <span className="text-[10px] text-[var(--color-text-secondary)]">
                Normal capacity
              </span>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* 2. Virtualized Geist Mono Log Excerpt */}
      <Card className="bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)]">
        <CardHeader className="pb-3 border-b border-[var(--color-border-subtle)]">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <FileText size={18} className="text-[var(--color-accent)]" />
              <CardTitle>Correlated Log Excerpt (Geist Mono)</CardTitle>
            </div>
            <span className="text-[var(--text-xs)] text-[var(--color-text-tertiary)] font-mono">
              {incident.affected_services.join(', ')}
            </span>
          </div>
        </CardHeader>
        <CardContent className="pt-4">
          <CodeBlock code={sampleLogExcerpt} language="log" className="max-h-64 overflow-y-auto" />
        </CardContent>
      </Card>

      {/* 3. Linked Runbooks with pgvector cosine similarity */}
      <Card className="bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)]">
        <CardHeader className="pb-3 border-b border-[var(--color-border-subtle)]">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <BookOpen size={18} className="text-[var(--color-accent)]" />
              <CardTitle>Linked Runbooks (pgvector Vector Search)</CardTitle>
            </div>
            <span className="text-[var(--text-xs)] text-[var(--color-text-tertiary)]">
              RAG Matched
            </span>
          </div>
        </CardHeader>
        <CardContent className="pt-4 space-y-3">
          {linkedRunbooks.map((rb) => (
            <div
              key={rb.id}
              className="flex items-center justify-between p-3 rounded-[var(--radius-sm)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] hover:border-[var(--color-border-strong)] transition-colors"
            >
              <div className="space-y-1">
                <div className="flex items-center gap-2">
                  <span className="font-mono text-[var(--text-xs)] text-[var(--color-text-tertiary)] font-semibold">
                    {rb.id}
                  </span>
                  <Badge variant="accent" size="sm">
                    {Math.round(rb.similarityScore * 100)}% Match
                  </Badge>
                </div>
                <h5 className="text-[var(--text-sm)] font-semibold text-[var(--color-text-primary)]">
                  {rb.title}
                </h5>
              </div>

              <Link
                to="/runbooks"
                className="inline-flex items-center gap-1 text-[var(--text-xs)] text-[var(--color-accent-text)] hover:underline shrink-0 ml-4"
              >
                <span>View runbook</span>
                <ExternalLink size={12} />
              </Link>
            </div>
          ))}
        </CardContent>
      </Card>
    </div>
  )
}
