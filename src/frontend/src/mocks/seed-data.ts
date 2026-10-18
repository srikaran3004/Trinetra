/**
 * Trinetra Seed Data
 *
 * Strict fixture data complying with Brief §10 and Doc 04 specifications:
 * - One critical incident awaiting approval (INC-2024-8901: database connection pool exhaustion)
 * - One escalated incident (INC-2024-8902: payment service timeout)
 * - One resolved incident with a guardrail-triggered timeline entry (INC-2024-8899)
 * - Short incident history for trends and charts
 * - 8 autonomous agents state
 * - Production runbooks with sanitized markdown
 * - Dashboard metrics
 */
import type {
  IncidentDetail,
  IncidentSummary,
  IncidentEvent,
  RemediationOption,
  ApprovalRequest,
  Agent,
  Runbook,
  DashboardMetrics,
} from '../api/types'

// ============================================================================
// Incident Events Seed Data
// ============================================================================

export const SEED_EVENTS_8901: IncidentEvent[] = [
  {
    id: 'evt-8901-01',
    incident_id: 'INC-2024-8901',
    event_type: 'alert_ingested',
    agent_name: 'Alert Ingestion Service',
    action_description: 'Received Prometheus alert HighErrorRate from service auth-api',
    tool_call: 'webhook_ingest',
    execution_status: 'success',
    requires_approval: false,
    guardrail_triggered: false,
    details: 'Error rate at 18.4% (threshold 5.0%), HTTP 500 response spike on /v1/auth/token',
    timestamp: '2026-10-17T16:30:00Z',
  },
  {
    id: 'evt-8901-02',
    incident_id: 'INC-2024-8901',
    event_type: 'analysis_started',
    agent_name: 'Incident Coordinator',
    action_description: 'Fanning out parallel investigation to Log, Metrics, and Runbook agents',
    tool_call: 'fan_out_investigation',
    execution_status: 'success',
    requires_approval: false,
    guardrail_triggered: false,
    details: 'Dispatched task ID stream-8901 to Redis Stream agent-queue',
    timestamp: '2026-10-17T16:30:15Z',
  },
  {
    id: 'evt-8901-03',
    incident_id: 'INC-2024-8901',
    event_type: 'metrics_analyzed',
    agent_name: 'Metrics Querier',
    action_description: 'Queried Prometheus pg_stat_activity connection saturation',
    tool_call: 'query_metrics',
    execution_status: 'success',
    requires_approval: false,
    guardrail_triggered: false,
    details:
      'pg_stat_activity active connections: 99/100 (99% saturation). Idle in transaction: 42 connections.',
    timestamp: '2026-10-17T16:31:02Z',
  },
  {
    id: 'evt-8901-04',
    incident_id: 'INC-2024-8901',
    event_type: 'logs_analyzed',
    agent_name: 'Log Analyzer',
    action_description: 'Queried Elasticsearch for auth-api stack traces',
    tool_call: 'query_logs',
    execution_status: 'success',
    requires_approval: false,
    guardrail_triggered: false,
    details:
      'Found 412 Npgsql.NpgsqlException: The connection pool has reached its maximum size of 100.',
    timestamp: '2026-10-17T16:31:25Z',
  },
  {
    id: 'evt-8901-05',
    incident_id: 'INC-2024-8901',
    event_type: 'runbook_retrieved',
    agent_name: 'Runbook Retriever',
    action_description: 'Semantic vector search (pgvector) for connection pool exhaustion recovery',
    tool_call: 'search_runbooks_by_embedding',
    execution_status: 'success',
    requires_approval: false,
    guardrail_triggered: false,
    details: 'Matched runbook RB-POSTGRES-POOL-01 (cosine similarity 0.94)',
    timestamp: '2026-10-17T16:31:40Z',
  },
  {
    id: 'evt-8901-06',
    incident_id: 'INC-2024-8901',
    event_type: 'root_cause_classified',
    agent_name: 'Root Cause Analyzer',
    action_description:
      'Correlated evidence: 42 leaked idle transactions holding database pool connections',
    tool_call: 'classify_root_cause',
    execution_status: 'success',
    requires_approval: false,
    guardrail_triggered: false,
    details: 'Classified: database exhaustion with confidence score 0.92 (threshold 0.80 passed)',
    timestamp: '2026-10-17T16:32:10Z',
  },
  {
    id: 'evt-8901-07',
    incident_id: 'INC-2024-8901',
    event_type: 'remediation_proposed',
    agent_name: 'Remediation Proposer',
    action_description:
      'High-risk remediation proposed: terminate idle transactions and recycle pool',
    tool_call: 'generate_remediation_options',
    execution_status: 'pending',
    requires_approval: true,
    guardrail_triggered: false,
    details: 'Approval requested: appr-8901 (requires SRE Lead role)',
    timestamp: '2026-10-17T16:32:30Z',
  },
]

