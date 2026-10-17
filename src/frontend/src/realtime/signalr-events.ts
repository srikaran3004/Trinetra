/**
 * Trinetra SignalR Realtime Event Contract
 *
 * Defines the real-time event contract between ASP.NET Core SignalR `IncidentHub`
 * (/hubs/incidents) and the frontend console.
 *
 * NOTE: As identified in Spec Conflict 6 / Brief §10, the SignalR hub is not
 * in Doc 04. This contract reconciles with backend `IncidentHub.cs` and
 * `SignalRIncidentEventNotifier.cs`.
 */
import { z } from 'zod'
import { IncidentSeveritySchema, IncidentStatusSchema } from '../api/types'

// ============================================================================
// Server-to-Client Event Schemas
// ============================================================================

/**
 * Dispatched when a new incident is ingested and triggered.
 */
export const IncidentCreatedPayloadSchema = z.object({
  id: z.string(),
  incidentId: z.string(),
  title: z.string(),
  severity: IncidentSeveritySchema,
  status: IncidentStatusSchema,
  triggeredAt: z.string(),
})
export type IncidentCreatedPayload = z.infer<typeof IncidentCreatedPayloadSchema>

/**
 * Dispatched when an incident's lifecycle status transitions.
 */
export const IncidentStatusChangedPayloadSchema = z.object({
  incidentId: z.string(),
  status: IncidentStatusSchema,
  updatedAt: z.string(),
})
export type IncidentStatusChangedPayload = z.infer<typeof IncidentStatusChangedPayloadSchema>

/**
 * Dispatched when an autonomous agent completes an investigation or remediation tool call.
 */
export const AgentActionCompletedPayloadSchema = z.object({
  incidentId: z.string(),
  agentName: z.string(),
  actionDescription: z.string(),
  toolCall: z.string().nullable().optional(),
  toolResult: z.string().nullable().optional(),
  status: z.string(),
  timestamp: z.string(),
})
export type AgentActionCompletedPayload = z.infer<typeof AgentActionCompletedPayloadSchema>

/**
 * Dispatched when a proposed remediation action requires human approval.
 */
export const ApprovalRequiredPayloadSchema = z.object({
  incidentId: z.string(),
  eventId: z.string(),
  actionDescription: z.string(),
  toolCall: z.string().nullable().optional(),
  timeoutMinutes: z.number().nullable().optional(),
  timestamp: z.string(),
})
export type ApprovalRequiredPayload = z.infer<typeof ApprovalRequiredPayloadSchema>

/**
 * Mapping of all SignalR Hub Event Names to their respective payloads.
 */
export interface SignalREventMap {
  IncidentCreated: IncidentCreatedPayload
  IncidentStatusChanged: IncidentStatusChangedPayload
  AgentActionCompleted: AgentActionCompletedPayload
  ApprovalRequired: ApprovalRequiredPayload
}

export type SignalREventName = keyof SignalREventMap

// ============================================================================
// Client-to-Server Hub Methods
// ============================================================================

export interface SignalRHubMethods {
  JoinIncidentGroup: (incidentId: string) => Promise<void>
  LeaveIncidentGroup: (incidentId: string) => Promise<void>
}
