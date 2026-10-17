import { useNavigate } from 'react-router'
import {
  Wrench,
  ShieldAlert,
  ShieldCheck,
  Clock,
  ArrowRight,
  RotateCcw,
  CheckCircle,
  XCircle,
} from 'lucide-react'
import { Card, CardHeader, CardTitle, CardContent } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { CodeBlock } from '@/components/ui/code-block'
import type { RemediationOption, RemediationVerdict } from '@/api/types'

export interface RemediationCardProps {
  options: RemediationOption[]
  incidentId: string
}

function getVerdictBadge(verdict: RemediationVerdict) {
  switch (verdict) {
    case 'allowed':
      return (
        <Badge variant="success" size="sm" icon={<ShieldCheck size={12} />}>
          Allowed by Policy
        </Badge>
      )
    case 'requires_approval':
      return (
        <Badge variant="medium" size="sm" icon={<Clock size={12} />}>
          Requires Human Approval
        </Badge>
      )
    case 'blocked_by_guardrail':
      return (
        <Badge variant="critical" size="sm" icon={<ShieldAlert size={12} />}>
          Blocked by Guardrail
        </Badge>
      )
  }
}

export function RemediationCard({ options, incidentId }: RemediationCardProps) {
  const navigate = useNavigate()

  return (
    <Card className="bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)]">
      <CardHeader className="pb-3 border-b border-[var(--color-border-subtle)]">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Wrench size={18} className="text-[var(--color-accent)]" />
            <CardTitle>Proposed Remediation Plans</CardTitle>
          </div>
          <span className="text-[var(--text-xs)] text-[var(--color-text-tertiary)]">
            {options.length} {options.length === 1 ? 'Option' : 'Options'} Evaluated
          </span>
        </div>
      </CardHeader>

      <CardContent className="pt-4 space-y-6">
        {options.length === 0 ? (
          <p className="text-[var(--text-sm)] text-[var(--color-text-secondary)] italic">
            Remediation proposer agent is currently compiling mitigation strategies...
          </p>
        ) : (
          options.map((option, idx) => {
            const isBlocked = option.verdict === 'blocked_by_guardrail'
            const requiresApproval = option.verdict === 'requires_approval'

            return (
              <div
                key={option.id || idx}
                className="p-4 rounded-[var(--radius-md)] bg-[var(--color-surface-hover)]/40 border border-[var(--color-border-subtle)] space-y-4"
              >
                {/* Header: Title, Risk, Backend Verdict */}
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2.5">
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="font-mono text-[var(--text-xs)] text-[var(--color-text-tertiary)] font-semibold">
                        OPTION #{idx + 1}
                      </span>
                      <Badge
                        variant={
                          option.risk_level === 'high'
                            ? 'high'
                            : option.risk_level === 'medium'
                              ? 'medium'
                              : 'low'
                        }
                        size="sm"
                      >
                        {option.risk_level.toUpperCase()} RISK
                      </Badge>
                    </div>
                    <h4 className="text-[var(--text-sm)] font-semibold text-[var(--color-text-primary)] mt-1">
                      {option.title}
                    </h4>
                  </div>

                  <div className="shrink-0">{getVerdictBadge(option.verdict)}</div>
                </div>

                {/* Pre-flight & Post-flight Checks Grid */}
                <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-[var(--text-xs)]">
                  {/* Pre-flight Checks */}
                  <div className="p-3 rounded-[var(--radius-sm)] bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)]">
                    <span className="font-semibold text-[var(--color-text-primary)] flex items-center gap-1.5 mb-2">
                      <CheckCircle size={13} className="text-[var(--color-low)]" />
                      Pre-flight Validation Checks
                    </span>
                    <ul className="space-y-1 text-[var(--color-text-secondary)] list-disc list-inside">
                      {option.pre_flight_checks.map((check, cIdx) => (
                        <li key={cIdx}>{check}</li>
                      ))}
                    </ul>
                  </div>

                  {/* Post-flight Checks */}
                  <div className="p-3 rounded-[var(--radius-sm)] bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)]">
                    <span className="font-semibold text-[var(--color-text-primary)] flex items-center gap-1.5 mb-2">
                      <CheckCircle size={13} className="text-[var(--color-success)]" />
                      Post-flight Verification Checks
                    </span>
                    <ul className="space-y-1 text-[var(--color-text-secondary)] list-disc list-inside">
                      {option.post_flight_checks.map((check, cIdx) => (
                        <li key={cIdx}>{check}</li>
                      ))}
                    </ul>
                  </div>
                </div>

                {/* Execution Command in Geist Mono CodeBlock */}
                <div>
                  <span className="text-[var(--text-xs)] font-medium uppercase tracking-wider text-[var(--color-text-tertiary)] mb-1 block">
                    Execution Command Payload
                  </span>
                  <CodeBlock code={option.action_command} language="bash" />
                </div>

                {/* Rollback Description */}
                {option.rollback_description && (
                  <div className="flex items-start gap-2 p-2.5 rounded-[var(--radius-sm)] bg-[var(--color-surface-raised)] border border-[var(--color-border-subtle)] text-[var(--text-xs)]">
                    <RotateCcw
                      size={14}
                      className="text-[var(--color-text-tertiary)] shrink-0 mt-0.5"
                    />
                    <div>
                      <span className="font-semibold text-[var(--color-text-primary)]">
                        Rollback Strategy:{' '}
                      </span>
                      <span className="text-[var(--color-text-secondary)]">
                        {option.rollback_description}
                      </span>
                    </div>
                  </div>
                )}

                {/* Action CTA */}
                <div className="flex items-center justify-end gap-3 pt-2">
                  {isBlocked ? (
                    <span className="text-[var(--text-xs)] text-[var(--color-critical)] flex items-center gap-1 font-medium">
                      <XCircle size={13} />
                      Execution blocked by non-bypassable guardrail policy
                    </span>
                  ) : requiresApproval ? (
                    <Button
                      variant="primary"
                      size="sm"
                      onClick={() => {
                        void navigate('/approvals', { state: { incidentId } })
                      }}
                      className="gap-1.5"
                    >
                      <span>Review in Approval Queue</span>
                      <ArrowRight size={13} />
                    </Button>
                  ) : (
                    <Badge variant="success" size="sm">
                      Auto-executor queued
                    </Badge>
                  )}
                </div>
              </div>
            )
          })
        )}
      </CardContent>
    </Card>
  )
}