export const SEED_EVENTS_8902: IncidentEvent[] = [
  {
    id: 'evt-8902-01',
    incident_id: 'INC-2024-8902',
    event_type: 'alert_ingested',
    agent_name: 'Alert Ingestion Service',
    action_description: 'Received DataDog alert PaymentGatewayTimeout from payment-gateway',
    tool_call: 'webhook_ingest',
    execution_status: 'success',
    requires_approval: false,
    guardrail_triggered: false,
    details:
      'P99 latency surged to 8,400ms (SLA 800ms); upstream bank gateway returning 504 Gateway Timeout',
    timestamp: '2026-10-17T15:10:00Z',
  },
  {
    id: 'evt-8902-02',
    incident_id: 'INC-2024-8902',
    event_type: 'analysis_started',
    agent_name: 'Incident Coordinator',
    action_description: 'Parallel diagnostic fan-out',
    tool_call: 'fan_out_investigation',
    execution_status: 'success',
    requires_approval: false,
    guardrail_triggered: false,
    timestamp: '2026-10-17T15:10:20Z',
  },
  {
    id: 'evt-8902-03',
    incident_id: 'INC-2024-8902',
    event_type: 'root_cause_classified',
    agent_name: 'Root Cause Analyzer',
    action_description: 'Upstream third-party payment rail degradation',
    tool_call: 'classify_root_cause',
    execution_status: 'success',
    requires_approval: false,
    guardrail_triggered: false,
    details: 'Confidence score: 0.87. Root cause category: dependency_external',
    timestamp: '2026-10-17T15:12:00Z',
  },
  {
    id: 'evt-8902-04',
    incident_id: 'INC-2024-8902',
    event_type: 'approval_denied',
    agent_name: 'Approval Gate',
    action_description: 'Operator denied failover proposal: "Too risky during flash sale peak"',
    tool_call: 'deny_approval',
    execution_status: 'failure',
    requires_approval: true,
    guardrail_triggered: false,
    details:
      'Denial reason: Unverified secondary rail capacity. Transitioning incident to escalated state.',
    timestamp: '2026-10-17T15:16:45Z',
  },
  {
    id: 'evt-8902-05',
    incident_id: 'INC-2024-8902',
    event_type: 'incident_escalated',
    agent_name: 'Incident Coordinator',
    action_description: 'Incident escalated to on-call Engineering VP and external vendor liaison',
    tool_call: 'escalate_incident',
    execution_status: 'success',
    requires_approval: false,
    guardrail_triggered: false,
    details: 'Automated remediation halted; human incident commander took manual control',
    timestamp: '2026-10-17T15:17:00Z',
  },
]

