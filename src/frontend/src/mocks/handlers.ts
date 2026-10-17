/**
 * Trinetra MSW Network Handlers
 *
 * Implements mock endpoints for all Doc 04 REST specifications and console workflows.
 */
import { http, HttpResponse } from 'msw'
import {
  SEED_INCIDENTS_DETAIL,
  SEED_APPROVALS,
  SEED_REMEDIATION_8901,
  SEED_AGENTS,
  SEED_RUNBOOKS,
  SEED_DASHBOARD_METRICS,
} from './seed-data'
import {
  type IncidentDetail,
  type IncidentSummary,
  type IncidentEvent,
  type ApprovalRequest,
  type Agent,
  type Runbook,
  CreateIncidentPayloadSchema,
  ApprovePayloadSchema,
  RejectPayloadSchema,
} from '../api/types'

// ============================================================================
// In-Memory Mutable State
// ============================================================================

let incidentsDb: Record<string, IncidentDetail> = JSON.parse(JSON.stringify(SEED_INCIDENTS_DETAIL))
let approvalsDb: ApprovalRequest[] = JSON.parse(JSON.stringify(SEED_APPROVALS))
let agentsDb: Agent[] = JSON.parse(JSON.stringify(SEED_AGENTS))
let runbooksDb: Runbook[] = JSON.parse(JSON.stringify(SEED_RUNBOOKS))

export function resetMockDatabase() {
  incidentsDb = JSON.parse(JSON.stringify(SEED_INCIDENTS_DETAIL))
  approvalsDb = JSON.parse(JSON.stringify(SEED_APPROVALS))
  agentsDb = JSON.parse(JSON.stringify(SEED_AGENTS))
  runbooksDb = JSON.parse(JSON.stringify(SEED_RUNBOOKS))
}

function errorResponse(status: number, code: string, message: string) {
  return HttpResponse.json(
    {
      error: {
        code,
        message,
        status,
        timestamp: new Date().toISOString(),
        request_id: `req-${Date.now()}`,
      },
    },
    { status },
  )
}

// ============================================================================
// MSW Handlers Definition
// ============================================================================

