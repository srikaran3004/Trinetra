/**
 * Trinetra API Schema & Types
 *
 * Strict Zod validation schemas for all Doc 04 REST endpoints
 * and console contracts (§10).
 */
import { z } from 'zod'

// ============================================================================
// Enums & Literals
// ============================================================================

export const IncidentSeveritySchema = z.enum(['critical', 'high', 'medium', 'low'])
export type IncidentSeverity = z.infer<typeof IncidentSeveritySchema>

export const IncidentStatusSchema = z.enum([
  'triggered',
  'investigating',
  'analysis_complete',
  'awaiting_approval',
  'executing_remediation',
  'monitoring_recovery',
  'resolved',
  'escalated',
])
export type IncidentStatus = z.infer<typeof IncidentStatusSchema>

export const RemediationRiskLevelSchema = z.enum(['low', 'medium', 'high'])
export type RemediationRiskLevel = z.infer<typeof RemediationRiskLevelSchema>

export const RemediationVerdictSchema = z.enum([
  'allowed',
  'requires_approval',
  'blocked_by_guardrail',
])
export type RemediationVerdict = z.infer<typeof RemediationVerdictSchema>

export const ApprovalStatusSchema = z.enum(['pending', 'approved', 'denied', 'timed_out'])
export type ApprovalStatus = z.infer<typeof ApprovalStatusSchema>

export const AgentStatusSchema = z.enum(['idle', 'working', 'waiting', 'failed'])
export type AgentStatus = z.infer<typeof AgentStatusSchema>

// ============================================================================
// Events & Audit Trail
// ============================================================================

export const IncidentEventSchema = z.object({
  id: z.string(),
  incident_id: z.string(),
  event_type: z.string(),
  agent_name: z.string(),
  action_description: z.string().optional(),
  tool_call: z.string().nullable().optional(),
  execution_status: z.enum(['success', 'failure', 'pending', 'blocked']).optional(),
  requires_approval: z.boolean().default(false),
  guardrail_triggered: z.boolean().default(false),
  details: z.string().optional(),
  timestamp: z.string(),
})
export type IncidentEvent = z.infer<typeof IncidentEventSchema>

export const IncidentEventsResponseSchema = z.object({
  events: z.array(IncidentEventSchema),
  total: z.number().int().nonnegative(),
  limit: z.number().int().positive().optional(),
  offset: z.number().int().nonnegative().optional(),
})
export type IncidentEventsResponse = z.infer<typeof IncidentEventsResponseSchema>

// ============================================================================
// Incident Models
// ============================================================================

export const IncidentSummarySchema = z.object({
  id: z.string(),
  alert_id: z.string().optional(),
  title: z.string(),
  severity: IncidentSeveritySchema,
  status: IncidentStatusSchema,
  affected_services: z.array(z.string()).default([]),
  triggered_at: z.string(),
  root_cause: z.string().nullable().optional(),
  assigned_to: z.string().nullable().optional(),
})
export type IncidentSummary = z.infer<typeof IncidentSummarySchema>

export const IncidentListResponseSchema = z.object({
  incidents: z.array(IncidentSummarySchema),
  total: z.number().int().nonnegative(),
  limit: z.number().int().positive().optional(),
  offset: z.number().int().nonnegative().optional(),
})
export type IncidentListResponse = z.infer<typeof IncidentListResponseSchema>

export const IncidentDetailSchema = z.object({
  id: z.string(),
  alert_id: z.string(),
  title: z.string(),
  severity: IncidentSeveritySchema,
  status: IncidentStatusSchema,
  affected_services: z.array(z.string()),
  affected_hosts: z.array(z.string()).default([]),
  root_cause: z.string().nullable(),
  root_cause_confidence: z.number().min(0).max(1).nullable().optional(),
  root_cause_category: z.string().nullable().optional(),
  proposed_remediation: z.string().nullable().optional(),
  remediation_risk_level: RemediationRiskLevelSchema.nullable().optional(),
  triggered_at: z.string(),
  started_investigating_at: z.string().nullable().optional(),
  resolved_at: z.string().nullable().optional(),
  assigned_to: z.string().nullable().optional(),
  events: z.array(IncidentEventSchema).default([]),
})
export type IncidentDetail = z.infer<typeof IncidentDetailSchema>

export const CreateIncidentPayloadSchema = z.object({
  alert_id: z.string().min(1, 'alert_id is required'),
  source: z.string().min(1, 'source is required'),
  title: z.string().min(1, 'title is required'),
  description: z.string().min(1, 'description is required'),
  severity: IncidentSeveritySchema,
  affected_service: z.string().min(1, 'affected_service is required'),
  alert_data: z.record(z.string(), z.unknown()).optional(),
})
export type CreateIncidentPayload = z.infer<typeof CreateIncidentPayloadSchema>

export const CreateIncidentResponseSchema = z.object({
  incident_id: z.string(),
  alert_id: z.string(),
  status: IncidentStatusSchema,
  severity: IncidentSeveritySchema,
  created_at: z.string(),
})
export type CreateIncidentResponse = z.infer<typeof CreateIncidentResponseSchema>

// ============================================================================
// Remediation
// ============================================================================

export const RemediationOptionSchema = z.object({
  id: z.string(),
  title: z.string(),
  risk_level: RemediationRiskLevelSchema,
  verdict: RemediationVerdictSchema,
  action_command: z.string(),
  pre_flight_checks: z.array(z.string()),
  steps: z.array(z.string()),
  post_flight_checks: z.array(z.string()),
  rollback_description: z.string(),
  approval_request_id: z.string().optional(),
})
export type RemediationOption = z.infer<typeof RemediationOptionSchema>