export const SEED_EVENTS_8899: IncidentEvent[] = [
  {
    id: 'evt-8899-01',
    incident_id: 'INC-2024-8899',
    event_type: 'alert_ingested',
    agent_name: 'Alert Ingestion Service',
    action_description: 'Alert: order-processor consumer group lag > 50,000 messages',
    tool_call: 'webhook_ingest',
    execution_status: 'success',
    requires_approval: false,
    guardrail_triggered: false,
    timestamp: '2026-10-17T13:00:00Z',
  },
  {
    id: 'evt-8899-02',
    incident_id: 'INC-2024-8899',
    event_type: 'analysis_started',
    agent_name: 'Incident Coordinator',
    action_description: 'Automated multi-agent investigation started',
    tool_call: 'fan_out_investigation',
    execution_status: 'success',
    requires_approval: false,
    guardrail_triggered: false,
    timestamp: '2026-10-17T13:00:15Z',
  },
  {
    id: 'evt-8899-03',
    incident_id: 'INC-2024-8899',
    event_type: 'root_cause_classified',
    agent_name: 'Root Cause Analyzer',
    action_description: 'Memory leak in unbuffered batch payload deserialization',
    tool_call: 'classify_root_cause',
    execution_status: 'success',
    requires_approval: false,
    guardrail_triggered: false,
    details: 'Heap dump analysis confirmed buffer retention. Confidence: 0.95',
    timestamp: '2026-10-17T13:02:10Z',
  },
  {
    id: 'evt-8899-04',
    incident_id: 'INC-2024-8899',
    event_type: 'guardrail_violation',
    agent_name: 'Guardrails Engine',
    action_description:
      'Mandatory Guardrail Interception: Blocked destructive command pattern on order-worker-03',
    tool_call: 'intercept_command',
    execution_status: 'blocked',
    requires_approval: false,
    guardrail_triggered: true,
    details:
      'BLOCKED by Non-Bypassable Policy RG-01 & RG-04. Intercepted command: "rm -rf /var/log/orders/* && kill -9 1". Command contains wildcard recursive delete and SIGKILL on PID 1. Zero tolerance rule enforced.',
    timestamp: '2026-10-17T13:03:00Z',
  },
  {
    id: 'evt-8899-05',
    incident_id: 'INC-2024-8899',
    event_type: 'remediation_executed',
    agent_name: 'Auto-Executor',
    action_description: 'Executed safe whitelisted command: systemctl restart order-worker',
    tool_call: 'execute_remediation_command',
    execution_status: 'success',
    requires_approval: false,
    guardrail_triggered: false,
    details: 'Pre-flight verified healthy replica; graceful restart succeeded in 4.2s',
    timestamp: '2026-10-17T13:04:15Z',
  },
  {
    id: 'evt-8899-06',
    incident_id: 'INC-2024-8899',
    event_type: 'incident_resolved',
    agent_name: 'Incident Coordinator',
    action_description:
      'Post-flight checks verified consumer lag decreased to zero; marked resolved',
    tool_call: 'resolve_incident',
    execution_status: 'success',
    requires_approval: false,
    guardrail_triggered: false,
    details: 'Total MTTR: 5 minutes 30 seconds',
    timestamp: '2026-10-17T13:05:30Z',
  },
]

// ============================================================================
// Remediation Options Seed Data
// ============================================================================

export const SEED_REMEDIATION_8901: RemediationOption[] = [
  {
    id: 'rem-8901-opt-01',
    title: 'Terminate idle in transaction connections and expand pool capacity',
    risk_level: 'high',
    verdict: 'requires_approval',
    action_command:
      "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE state = 'idle in transaction' AND state_change < current_timestamp - INTERVAL '5 minutes';",
    pre_flight_checks: [
      'Verify replication lag on read replica < 50ms',
      'Confirm connection pool utilization > 95%',
      'Verify backup snapshot completed within 60 minutes',
    ],
    steps: [
      'Execute pg_terminate_backend on all hung transactions older than 300s',
      'Increase MaxPoolSize in auth-api connection string pool from 100 to 200',
      'Trigger graceful worker pool refresh in auth-api pods',
    ],
    post_flight_checks: [
      'Verify active connection count drops below 40%',
      'Confirm auth-api error rate falls below 0.1% for 3 consecutive minutes',
      'Verify zero deadlocks reported in Postgres log',
    ],
    rollback_description:
      'Revert MaxPoolSize back to 100; reload auth-api configuration with default connection settings.',
    approval_request_id: 'appr-8901',
  },
  {
    id: 'rem-8901-opt-02',
    title: 'Restart all auth-api pods simultaneously (BLOCKED)',
    risk_level: 'high',
    verdict: 'blocked_by_guardrail',
    action_command: 'kubectl delete pods --all -n auth --force --grace-period=0',
    pre_flight_checks: ['None — destructive pattern'],
    steps: ['Forced pod termination across entire auth namespace'],
    post_flight_checks: ['Unreachable'],
    rollback_description: 'Cannot rollback service outage caused by simultaneous pod termination',
  },
]

// ============================================================================
// Approvals Seed Data
// ============================================================================

