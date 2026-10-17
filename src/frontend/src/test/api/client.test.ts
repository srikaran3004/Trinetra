/**
 * Unit Tests for Trinetra REST API Client & MSW Handlers (Task F2-3)
 *
 * Verifies all Doc 04 endpoints, Zod schema boundary validation,
 * seed data conformance (§10), and error handling.
 */
import { describe, it, expect } from 'vitest'
import { http, HttpResponse } from 'msw'
import { server } from '@/mocks/server'
import { apiClient, ApiClientError } from '@/api/client'
import {
  IncidentListResponseSchema,
  IncidentDetailSchema,
  IncidentEventsResponseSchema,
  AgentsResponseSchema,
  RunbooksResponseSchema,
  DashboardMetricsSchema,
} from '@/api/types'

describe('REST API Client & MSW Handlers (Task F2-3)', () => {
  // ==========================================================================
  // Incidents Endpoints
  // ==========================================================================

  describe('GET /v1/incidents', () => {
    it('fetches all incidents conforming to IncidentListResponseSchema', async () => {
      const result = await apiClient.getIncidents()

      expect(result).toBeDefined()
      expect(result.incidents.length).toBeGreaterThanOrEqual(3)
      expect(IncidentListResponseSchema.safeParse(result).success).toBe(true)

      const ids = result.incidents.map((i) => i.id)
      expect(ids).toContain('INC-2024-8901')
      expect(ids).toContain('INC-2024-8902')
      expect(ids).toContain('INC-2024-8899')
    })

    it('filters incidents by status', async () => {
      const result = await apiClient.getIncidents({ status: ['awaiting_approval'] })

      expect(result.incidents.length).toBe(1)
      expect(result.incidents[0]?.id).toBe('INC-2024-8901')
      expect(result.incidents[0]?.status).toBe('awaiting_approval')
    })

    it('filters incidents by severity', async () => {
      const result = await apiClient.getIncidents({ severity: ['critical'] })

      expect(result.incidents.length).toBe(1)
      expect(result.incidents[0]?.id).toBe('INC-2024-8901')
      expect(result.incidents[0]?.severity).toBe('critical')
    })

    it('handles pagination with limit and offset', async () => {
      const page1 = await apiClient.getIncidents({ limit: 2, offset: 0 })
      const page2 = await apiClient.getIncidents({ limit: 2, offset: 2 })

      expect(page1.incidents).toHaveLength(2)
      expect(page2.incidents).toHaveLength(2)
      expect(page1.incidents[0]?.id).not.toBe(page2.incidents[0]?.id)
    })
  })

  describe('POST /v1/incidents', () => {
    it('creates a new incident and returns 201 Created', async () => {
      const newIncident = await apiClient.createIncident({
        alert_id: 'alert-test-cpu-999',
        source: 'prometheus',
        title: 'High CPU in ingress-proxy',
        description: 'CPU threshold exceeded 90%',
        severity: 'high',
        affected_service: 'ingress-proxy',
      })

      expect(newIncident.incident_id).toMatch(/^INC-\d{4}-\d{4}$/)
      expect(newIncident.status).toBe('triggered')
      expect(newIncident.severity).toBe('high')

      // Verify incident is now retrievable
      const detail = await apiClient.getIncidentById(newIncident.incident_id)
      expect(detail.title).toBe('High CPU in ingress-proxy')
      expect(detail.events).toHaveLength(1)
      expect(detail.events[0]?.event_type).toBe('alert_ingested')
    })

    it('throws ApiClientError 400 when missing required fields', async () => {
      await expect(
        apiClient.createIncident({
          alert_id: 'bad-alert',
          source: 'test',
          title: '', // Empty title
          description: 'bad',
          severity: 'high',
          affected_service: '',
        }),
      ).rejects.toThrow(ApiClientError)
    })
  })

  describe('GET /v1/incidents/:id', () => {
    it('returns full incident detail for critical incident INC-2024-8901', async () => {
      const incident = await apiClient.getIncidentById('INC-2024-8901')

      expect(incident.id).toBe('INC-2024-8901')
      expect(incident.severity).toBe('critical')
      expect(incident.status).toBe('awaiting_approval')
      expect(incident.root_cause).toContain('exhaustion')
      expect(incident.root_cause_confidence).toBe(0.92)
      expect(incident.root_cause_confidence).toBeGreaterThan(0.8) // Confidence threshold §7.4
      expect(incident.affected_services).toContain('auth-api')
      expect(IncidentDetailSchema.safeParse(incident).success).toBe(true)
    })

    it('returns escalated incident INC-2024-8902', async () => {
      const incident = await apiClient.getIncidentById('INC-2024-8902')

      expect(incident.id).toBe('INC-2024-8902')
      expect(incident.status).toBe('escalated')
      expect(incident.affected_services).toContain('payment-gateway')
    })

    it('returns resolved incident INC-2024-8899', async () => {
      const incident = await apiClient.getIncidentById('INC-2024-8899')

      expect(incident.id).toBe('INC-2024-8899')
      expect(incident.status).toBe('resolved')
      expect(incident.resolved_at).not.toBeNull()
    })

    it('throws 404 ApiClientError for non-existent incident', async () => {
      try {
        await apiClient.getIncidentById('INC-NON-EXISTENT')
        expect.unreachable('Should have thrown 404')
      } catch (err) {
        expect(err).toBeInstanceOf(ApiClientError)
        const apiErr = err as ApiClientError
        expect(apiErr.status).toBe(404)
        expect(apiErr.code).toBe('INCIDENT_NOT_FOUND')
      }
    })
  })

  // ==========================================================================
  // Events & Guardrail Audit Trail
  // ==========================================================================

  describe('GET /v1/incidents/:id/events', () => {
    it('retrieves incident audit trail events', async () => {
      const eventsRes = await apiClient.getIncidentEvents('INC-2024-8901')

      expect(eventsRes.events.length).toBeGreaterThan(0)
      expect(IncidentEventsResponseSchema.safeParse(eventsRes).success).toBe(true)
      expect(eventsRes.events[0]?.agent_name).toBeDefined()
    })

    it('includes guardrail-triggered timeline entry in INC-2024-8899 (§10)', async () => {
      const eventsRes = await apiClient.getIncidentEvents('INC-2024-8899')

      const guardrailEvent = eventsRes.events.find((e) => e.guardrail_triggered)
      expect(guardrailEvent).toBeDefined()
      expect(guardrailEvent?.agent_name).toBe('Guardrails Engine')
      expect(guardrailEvent?.execution_status).toBe('blocked')
      expect(guardrailEvent?.details).toContain('Non-Bypassable Policy')
      expect(guardrailEvent?.details).toContain('rm -rf')
    })
  })

  // ==========================================================================
  // Remediation
  // ==========================================================================

  describe('GET /v1/incidents/:id/remediation', () => {
    it('returns remediation options for INC-2024-8901 with pre/post checks and rollback', async () => {
      const remediation = await apiClient.getIncidentRemediation('INC-2024-8901')

      expect(remediation.incident_id).toBe('INC-2024-8901')
      expect(remediation.options.length).toBeGreaterThanOrEqual(1)

      const primary = remediation.options[0]
      expect(primary).toBeDefined()
      expect(primary?.verdict).toBe('requires_approval')
      expect(primary?.pre_flight_checks.length).toBeGreaterThan(0)
      expect(primary?.steps.length).toBeGreaterThan(0)
      expect(primary?.post_flight_checks.length).toBeGreaterThan(0)
      expect(primary?.rollback_description).toBeDefined()
      expect(primary?.approval_request_id).toBe('appr-8901')
    })

    it('shows blocked verdict for destructive remediation options (§7.4)', async () => {
      const remediation = await apiClient.getIncidentRemediation('INC-2024-8901')
      const blockedOption = remediation.options.find((o) => o.verdict === 'blocked_by_guardrail')

      expect(blockedOption).toBeDefined()
      expect(blockedOption?.title).toContain('BLOCKED')
    })
  })

  // ==========================================================================
  // Approvals Workflow
  // ==========================================================================

  describe('Approvals Workflow', () => {
    it('fetches pending approvals', async () => {
      const pending = await apiClient.getPendingApprovals()

      expect(pending.approvals.length).toBeGreaterThan(0)
      const target = pending.approvals.find((a) => a.id === 'appr-8901')
      expect(target).toBeDefined()
      expect(target?.status).toBe('pending')
      expect(target?.command).toContain('pg_terminate_backend')
    })

    it('approves remediation and transitions incident to executing_remediation', async () => {
      const res = await apiClient.approveRemediation('appr-8901', {
        approved_by_user_id: 'sre-lead@trinetra.internal',
        notes: 'Approved after verification',
      })

      expect(res.approval_id).toBe('appr-8901')
      expect(res.status).toBe('approved')

      // Verify incident status updated
      const updatedIncident = await apiClient.getIncidentById('INC-2024-8901')
      expect(updatedIncident.status).toBe('executing_remediation')

      // Verify approval event appended to audit trail
      const eventsRes = await apiClient.getIncidentEvents('INC-2024-8901')
      const approvalEvent = eventsRes.events.find((e) => e.event_type === 'approval_granted')
      expect(approvalEvent).toBeDefined()
    })

    it('rejects approval and transitions incident to escalated', async () => {
      // Reject existing pending approval appr-8901
      const res = await apiClient.rejectRemediation('appr-8901', {
        denied_by_user_id: 'sre-lead@trinetra.internal',
        reason: 'Too dangerous to run right now',
      })

      expect(res.approval_id).toBe('appr-8901')
      expect(res.status).toBe('denied')
      expect(res.incident_status).toBe('escalated')

      const updatedIncident = await apiClient.getIncidentById('INC-2024-8901')
      expect(updatedIncident.status).toBe('escalated')
    })

    it('throws 409 Conflict when attempting to approve an already decided request', async () => {
      // Approve once
      await apiClient.approveRemediation('appr-8901', {
        approved_by_user_id: 'sre-lead@trinetra.internal',
      })

      // Attempt to approve again
      await expect(
        apiClient.approveRemediation('appr-8901', {
          approved_by_user_id: 'sre-lead@trinetra.internal',
        }),
      ).rejects.toThrow(ApiClientError)
    })
  })

  // ==========================================================================
  // Agents
  // ==========================================================================

  describe('GET /v1/agents', () => {
    it('returns all 8 autonomous agents conforming to AgentsResponseSchema', async () => {
      const result = await apiClient.getAgents()

      expect(result.agents).toHaveLength(8)
      expect(AgentsResponseSchema.safeParse(result).success).toBe(true)

      const names = result.agents.map((a) => a.name)
      expect(names).toContain('Incident Coordinator')
      expect(names).toContain('Log Analyzer')
      expect(names).toContain('Metrics Querier')
      expect(names).toContain('Runbook Retriever')
      expect(names).toContain('Root Cause Analyzer')
      expect(names).toContain('Remediation Proposer')
      expect(names).toContain('Auto-Executor')
      expect(names).toContain('Audit Logger')

      // Verify Auto-Executor has 1 guardrail violation recorded from INC-2024-8899
      const autoExecutor = result.agents.find((a) => a.name === 'Auto-Executor')
      expect(autoExecutor?.guardrail_violations_count).toBe(1)
    })
  })

  // ==========================================================================
  // Runbooks
  // ==========================================================================

  describe('GET /v1/runbooks', () => {
    it('lists all active runbooks conforming to RunbooksResponseSchema', async () => {
      const result = await apiClient.getRunbooks()

      expect(result.runbooks.length).toBeGreaterThanOrEqual(3)
      expect(RunbooksResponseSchema.safeParse(result).success).toBe(true)
    })

    it('searches runbooks by keyword query', async () => {
      const result = await apiClient.getRunbooks({ search: 'connection pool' })

      expect(result.runbooks.length).toBe(1)
      expect(result.runbooks[0]?.id).toBe('runbook-db-pool-01')
      expect(result.runbooks[0]?.remediation_steps.length).toBeGreaterThan(0)
    })

    it('fetches single runbook by ID', async () => {
      const runbook = await apiClient.getRunbookById('runbook-db-pool-01')

      expect(runbook.id).toBe('runbook-db-pool-01')
      expect(runbook.title).toContain('Database Connection Pool')
      expect(runbook.content).toContain('## Overview')
    })
  })

  // ==========================================================================
  // Dashboard & Metrics
  // ==========================================================================

  describe('GET /v1/dashboard/metrics', () => {
    it('returns metrics conforming to DashboardMetricsSchema', async () => {
      const metrics = await apiClient.getDashboardMetrics()

      expect(DashboardMetricsSchema.safeParse(metrics).success).toBe(true)
      expect(metrics.incident_trends.labels.length).toBe(7)
      expect(metrics.severity_distribution.critical).toBeGreaterThanOrEqual(1)
      expect(metrics.summary?.avg_mttr_minutes).toBe(11.8)
    })
  })

  // ==========================================================================
  // Boundary Schema Validation Error Gate
  // ==========================================================================

  describe('Boundary Schema Validation Gate (§10)', () => {
    it('throws ApiClientError with SCHEMA_VALIDATION_ERROR if backend returns invalid schema', async () => {
      // Temporarily mock an endpoint returning corrupt/invalid shape
      server.use(
        http.get('*/v1/incidents/corrupt-test', () => {
          return HttpResponse.json({
            id: 'corrupt-test',
            // Missing required title, severity, status, etc.
          })
        }),
      )

      await expect(apiClient.getIncidentById('corrupt-test')).rejects.toThrow(
        /API schema validation failed/,
      )
    })
  })
})