export const handlers = [
  // --------------------------------------------------------------------------
  // GET /v1/incidents
  // --------------------------------------------------------------------------
  http.get('*/v1/incidents', ({ request }) => {
    const url = new URL(request.url)
    const statusFilter = url.searchParams
      .get('status')
      ?.split(',')
      .map((s) => s.trim())
    const severityFilter = url.searchParams
      .get('severity')
      ?.split(',')
      .map((s) => s.trim())
    const serviceFilter = url.searchParams.get('service')?.trim().toLowerCase()
    const limit = parseInt(url.searchParams.get('limit') || '50', 10)
    const offset = parseInt(url.searchParams.get('offset') || '0', 10)

    let list: IncidentSummary[] = Object.values(incidentsDb).map((inc) => ({
      id: inc.id,
      alert_id: inc.alert_id,
      title: inc.title,
      severity: inc.severity,
      status: inc.status,
      affected_services: inc.affected_services,
      triggered_at: inc.triggered_at,
      root_cause: inc.root_cause,
      assigned_to: inc.assigned_to,
    }))

    if (statusFilter && statusFilter.length > 0) {
      list = list.filter((inc) => statusFilter.includes(inc.status))
    }

    if (severityFilter && severityFilter.length > 0) {
      list = list.filter((inc) => severityFilter.includes(inc.severity))
    }

    if (serviceFilter && serviceFilter.length > 0 && serviceFilter !== 'all') {
      list = list.filter((inc) =>
        inc.affected_services.some((s) => s.toLowerCase().includes(serviceFilter)),
      )
    }

    const total = list.length
    const paginated = list.slice(offset, offset + limit)

    return HttpResponse.json({
      incidents: paginated,
      total,
      limit,
      offset,
    })
  }),

  // --------------------------------------------------------------------------
  // POST /v1/incidents
  // --------------------------------------------------------------------------
  http.post('*/v1/incidents', async ({ request }) => {
    const rawBody: unknown = await request.json()
    const parsed = CreateIncidentPayloadSchema.safeParse(rawBody)

    if (!parsed.success) {
      return errorResponse(400, 'BAD_REQUEST', 'Missing required fields for incident creation')
    }

    const body = parsed.data
    const incidentId = `INC-${new Date().getFullYear()}-${Math.floor(1000 + Math.random() * 9000)}`
    const now = new Date().toISOString()

    const initialEvent: IncidentEvent = {
      id: `evt-${Date.now()}-01`,
      incident_id: incidentId,
      event_type: 'alert_ingested',
      agent_name: 'Alert Ingestion Service',
      action_description: `Alert ingested from source ${body.source}`,
      tool_call: 'webhook_ingest',
      execution_status: 'success',
      requires_approval: false,
      guardrail_triggered: false,
      details: body.description,
      timestamp: now,
    }

    const newIncident: IncidentDetail = {
      id: incidentId,
      alert_id: body.alert_id,
      title: body.title,
      severity: body.severity,
      status: 'triggered',
      affected_services: [body.affected_service],
      affected_hosts: [],
      root_cause: null,
      root_cause_confidence: null,
      root_cause_category: null,
      proposed_remediation: null,
      remediation_risk_level: null,
      triggered_at: now,
      started_investigating_at: null,
      resolved_at: null,
      assigned_to: null,
      events: [initialEvent],
    }

    incidentsDb[incidentId] = newIncident

    return HttpResponse.json(
      {
        incident_id: incidentId,
        alert_id: newIncident.alert_id,
        status: 'triggered',
        severity: body.severity,
        created_at: now,
      },
      { status: 201 },
    )
  }),

  // --------------------------------------------------------------------------
  // GET /v1/incidents/:id
  // --------------------------------------------------------------------------
  http.get('*/v1/incidents/:id', ({ params }) => {
    const id = String(params.id)
    const incident = incidentsDb[id]

    if (!incident) {
      return errorResponse(404, 'INCIDENT_NOT_FOUND', `Incident with ID '${id}' not found`)
    }

    return HttpResponse.json(incident)
  }),

  // --------------------------------------------------------------------------
  // GET /v1/incidents/:id/events
  // --------------------------------------------------------------------------
  http.get('*/v1/incidents/:id/events', ({ params, request }) => {
    const id = String(params.id)
    const incident = incidentsDb[id]

    if (!incident) {
      return errorResponse(404, 'INCIDENT_NOT_FOUND', `Incident with ID '${id}' not found`)
    }

    const url = new URL(request.url)
    const eventType = url.searchParams.get('event_type')
    const limit = parseInt(url.searchParams.get('limit') || '100', 10)
    const offset = parseInt(url.searchParams.get('offset') || '0', 10)

    let events = [...incident.events]
    if (eventType) {
      events = events.filter((e) => e.event_type === eventType)
    }

    const total = events.length
    const paginated = events.slice(offset, offset + limit)

    return HttpResponse.json({
      events: paginated,
      total,
      limit,
      offset,
    })
  }),

  // --------------------------------------------------------------------------
  // GET /v1/incidents/:id/remediation
  // --------------------------------------------------------------------------
  http.get('*/v1/incidents/:id/remediation', ({ params }) => {
    const id = String(params.id)
    const incident = incidentsDb[id]

    if (!incident) {
      return errorResponse(404, 'INCIDENT_NOT_FOUND', `Incident with ID '${id}' not found`)
    }

    if (id === 'INC-2024-8901') {
      return HttpResponse.json({
        incident_id: id,
        options: SEED_REMEDIATION_8901,
      })
    }

    // Default remediation response
    return HttpResponse.json({
      incident_id: id,
      options: [
        {
          id: `rem-${id}-01`,
          title: `Remediate ${incident.title}`,
          risk_level: incident.severity === 'critical' ? 'high' : 'low',
          verdict: incident.severity === 'critical' ? 'requires_approval' : 'allowed',
          action_command: `systemctl restart ${incident.affected_services[0] || 'service'}`,
          pre_flight_checks: ['Verify host resource headroom > 20%'],
          steps: ['Execute graceful process restart'],
          post_flight_checks: ['Verify process healthcheck returns 200 OK'],
          rollback_description: 'Revert to previous service configuration',
        },
      ],
    })
  }),

  // --------------------------------------------------------------------------
  // GET /v1/approvals/pending
  // --------------------------------------------------------------------------
  http.get('*/v1/approvals/pending', () => {
    const pending = approvalsDb.filter((a) => a.status === 'pending')
    return HttpResponse.json({
      approvals: pending,
      total: pending.length,
    })
  }),

  // --------------------------------------------------------------------------
  // POST /v1/approvals/:id/approve
  // --------------------------------------------------------------------------
  http.post('*/v1/approvals/:id/approve', async ({ params, request }) => {
    const id = String(params.id)
    const rawBody: unknown = await request.json()
    const parsed = ApprovePayloadSchema.safeParse(rawBody)

    if (!parsed.success) {
      return errorResponse(400, 'BAD_REQUEST', 'Invalid approval payload')
    }

    const body = parsed.data
    const approval = approvalsDb.find((a) => a.id === id)

    if (!approval) {
      return errorResponse(404, 'APPROVAL_NOT_FOUND', `Approval request '${id}' not found`)
    }

    if (approval.status !== 'pending') {
      return errorResponse(
        409,
        'APPROVAL_CONFLICT',
        `Approval request '${id}' is already in status '${approval.status}'`,
      )
    }

    approval.status = 'approved'
    const now = new Date().toISOString()

    // Update incident status if matching
    const incident = incidentsDb[approval.incident_id]
    if (incident) {
      incident.status = 'executing_remediation'
      incident.events.push({
        id: `evt-appr-${Date.now()}`,
        incident_id: incident.id,
        event_type: 'approval_granted',
        agent_name: 'Approval Gate',
        action_description: `Approved by user ${body.approved_by_user_id}. Notes: ${body.notes || 'None'}`,
        tool_call: 'grant_approval',
        execution_status: 'success',
        requires_approval: true,
        guardrail_triggered: false,
        timestamp: now,
      })
    }

    return HttpResponse.json({
      approval_id: id,
      status: 'approved',
      approved_at: now,
      remediation_will_execute_at: now,
    })
  }),

  // --------------------------------------------------------------------------
  // POST /v1/approvals/:id/reject & POST /v1/approvals/:id/deny
  // --------------------------------------------------------------------------
  http.post('*/v1/approvals/:id/reject', async ({ params, request }) => {
    const id = String(params.id)
    const rawBody: unknown = await request.json()
    const parsed = RejectPayloadSchema.safeParse(rawBody)

    if (!parsed.success) {
      return errorResponse(400, 'BAD_REQUEST', 'Invalid reject payload')
    }

    const body = parsed.data
    const approval = approvalsDb.find((a) => a.id === id)

    if (!approval) {
      return errorResponse(404, 'APPROVAL_NOT_FOUND', `Approval request '${id}' not found`)
    }

    if (approval.status !== 'pending') {
      return errorResponse(
        409,
        'APPROVAL_CONFLICT',
        `Approval request '${id}' is already in status '${approval.status}'`,
      )
    }

    approval.status = 'denied'
    const now = new Date().toISOString()

    // Update incident status to escalated per spec
    const incident = incidentsDb[approval.incident_id]
    if (incident) {
      incident.status = 'escalated'
      incident.events.push({
        id: `evt-deny-${Date.now()}`,
        incident_id: incident.id,
        event_type: 'approval_denied',
        agent_name: 'Approval Gate',
        action_description: `Remediation denied by ${body.denied_by_user_id}. Reason: ${body.reason}`,
        tool_call: 'deny_approval',
        execution_status: 'failure',
        requires_approval: true,
        guardrail_triggered: false,
        timestamp: now,
      })
    }

    return HttpResponse.json({
      approval_id: id,
      status: 'denied',
      denied_at: now,
      incident_status: 'escalated',
    })
  }),

  // Support /deny alias from doc 04 line 206
  http.post('*/v1/approvals/:id/deny', async ({ params, request }) => {
    const id = String(params.id)
    const rawBody: unknown = await request.json()
    const parsed = RejectPayloadSchema.safeParse(rawBody)

    if (!parsed.success) {
      return errorResponse(400, 'BAD_REQUEST', 'Invalid reject payload')
    }

    const body = parsed.data
    const approval = approvalsDb.find((a) => a.id === id)

    if (!approval) {
      return errorResponse(404, 'APPROVAL_NOT_FOUND', `Approval request '${id}' not found`)
    }

    approval.status = 'denied'
    const now = new Date().toISOString()

    const incident = incidentsDb[approval.incident_id]
    if (incident) {
      incident.status = 'escalated'
      incident.events.push({
        id: `evt-deny-${Date.now()}`,
        incident_id: incident.id,
        event_type: 'approval_denied',
        agent_name: 'Approval Gate',
        action_description: `Remediation denied by ${body.denied_by_user_id}. Reason: ${body.reason}`,
        tool_call: 'deny_approval',
        execution_status: 'failure',
        requires_approval: true,
        guardrail_triggered: false,
        timestamp: now,
      })
    }

    return HttpResponse.json({
      approval_id: id,
      status: 'denied',
      denied_at: now,
      incident_status: 'escalated',
    })
  }),

  // --------------------------------------------------------------------------
  // GET /v1/agents
  // --------------------------------------------------------------------------
  http.get('*/v1/agents', () => {
    return HttpResponse.json({
      agents: agentsDb,
      total: agentsDb.length,
    })
  }),

  // --------------------------------------------------------------------------
  // GET /v1/runbooks
  // --------------------------------------------------------------------------
  http.get('*/v1/runbooks', ({ request }) => {
    const url = new URL(request.url)
    const search = url.searchParams.get('search')?.toLowerCase()
    const tags = url.searchParams
      .get('tags')
      ?.split(',')
      .map((t) => t.trim().toLowerCase())
    const services = url.searchParams
      .get('services')
      ?.split(',')
      .map((s) => s.trim().toLowerCase())
    const limit = parseInt(url.searchParams.get('limit') || '20', 10)
    const offset = parseInt(url.searchParams.get('offset') || '0', 10)

    let filtered = [...runbooksDb]

    if (search) {
      filtered = filtered.filter(
        (rb) =>
          rb.title.toLowerCase().includes(search) ||
          rb.description?.toLowerCase().includes(search) ||
          rb.content.toLowerCase().includes(search),
      )
    }

    if (tags && tags.length > 0) {
      filtered = filtered.filter((rb) =>
        tags.some((tag) => rb.tags.map((t) => t.toLowerCase()).includes(tag)),
      )
    }

    if (services && services.length > 0) {
      filtered = filtered.filter((rb) =>
        services.some((svc) => rb.applicable_services.map((s) => s.toLowerCase()).includes(svc)),
      )
    }

    const total = filtered.length
    const paginated = filtered.slice(offset, offset + limit)

    return HttpResponse.json({
      runbooks: paginated,
      total,
      limit,
      offset,
    })
  }),

  // --------------------------------------------------------------------------
  // GET /v1/runbooks/:id
  // --------------------------------------------------------------------------
  http.get('*/v1/runbooks/:id', ({ params }) => {
    const id = String(params.id)
    const runbook = runbooksDb.find((rb) => rb.id === id)

    if (!runbook) {
      return errorResponse(404, 'RUNBOOK_NOT_FOUND', `Runbook with ID '${id}' not found`)
    }

    return HttpResponse.json(runbook)
  }),

  // --------------------------------------------------------------------------
  // GET /v1/dashboard/metrics
  // --------------------------------------------------------------------------
  http.get('*/v1/dashboard/metrics', () => {
    return HttpResponse.json(SEED_DASHBOARD_METRICS)
  }),
]
