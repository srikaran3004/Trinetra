import { useState, useId } from 'react'
import { motion, useReducedMotion } from 'motion/react'
import {
  ShieldAlert,
  Lock,
  RotateCcw,
  ExternalLink,
  CheckCircle2,
  XCircle,
  AlertOctagon,
} from 'lucide-react'
import { Card } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Countdown } from '@/components/ui/countdown'
import { CodeBlock } from '@/components/ui/code-block'
import { Dialog } from '@/components/ui/dialog'
import { TextArea } from '@/components/ui/text-area'
import { InlineAlert } from '@/components/ui/inline-alert'
import type { ApprovalRequest } from '@/api/types'
import { useAuth } from '@/auth/auth-context'
import { canUserActOnApproval } from '@/hooks/use-approvals'

export interface ApprovalCardProps {
  approval: ApprovalRequest
  onApprove: (approvalId: string, notes?: string) => Promise<unknown>
  onDeny: (approvalId: string, reason: string) => Promise<unknown>
  isApproving?: boolean
  isDenying?: boolean
  errorMessage?: string | null
  onClearError?: () => void
  className?: string
}

export function ApprovalCard({
  approval,
  onApprove,
  onDeny,
  isApproving = false,
  isDenying = false,
  errorMessage,
  onClearError,
  className,
}: ApprovalCardProps) {
  const { user } = useAuth()
  const prefersReducedMotion = useReducedMotion()
  const denyReasonId = useId()

  const [isApproveOpen, setIsApproveOpen] = useState(false)
  const [isDenyOpen, setIsDenyOpen] = useState(false)
  const [denyReason, setDenyReason] = useState('')
  const [isSuccessAnimated, setIsSuccessAnimated] = useState(false)

  const isBlockedByGuardrail = approval.verdict === 'blocked_by_guardrail'
  const isApproved = approval.status === 'approved' || isSuccessAnimated
  const isDenied = approval.status === 'denied'
  const isPending = approval.status === 'pending' && !isApproved && !isDenied

  // Role verification per Brief §7.6
  const roleCheck = canUserActOnApproval(user?.role, approval.approval_level)
  const canAct = roleCheck.allowed && isPending

  // Handle Approve action
  const handleConfirmApprove = async () => {
    setIsApproveOpen(false)
    setIsSuccessAnimated(true)
    try {
      await onApprove(approval.id)
    } catch {
      setIsSuccessAnimated(false)
    }
  }

  // Handle Deny action
  const handleConfirmDeny = async () => {
    if (!denyReason.trim()) return
    setIsDenyOpen(false)
    await onDeny(approval.id, denyReason)
  }

  // Risk level badge mapping
  const riskBadgeVariant =
    approval.risk_level === 'high' ? 'critical' : approval.risk_level === 'medium' ? 'high' : 'low'

  return (
    <motion.div
      layout={!prefersReducedMotion}
      initial={false}
      animate={
        isApproved && !prefersReducedMotion
          ? {
              opacity: [1, 1, 0.4],
              scale: [1, 1, 0.98],
            }
          : { opacity: 1, scale: 1 }
      }
      transition={{ duration: 0.3 }}
    >
      <Card
        className={`p-[var(--space-5)] relative overflow-hidden flex flex-col gap-[var(--space-4)] border-[var(--color-border-subtle)] ${
          isBlockedByGuardrail
            ? 'border-[color-mix(in_srgb,var(--color-critical)_40%,transparent)] bg-[color-mix(in_srgb,var(--color-critical)_4%,transparent)]'
            : ''
        } ${className || ''}`}
        data-testid={`approval-card-${approval.id}`}
        data-approval-id={approval.id}
      >
        {/* Success Animation Banner (Brief §8: Checkmark path draws in 300ms) */}
        {isApproved && (
          <motion.div
            initial={{ opacity: 0, y: -8 }}
            animate={{ opacity: 1, y: 0 }}
            className="flex items-center gap-3 p-3 bg-[color-mix(in_srgb,var(--color-success)_15%,transparent)] border border-[color-mix(in_srgb,var(--color-success)_30%,transparent)] rounded-[var(--radius-sm)] text-[var(--color-success)]"
            data-testid="approval-success-banner"
          >
            {prefersReducedMotion ? (
              <CheckCircle2 size={24} className="shrink-0" />
            ) : (
              <svg
                viewBox="0 0 24 24"
                className="w-6 h-6 shrink-0"
                fill="none"
                stroke="currentColor"
                strokeWidth="2.5"
                strokeLinecap="round"
                strokeLinejoin="round"
              >
                <motion.path
                  d="M5 13l4 4L19 7"
                  initial={{ pathLength: 0 }}
                  animate={{ pathLength: 1 }}
                  transition={{ duration: 0.3, ease: 'easeOut' }}
                />
              </svg>
            )}
            <div className="flex flex-col">
              <span className="font-semibold text-sm">Remediation Approved</span>
              <span className="text-xs opacity-90">
                Command dispatched to Auto-Executor sandbox for verified execution.
              </span>
            </div>
          </motion.div>
        )}

        {/* Denied Banner */}
        {isDenied && (
          <div
            className="flex items-center gap-3 p-3 bg-[color-mix(in_srgb,var(--color-critical)_15%,transparent)] border border-[color-mix(in_srgb,var(--color-critical)_30%,transparent)] rounded-[var(--radius-sm)] text-[var(--color-critical)]"
            data-testid="approval-denied-banner"
          >
            <XCircle size={20} className="shrink-0" />
            <div className="flex flex-col">
              <span className="font-semibold text-sm">Remediation Denied</span>
              <span className="text-xs opacity-90">
                Incident escalated to human commander. Autonomous remediation halted.
              </span>
            </div>
          </div>
        )}

        {/* Inline Rollback Error Alert */}
        {errorMessage && (
          <InlineAlert
            variant="critical"
            title="Optimistic Action Failed"
            className="animate-in fade-in"
            onDismiss={onClearError}
          >
            {errorMessage} Approval state was reverted to pending.
          </InlineAlert>
        )}

        {/* Header: Incident link, Title, Risk & Approver Level, and Countdown */}
        <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-3 pb-3 border-b border-[var(--color-border-subtle)]">
          <div className="flex flex-col gap-1">
            <div className="flex items-center gap-2 flex-wrap">
              <a
                href={`/incidents/${approval.incident_id}`}
                className="inline-flex items-center gap-1 font-mono text-xs font-semibold text-[var(--color-accent)] hover:underline"
                title={`View incident ${approval.incident_id}`}
              >
                {approval.incident_id}
                <ExternalLink size={12} strokeWidth={1.5} />
              </a>

              <Badge variant={riskBadgeVariant} size="sm">
                Risk: {approval.risk_level.toUpperCase()}
              </Badge>

              <Badge variant="outline" size="sm" className="capitalize">
                Required: {approval.approval_level}
              </Badge>

              {isBlockedByGuardrail && (
                <Badge variant="critical" size="sm" className="font-semibold gap-1">
                  <ShieldAlert size={12} />
                  GUARDRAIL BLOCKED
                </Badge>
              )}
            </div>

            <h3 className="text-sm font-semibold text-[var(--color-text-primary)] mt-1">
              {approval.action}
            </h3>
          </div>

          {/* 15-Minute Countdown Timer (Brief §7.6) */}
          {isPending && (
            <div className="flex items-center gap-2 self-start sm:self-center shrink-0">
              <span className="text-xs text-[var(--color-text-tertiary)]">Timeout:</span>
              <Countdown targetDate={new Date(approval.timeout_at)} className="text-xs" />
            </div>
          )}
        </div>

        {/* Guardrail Violation Warning Verdict */}
        {isBlockedByGuardrail && (
          <div
            className="flex items-start gap-2.5 p-3 rounded-[var(--radius-sm)] bg-[color-mix(in_srgb,var(--color-critical)_12%,transparent)] border border-[color-mix(in_srgb,var(--color-critical)_25%,transparent)] text-[var(--color-critical)]"
            data-testid="guardrail-blocked-verdict"
          >
            <AlertOctagon size={16} strokeWidth={2} className="shrink-0 mt-0.5" />
            <div className="text-xs leading-relaxed">
              <strong className="font-semibold">Guardrail Verdict — Command Blocked: </strong>
              This action contains destructive patterns intercepted by the Non-Bypassable Guardrails
              Engine (Rule RG-01/RG-04). In accordance with Trinetra safety invariants, this action{' '}
              <strong>cannot be approved or executed</strong>.
            </div>
          </div>
        )}

        {/* Notes / Context */}
        {approval.notes && (
          <p className="text-xs text-[var(--color-text-secondary)] leading-relaxed m-0">
            {approval.notes}
          </p>
        )}

        {/* Command Block in Geist Mono */}
        <div className="flex flex-col gap-1.5">
          <span className="text-xs font-medium text-[var(--color-text-tertiary)]">
            Proposed Command:
          </span>
          <CodeBlock code={approval.command} language="bash" className="text-xs" />
        </div>

        {/* Rollback Description */}
        {approval.rollback_description && (
          <div className="flex items-start gap-2 p-2.5 rounded-[var(--radius-sm)] bg-[var(--color-surface-hover)] border border-[var(--color-border-subtle)] text-xs text-[var(--color-text-secondary)]">
            <RotateCcw
              size={14}
              strokeWidth={1.5}
              className="shrink-0 text-[var(--color-text-tertiary)] mt-0.5"
            />
            <div className="leading-relaxed">
              <span className="font-semibold text-[var(--color-text-primary)]">
                Rollback Plan:{' '}
              </span>
              {approval.rollback_description}
            </div>
          </div>
        )}

        {/* Actions and Authorization Footer */}
        {isPending && (
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pt-3 border-t border-[var(--color-border-subtle)] mt-1">
            {/* Role Check Message (Brief §7.6: "If current user's role cannot act, buttons are disabled and reason is shown next to them") */}
            <div className="text-xs text-[var(--color-text-tertiary)] flex items-center gap-1.5 min-h-[24px]">
              {!roleCheck.allowed ? (
                <span className="inline-flex items-center gap-1.5 text-[var(--color-high)] font-medium">
                  <Lock size={13} strokeWidth={1.5} className="shrink-0" />
                  {roleCheck.reason}
                </span>
              ) : isBlockedByGuardrail ? (
                <span className="inline-flex items-center gap-1.5 text-[var(--color-critical)] font-medium">
                  <ShieldAlert size={13} strokeWidth={1.5} className="shrink-0" />
                  Approval disabled by guardrail policy
                </span>
              ) : (
                <span>Authenticated as {user?.role}</span>
              )}
            </div>

            {/* Action Buttons */}
            <div className="flex items-center gap-2.5 self-end sm:self-center shrink-0">
              <Button
                variant="secondary"
                size="sm"
                isDisabled={!canAct || isDenying}
                onPress={() => {
                  setDenyReason('')
                  setIsDenyOpen(true)
                }}
                data-testid="deny-button"
              >
                Deny
              </Button>

              <Button
                variant={approval.risk_level === 'high' ? 'danger' : 'primary'}
                size="sm"
                isDisabled={!canAct || isBlockedByGuardrail || isApproving}
                isLoading={isApproving}
                onPress={() => {
                  setIsApproveOpen(true)
                }}
                data-testid="approve-button"
              >
                {approval.risk_level === 'high' ? 'Approve (High Risk)' : 'Approve'}
              </Button>
            </div>
          </div>
        )}

        {/* Approve Confirmation Dialog (Brief §7.6: Typed confirmation for high risk) */}
        <Dialog
          isOpen={isApproveOpen}
          onOpenChange={setIsApproveOpen}
          title={
            approval.risk_level === 'high'
              ? 'Confirm High-Risk Remediation'
              : 'Confirm Remediation Execution'
          }
          description={`Approving this proposal will execute the remediation command immediately on the affected cluster.`}
          confirmLabel="Execute Command"
          confirmVariant={approval.risk_level === 'high' ? 'danger' : 'primary'}
          confirmPhrase={approval.risk_level === 'high' ? approval.incident_id : undefined}
          onConfirm={() => {
            void handleConfirmApprove()
          }}
          onCancel={() => {
            setIsApproveOpen(false)
          }}
        >
          <div className="flex flex-col gap-2 my-2">
            <div className="text-xs text-[var(--color-text-secondary)]">
              <strong>Target Action: </strong>
              {approval.action}
            </div>
            <CodeBlock code={approval.command} language="bash" className="text-xs" />
          </div>
        </Dialog>

        {/* Deny Dialog with Mandatory Reason (Brief §7.6: "Deny requires a reason") */}
        <Dialog
          isOpen={isDenyOpen}
          onOpenChange={setIsDenyOpen}
          title="Deny Remediation Proposal"
          description="Denying this remediation will immediately halt automated actions and escalate the incident to manual engineering response."
          confirmLabel="Confirm Denial"
          confirmVariant="danger"
          isConfirmDisabled={!denyReason.trim()}
          onConfirm={() => {
            void handleConfirmDeny()
          }}
          onCancel={() => {
            setIsDenyOpen(false)
          }}
        >
          <div className="flex flex-col gap-2 my-2">
            <TextArea
              id={denyReasonId}
              label="Reason for Denial (Required)"
              value={denyReason}
              onChange={setDenyReason}
              placeholder="e.g. Unverified secondary failover capacity during peak hours."
              rows={3}
              className="text-xs"
              aria-required="true"
            />
            {!denyReason.trim() && (
              <span className="text-[11px] text-[var(--color-critical)]">
                A reason must be provided before denying.
              </span>
            )}
          </div>
        </Dialog>
      </Card>
    </motion.div>
  )
}