export const SEED_APPROVALS: ApprovalRequest[] = [
  {
    id: 'appr-8904',
    incident_id: 'INC-2024-8904',
    action: 'Drain and isolate compromised border gateway router',
    command: 'edge-router-cli --action drain --target bgp-gw-02 --grace-period 30s',
    risk_level: 'high',
    approval_level: 'admin',
    timeout_at: new Date(Date.now() + 50 * 1000).toISOString(), // 50 seconds remaining (< 60s countdown)
    created_at: new Date(Date.now() - 14 * 60 * 1000).toISOString(),
    status: 'pending',
    notes:
      'Edge anomaly detected. Automated failover timeout in under 1 minute. Admin confirmation mandatory.',
    rollback_description: 'edge-router-cli --action undrain --target bgp-gw-02',
    verdict: 'requires_approval',
  },
  {
    id: 'appr-8903',
    incident_id: 'INC-2024-8903',
    action: 'Restart redis-cart cache cluster pods to evict corrupted serialization keys',
    command: 'kubectl rollout restart deployment/redis-cart -n cart-prod',
    risk_level: 'medium',
    approval_level: 'operator',
    timeout_at: new Date(Date.now() + 8 * 60 * 1000).toISOString(), // 8 minutes remaining
    created_at: new Date(Date.now() - 7 * 60 * 1000).toISOString(),
    status: 'pending',
    notes: 'Cache key deserialization exceptions in cart-service. Operator or Lead may approve.',
    rollback_description: 'kubectl rollout undo deployment/redis-cart -n cart-prod',
    verdict: 'requires_approval',
  },
  {
    id: 'appr-8901',
    incident_id: 'INC-2024-8901',
    action: 'Terminate idle connections and increase connection pool',
    command:
      "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE state = 'idle in transaction' AND state_change < current_timestamp - INTERVAL '5 minutes';",
    risk_level: 'high',
    approval_level: 'lead',
    timeout_at: new Date(Date.now() + 12 * 60 * 1000).toISOString(), // 12 minutes remaining
    created_at: '2026-10-17T16:32:30Z',
    status: 'pending',
    notes: 'Connection pool saturation causing 18% auth errors. Requires SRE Lead confirmation.',
    rollback_description:
      'Revert MaxPoolSize back to 100; reload auth-api configuration with default connection settings.',
    verdict: 'requires_approval',
  },
  {
    id: 'appr-8905',
    incident_id: 'INC-2024-8901',
    action: 'Force delete all pods in namespace auth (BLOCKED BY GUARDRAIL)',
    command: 'kubectl delete pods --all -n auth --force --grace-period=0',
    risk_level: 'high',
    approval_level: 'admin',
    timeout_at: new Date(Date.now() + 14 * 60 * 1000).toISOString(),
    created_at: new Date().toISOString(),
    status: 'pending',
    notes: 'Intercepted destructive pattern: mass pod termination violates Guardrail Policy RG-01.',
    rollback_description: 'Cannot rollback service outage caused by simultaneous pod termination',
    verdict: 'blocked_by_guardrail',
  },
  {
    id: 'appr-8902',
    incident_id: 'INC-2024-8902',
    action: 'Force circuit breaker trip and switch traffic to secondary payment rail',
    command: 'curl -X POST http://payment-gateway.internal/admin/failover -d \'{"rail":"stripe"}\'',
    risk_level: 'high',
    approval_level: 'lead',
    timeout_at: '2026-10-17T15:25:00Z',
    created_at: '2026-10-17T15:10:00Z',
    status: 'denied',
    notes: 'Denied by SRE Lead: Too risky during peak flash sale hours without prior smoke test.',
    rollback_description: 'Re-enable primary payment gateway route in Envoy service mesh.',
    verdict: 'requires_approval',
  },
]

// ============================================================================
// Incidents Seed Data
// ============================================================================