export const IncidentRemediationResponseSchema = z.object({
  incident_id: z.string(),
  options: z.array(RemediationOptionSchema),
})
export type IncidentRemediationResponse = z.infer<typeof IncidentRemediationResponseSchema>

// ============================================================================
// Approvals
// ============================================================================

export const ApprovalRequestSchema = z.object({
  id: z.string(),
  incident_id: z.string(),
  action: z.string(),
  command: z.string(),
  risk_level: RemediationRiskLevelSchema,
  approval_level: z.enum(['lead', 'operator', 'admin']),
  timeout_at: z.string(),
  created_at: z.string(),
  status: ApprovalStatusSchema,
  notes: z.string().optional(),
  rollback_description: z.string().optional(),
  verdict: RemediationVerdictSchema.optional(),
})
export type ApprovalRequest = z.infer<typeof ApprovalRequestSchema>

export const PendingApprovalsResponseSchema = z.object({
  approvals: z.array(ApprovalRequestSchema),
  total: z.number().int().nonnegative(),
})
export type PendingApprovalsResponse = z.infer<typeof PendingApprovalsResponseSchema>

export const ApprovePayloadSchema = z.object({
  approved_by_user_id: z.string(),
  notes: z.string().optional(),
  scheduled_for: z.string().optional(),
})
export type ApprovePayload = z.infer<typeof ApprovePayloadSchema>

export const ApproveResponseSchema = z.object({
  approval_id: z.string(),
  status: z.literal('approved'),
  approved_at: z.string(),
  remediation_will_execute_at: z.string().optional(),
})
export type ApproveResponse = z.infer<typeof ApproveResponseSchema>

export const RejectPayloadSchema = z.object({
  denied_by_user_id: z.string(),
  reason: z.string().min(1, 'Reason is required'),
})
export type RejectPayload = z.infer<typeof RejectPayloadSchema>

export const RejectResponseSchema = z.object({
  approval_id: z.string(),
  status: z.literal('denied'),
  denied_at: z.string(),
  incident_status: z.literal('escalated'),
})
export type RejectResponse = z.infer<typeof RejectResponseSchema>

// ============================================================================
// Agents
// ============================================================================

export const AgentSchema = z.object({
  id: z.string(),
  name: z.string(),
  status: AgentStatusSchema,
  current_tool_call: z.string().nullable().optional(),
  elapsed_time_seconds: z.number().nonnegative(),
  guardrail_violations_count: z.number().int().nonnegative(),
  last_active_at: z.string(),
  current_incident_id: z.string().nullable().optional(),
})
export type Agent = z.infer<typeof AgentSchema>

export const AgentsResponseSchema = z.object({
  agents: z.array(AgentSchema),
  total: z.number().int().nonnegative(),
})
export type AgentsResponse = z.infer<typeof AgentsResponseSchema>

// ============================================================================
// Runbooks
// ============================================================================

export const RunbookStepSchema = z.object({
  step: z.number().int().positive(),
  action: z.string(),
  command: z.string(),
})
export type RunbookStep = z.infer<typeof RunbookStepSchema>

export const RunbookSchema = z.object({
  id: z.string(),
  title: z.string(),
  category: z.string(),
  tags: z.array(z.string()),
  applicable_services: z.array(z.string()),
  estimated_time_minutes: z.number().positive(),
  is_active: z.boolean(),
  version: z.number().int().positive(),
  description: z.string().optional(),
  content: z.string(),
  remediation_steps: z.array(RunbookStepSchema).default([]),
})
export type Runbook = z.infer<typeof RunbookSchema>

export const RunbooksResponseSchema = z.object({
  runbooks: z.array(RunbookSchema),
  total: z.number().int().nonnegative(),
  limit: z.number().int().positive().optional(),
  offset: z.number().int().nonnegative().optional(),
})
export type RunbooksResponse = z.infer<typeof RunbooksResponseSchema>

// ============================================================================
// Dashboard & Analytics
// ============================================================================

export const DashboardMetricsSchema = z.object({
  incident_trends: z.object({
    labels: z.array(z.string()),
    data: z.array(z.number()),
  }),
  severity_distribution: z.object({
    critical: z.number().int().nonnegative(),
    high: z.number().int().nonnegative(),
    medium: z.number().int().nonnegative(),
    low: z.number().int().nonnegative(),
  }),
  resolution_time_distribution: z.object({
    under_5_min: z.number().int().nonnegative(),
    '5_to_15_min': z.number().int().nonnegative(),
    '15_to_30_min': z.number().int().nonnegative(),
    over_30_min: z.number().int().nonnegative(),
  }),
  mttr_trend: z
    .object({
      labels: z.array(z.string()),
      data: z.array(z.number()),
    })
    .optional(),
  summary: z
    .object({
      active_incidents: z.number().int().nonnegative(),
      critical_incidents: z.number().int().nonnegative(),
      avg_mttr_minutes: z.number(),
      auto_resolution_rate: z.number().min(0).max(1),
      incidents_this_week: z.number().int().nonnegative(),
      most_common_root_cause: z.string().optional(),
    })
    .optional(),
})
export type DashboardMetrics = z.infer<typeof DashboardMetricsSchema>

// ============================================================================
// Standard Error Response
// ============================================================================

export const ApiErrorDetailsSchema = z.object({
  code: z.string(),
  message: z.string(),
  status: z.number().int(),
  timestamp: z.string(),
  request_id: z.string().optional(),
})
export type ApiErrorDetails = z.infer<typeof ApiErrorDetailsSchema>

export const ApiErrorResponseSchema = z.object({
  error: ApiErrorDetailsSchema,
})
export type ApiErrorResponse = z.infer<typeof ApiErrorResponseSchema>