export const SEED_INCIDENTS_DETAIL: Record<string, IncidentDetail> = {
  'INC-2024-8901': {
    id: 'INC-2024-8901',
    alert_id: 'alert-prom-auth-4412',
    title: 'Database connection pool exhaustion in auth-api',
    severity: 'critical',
    status: 'awaiting_approval',
    affected_services: ['auth-api', 'user-session'],
    affected_hosts: ['auth-api-pod-1', 'auth-api-pod-2', 'pg-primary-01'],
    root_cause: 'Database connection pool exhaustion caused by 42 leaked idle transactions',
    root_cause_confidence: 0.92,
    root_cause_category: 'database',
    proposed_remediation: 'Terminate idle in transaction queries and expand pool capacity to 200',
    remediation_risk_level: 'high',
    triggered_at: '2026-10-17T16:30:00Z',
    started_investigating_at: '2026-10-17T16:30:15Z',
    resolved_at: null,
    assigned_to: 'sre-lead@trinetra.internal',
    events: SEED_EVENTS_8901,
  },
  'INC-2024-8902': {
    id: 'INC-2024-8902',
    alert_id: 'alert-dd-pay-9981',
    title: 'Upstream payment gateway timeout surge',
    severity: 'high',
    status: 'escalated',
    affected_services: ['payment-gateway', 'checkout-service'],
    affected_hosts: ['payment-svc-01', 'payment-svc-02'],
    root_cause: 'Third-party upstream payment rail latency degradation and HTTP 504 timeouts',
    root_cause_confidence: 0.87,
    root_cause_category: 'dependency_external',
    proposed_remediation: 'Failover to secondary payment gateway processor',
    remediation_risk_level: 'high',
    triggered_at: '2026-10-17T15:10:00Z',
    started_investigating_at: '2026-10-17T15:10:20Z',
    resolved_at: null,
    assigned_to: 'devops-operator@trinetra.internal',
    events: SEED_EVENTS_8902,
  },
  'INC-2024-8899': {
    id: 'INC-2024-8899',
    alert_id: 'alert-prom-order-1184',
    title: 'Memory leak and unhandled exception in order-processor worker',
    severity: 'high',
    status: 'resolved',
    affected_services: ['order-processor'],
    affected_hosts: ['order-worker-03'],
    root_cause: 'Memory leak in unbuffered batch message deserializer',
    root_cause_confidence: 0.95,
    root_cause_category: 'application',
    proposed_remediation: 'Restart order-worker container gracefully with clean memory bounds',
    remediation_risk_level: 'low',
    triggered_at: '2026-10-17T13:00:00Z',
    started_investigating_at: '2026-10-17T13:00:15Z',
    resolved_at: '2026-10-17T13:05:30Z',
    assigned_to: 'sre-lead@trinetra.internal',
    events: SEED_EVENTS_8899,
  },
  'INC-2024-8898': {
    id: 'INC-2024-8898',
    alert_id: 'alert-prom-search-3392',
    title: 'High CPU utilization in search-indexer cluster',
    severity: 'medium',
    status: 'resolved',
    affected_services: ['search-indexer'],
    affected_hosts: ['search-idx-01', 'search-idx-02'],
    root_cause: 'Heavy unindexed regex wildcard query burst',
    root_cause_confidence: 0.89,
    root_cause_category: 'workload_spike',
    proposed_remediation: 'Apply query throttling and index cache warming',
    remediation_risk_level: 'low',
    triggered_at: '2026-10-17T11:20:00Z',
    started_investigating_at: '2026-10-17T11:20:10Z',
    resolved_at: '2026-10-17T11:28:45Z',
    assigned_to: null,
    events: [],
  },
  'INC-2024-8897': {
    id: 'INC-2024-8897',
    alert_id: 'alert-prom-redis-7721',
    title: 'Redis session store maxmemory eviction threshold exceeded',
    severity: 'medium',
    status: 'resolved',
    affected_services: ['session-store'],
    affected_hosts: ['redis-cache-01'],
    root_cause: 'Expired session TTL keys not pruned due to background task stall',
    root_cause_confidence: 0.93,
    root_cause_category: 'cache',
    proposed_remediation: 'Trigger active key eviction and scale volatile-lru policy',
    remediation_risk_level: 'low',
    triggered_at: '2026-10-17T09:40:00Z',
    started_investigating_at: '2026-10-17T09:40:15Z',
    resolved_at: '2026-10-17T09:47:00Z',
    assigned_to: null,
    events: [],
  },
  'INC-2024-8896': {
    id: 'INC-2024-8896',
    alert_id: 'alert-prom-dns-6612',
    title: 'DNS resolution degradation in internal ingress controller',
    severity: 'low',
    status: 'resolved',
    affected_services: ['internal-ingress'],
    affected_hosts: ['ingress-gw-01'],
    root_cause: 'CoreDNS upstream recursive timeout on internal cluster domain',
    root_cause_confidence: 0.84,
    root_cause_category: 'network',
    proposed_remediation: 'Flush local resolver cache and cycle CoreDNS pods',
    remediation_risk_level: 'low',
    triggered_at: '2026-10-17T07:15:00Z',
    started_investigating_at: '2026-10-17T07:15:20Z',
    resolved_at: '2026-10-17T07:22:10Z',
    assigned_to: null,
    events: [],
  },
}

export const SEED_INCIDENTS_SUMMARY: IncidentSummary[] = Object.values(SEED_INCIDENTS_DETAIL).map(
  (inc) => ({
    id: inc.id,
    alert_id: inc.alert_id,
    title: inc.title,
    severity: inc.severity,
    status: inc.status,
    affected_services: inc.affected_services,
    triggered_at: inc.triggered_at,
    root_cause: inc.root_cause,
    assigned_to: inc.assigned_to,
  }),
)

// ============================================================================
// Autonomous Agents Seed Data
// ============================================================================

export const SEED_AGENTS: Agent[] = [
  {
    id: 'agent-coordinator',
    name: 'Incident Coordinator',
    status: 'working',
    current_tool_call: 'orchestrate_parallel_investigation',
    elapsed_time_seconds: 145,
    guardrail_violations_count: 0,
    last_active_at: '2026-10-17T16:32:30Z',
    current_incident_id: 'INC-2024-8901',
  },
  {
    id: 'agent-log-analyzer',
    name: 'Log Analyzer',
    status: 'idle',
    current_tool_call: null,
    elapsed_time_seconds: 0,
    guardrail_violations_count: 0,
    last_active_at: '2026-10-17T16:31:25Z',
    current_incident_id: null,
  },
  {
    id: 'agent-metrics-querier',
    name: 'Metrics Querier',
    status: 'idle',
    current_tool_call: null,
    elapsed_time_seconds: 0,
    guardrail_violations_count: 0,
    last_active_at: '2026-10-17T16:31:02Z',
    current_incident_id: null,
  },
  {
    id: 'agent-runbook-retriever',
    name: 'Runbook Retriever',
    status: 'idle',
    current_tool_call: null,
    elapsed_time_seconds: 0,
    guardrail_violations_count: 0,
    last_active_at: '2026-10-17T16:31:40Z',
    current_incident_id: null,
  },
  {
    id: 'agent-root-cause-analyzer',
    name: 'Root Cause Analyzer',
    status: 'idle',
    current_tool_call: null,
    elapsed_time_seconds: 0,
    guardrail_violations_count: 0,
    last_active_at: '2026-10-17T16:32:10Z',
    current_incident_id: null,
  },
  {
    id: 'agent-remediation-proposer',
    name: 'Remediation Proposer',
    status: 'waiting',
    current_tool_call: 'await_human_approval(appr-8901)',
    elapsed_time_seconds: 120,
    guardrail_violations_count: 0,
    last_active_at: '2026-10-17T16:32:30Z',
    current_incident_id: 'INC-2024-8901',
  },
  {
    id: 'agent-auto-executor',
    name: 'Auto-Executor',
    status: 'idle',
    current_tool_call: null,
    elapsed_time_seconds: 0,
    guardrail_violations_count: 1, // 1 violation caught on INC-2024-8899!
    last_active_at: '2026-10-17T13:04:15Z',
    current_incident_id: null,
  },
  {
    id: 'agent-audit-logger',
    name: 'Audit Logger',
    status: 'working',
    current_tool_call: 'append_audit_event',
    elapsed_time_seconds: 4,
    guardrail_violations_count: 0,
    last_active_at: '2026-10-17T16:32:30Z',
    current_incident_id: 'INC-2024-8901',
  },
]

// ============================================================================
// Runbooks Seed Data
// ============================================================================

export const SEED_RUNBOOKS: Runbook[] = [
  {
    id: 'runbook-db-pool-01',
    title: 'Database Connection Pool Recovery',
    category: 'database',
    tags: ['database', 'postgres', 'connection-pool', 'auth-api'],
    applicable_services: ['auth-api', 'user-session', 'billing-service'],
    estimated_time_minutes: 10,
    is_active: true,
    version: 3,
    description:
      'Diagnostic and automated recovery procedures when PostgreSQL connection pool reaches saturation.',
    content: `## Overview
This runbook covers emergency diagnostics and automated mitigation for connection pool exhaustion in PostgreSQL clusters.

### Symptoms
- Sudden spike in HTTP 500 error rates on authentication endpoints.
- Log error messages matching \`The connection pool has reached its maximum size\`.
- \`pg_stat_activity\` active count approaches \`max_connections\`.

### Diagnostic Steps
1. Inspect \`pg_stat_activity\` for sessions in state \`idle in transaction\`.
2. Determine if queries are blocked behind table or row level exclusive locks.
3. Review replica replication delay.

### Safe Mitigation
Terminate sessions that have remained in \`idle in transaction\` state for more than 5 minutes. If connection demand remains sustained, scale connection pool max capacity.`,
    remediation_steps: [
      {
        step: 1,
        action: 'Query active and idle connections',
        command:
          "SELECT pid, client_addr, state, query_start, query FROM pg_stat_activity WHERE state != 'idle';",
      },
      {
        step: 2,
        action: 'Terminate idle transactions older than 5 minutes',
        command:
          "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE state = 'idle in transaction' AND state_change < current_timestamp - INTERVAL '5 minutes';",
      },
    ],
  },
  {
    id: 'runbook-pod-oom-02',
    title: 'Kubernetes Pod Out-Of-Memory (OOMKilled) Remediation',
    category: 'kubernetes',
    tags: ['kubernetes', 'oom', 'memory-leak', 'order-processor'],
    applicable_services: ['order-processor', 'search-indexer'],
    estimated_time_minutes: 8,
    is_active: true,
    version: 2,
    description:
      'Procedures for identifying memory exhaustion and executing graceful worker cycle.',
    content: `## Overview
Addresses pods failing with exit code 137 (OOMKilled) caused by batch processing memory leaks.`,
    remediation_steps: [
      {
        step: 1,
        action: 'Check pod restart count and exit reason',
        command:
          'kubectl get pod -l app=order-processor -o jsonpath="{.items[*].status.containerStatuses[*].lastState.terminated.reason}"',
      },
      {
        step: 2,
        action: 'Graceful rolling restart of worker deployment',
        command: 'kubectl rollout restart deployment/order-processor',
      },
    ],
  },
  {
    id: 'runbook-upstream-timeout-03',
    title: 'External Payment Gateway Upstream Circuit Breaker',
    category: 'network',
    tags: ['payment', 'timeout', 'circuit-breaker', 'gateway'],
    applicable_services: ['payment-gateway'],
    estimated_time_minutes: 5,
    is_active: true,
    version: 1,
    description: 'Circuit breaker failover procedures when third party payment provider degrades.',
    content: `## Overview
Steps to trip the circuit breaker and switch checkout requests to the secondary rail.`,
    remediation_steps: [
      {
        step: 1,
        action: 'Check upstream endpoint health',
        command: 'curl -I https://api.primary-payment-rail.net/health',
      },
      {
        step: 2,
        action: 'Trip circuit breaker to secondary rail',
        command:
          'curl -X POST http://payment-gateway.internal/admin/failover -d \'{"rail":"stripe"}\'',
      },
    ],
  },
]

// ============================================================================
// Dashboard Metrics Seed Data
// ============================================================================

export const SEED_DASHBOARD_METRICS: DashboardMetrics = {
  incident_trends: {
    labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
    data: [12, 15, 8, 10, 14, 6, 9],
  },
  severity_distribution: {
    critical: 1,
    high: 2,
    medium: 2,
    low: 1,
  },
  resolution_time_distribution: {
    under_5_min: 24,
    '5_to_15_min': 18,
    '15_to_30_min': 9,
    over_30_min: 2,
  },
  mttr_trend: {
    labels: ['Week 1', 'Week 2', 'Week 3', 'Week 4'],
    data: [18.4, 15.2, 13.0, 11.8],
  },
  summary: {
    active_incidents: 2,
    critical_incidents: 1,
    avg_mttr_minutes: 11.8,
    auto_resolution_rate: 0.68,
    incidents_this_week: 14,
    most_common_root_cause: 'database',
  },
}
