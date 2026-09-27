# Trinetra Agent Specifications
## Complete Multi-Agent Architecture & Specifications

**Version:** 1.0  
**Total Agents:** 7 + 1 Coordinator = 8 agents  
**Framework:** LangGraph (Python) OR Semantic Kernel (.NET)  
**LLM:** Claude 3.5 Sonnet (Anthropic API)

---

## TABLE OF CONTENTS

1. [Agent Overview](#agent-overview)
2. [Incident Coordinator](#incident-coordinator-agent)
3. [Log Analyzer Agent](#log-analyzer-agent)
4. [Metrics Querier Agent](#metrics-querier-agent)
5. [Runbook Retriever Agent](#runbook-retriever-agent)
6. [Root Cause Analyzer](#root-cause-analyzer-agent)
7. [Remediation Proposer](#remediation-proposer-agent)
8. [Auto-Executor Agent](#auto-executor-agent)
9. [Approval Gate Agent](#approval-gate-agent)
10. [Agent Orchestration](#agent-orchestration)

---

## AGENT OVERVIEW

### Agent Roles & Responsibilities

| Agent | Role | Parallelizable |
|-------|------|---|
| Incident Coordinator | Controller/Orchestrator | No |
| Log Analyzer | Investigation | Yes |
| Metrics Querier | Investigation | Yes |
| Runbook Retriever | Investigation | Yes |
| Root Cause Analyzer | Analysis | No |
| Remediation Proposer | Decision | No |
| Auto-Executor | Execution | No |
| Approval Gate | Workflow | No |

---

## INCIDENT COORDINATOR AGENT

### Purpose
Entry point and orchestrator for the entire incident investigation workflow

### Configuration
```
Model:              Claude 3.5 Sonnet
Temperature:        0.3 (low randomness, deterministic)
Max Tokens:         2000
Response Format:    JSON
Timeout:            5 minutes
```

### Responsibilities
1. Receive and validate incident data
2. Create incident record in database
3. Orchestrate multi-agent investigation
4. Manage workflow state transitions
5. Handle escalations
6. Generate incident summary

### Tools Available
```
1. create_incident(alert_data)
   Returns: incident_id
   
2. update_incident_status(incident_id, new_status)
   Returns: success/error
   
3. invoke_agent(agent_name, params)
   Agents: [log_analyzer, metrics_querier, runbook_retriever]
   Returns: agent_response
   
4. wait_for_agents(agent_list, timeout_seconds=300)
   Waits for multiple agents in parallel
   Returns: {agent_name: result}
   
5. escalate_incident(incident_id, reason)
   Marks incident as escalated
   Returns: success
```

### Workflow
```
1. Receive alert
   ├─ Validate schema
   ├─ Create incident
   └─ Set status: TRIGGERED

2. Determine severity & priority
   ├─ Assess impact
   ├─ Check SLA
   └─ Assign team

3. Invoke parallel investigation
   ├─ Start Log Analyzer
   ├─ Start Metrics Querier
   └─ Start Runbook Retriever

4. Await investigation completion
   ├─ Monitor timeouts
   ├─ Collect findings
   └─ Update status: ANALYSIS_COMPLETE

5. Invoke Root Cause Analyzer
   ├─ Pass all findings
   ├─ Await analysis
   └─ Store root cause

6. Invoke Remediation Proposer
   ├─ Generate options
   ├─ Assess risk
   └─ Create approval request if needed

7. Route to execution
   ├─ If low-risk: invoke Auto-Executor
   └─ If high-risk: invoke Approval Gate

8. Monitor resolution
   ├─ Track execution
   ├─ Validate recovery
   └─ Close incident
```

---

## LOG ANALYZER AGENT

### Purpose
Analyze logs to identify error patterns, stack traces, and anomalies

### Configuration
```
Model:              Claude 3.5 Sonnet
Temperature:        0.2
Max Tokens:         3000
Response Format:    JSON
Timeout:            2 minutes
Max Log Lines:      10000
```

### Responsibilities
1. Query log aggregation systems
2. Parse structured and unstructured logs
3. Identify error patterns
4. Extract stack traces
5. Detect anomalies
6. Correlate with deployments

### Tools Available
```
1. query_logs(service_name, time_range="5m", limit=1000)
   Calls: Elasticsearch / Splunk / DataDog
   Returns: {logs: [], count: int, errors: int}
   
2. extract_stack_traces(log_data)
   Parses stack traces from logs
   Returns: {traces: [], languages: [], exceptions: []}
   
3. detect_anomalies(log_data, baseline_metrics)
   ML-based anomaly detection
   Returns: {anomalies: [], severity: string}
   
4. correlate_with_deployment(service, time_range)
   Links logs to recent deployments
   Returns: {recent_deployments: [], changed_files: []}
```

### Output Example
```json
{
  "incident_id": "inc_uuid_123",
  "source": "elasticsearch",
  "logs_retrieved": 1250,
  "error_count": 180,
  "error_patterns": [
    "NullReferenceException in PaymentProcessor.cs:234",
    "Database connection timeout (pool exhausted)",
    "Redis timeout errors"
  ],
  "stack_traces": [
    "System.NullReferenceException: Object reference not set",
    "System.Data.SqlClient.SqlTimeout: Connection timeout"
  ],
  "anomalies": [
    "Error rate spiked from 0.1% to 12% at 14:23:45",
    "Exception count increased 50x in last 5 minutes"
  ],
  "related_deployments": [
    {
      "service": "auth-api",
      "version": "v2.3.4",
      "deployed_at": "2026-08-26T14:08:00Z",
      "changed_files": ["PaymentProcessor.cs"]
    }
  ],
  "summary": "Database connection pool exhaustion with null reference error in new deployment",
  "confidence": 0.92
}
```

---

## METRICS QUERIER AGENT

### Purpose
Query infrastructure metrics and detect resource anomalies

### Configuration
```
Model:              Claude 3.5 Sonnet
Temperature:        0.2
Max Tokens:         2500
Response Format:    JSON
Timeout:            2 minutes
```

### Responsibilities
1. Query metrics platforms
2. Calculate resource utilization
3. Detect baseline deviations
4. Identify resource saturation
5. Correlate metrics with incident time
6. Provide resource allocation insights

### Tools Available
```
1. query_metrics(metric_names[], time_range="5m", aggregation="avg")
   Sources: Prometheus / DataDog / New Relic
   Returns: {metrics: {name: [values]}}
   
2. get_baseline_metrics(service, metric, lookback_days=7)
   Retrieves statistical baseline
   Returns: {p50, p95, p99, avg, min, max}
   
3. detect_saturation(service, threshold_percent=90)
   Identifies resource limits
   Returns: {saturated_resources: [], spikes: []}
   
4. correlate_with_incident_time(metrics, incident_start)
   Finds metric deviations at incident time
   Returns: {correlated_metrics: [], timing_offset_sec: int}
```

### Output Example
```json
{
  "incident_id": "inc_uuid_123",
  "metrics_source": "prometheus",
  "time_range": {
    "start": "2026-08-26T14:18:00Z",
    "end": "2026-08-26T14:35:00Z"
  },
  "resource_saturation": {
    "cpu_percent": {
      "current": 92,
      "baseline_p95": 65,
      "spike_percent": 27
    },
    "memory_percent": {
      "current": 88,
      "baseline_p95": 72,
      "spike_percent": 16
    },
    "database_connections": {
      "current": 485,
      "max_pool": 500,
      "available": 15,
      "utilization_percent": 97
    },
    "disk_io_read_mbps": {
      "current": 450,
      "baseline_p95": 120,
      "spike_percent": 275
    }
  },
  "anomalies": [
    "Database connection pool at 97% capacity",
    "CPU spiked 40% above normal baseline",
    "Memory pressure increased significantly"
  ],
  "correlation_analysis": {
    "anomaly_start": "2026-08-26T14:23:45Z",
    "incident_start": "2026-08-26T14:23:45Z",
    "offset_seconds": 0,
    "metrics_led_incident": true
  },
  "summary": "Database connection pool exhaustion with cascading CPU and memory pressure",
  "confidence": 0.95
}
```

---

## RUNBOOK RETRIEVER AGENT

### Purpose
Use RAG to find relevant operational runbooks for resolution

### Configuration
```
Model:              Claude 3.5 Sonnet
Temperature:        0.1 (minimal randomness)
Max Tokens:         2000
Response Format:    JSON
Timeout:            1 minute
Vector Similarity:  cosine distance < 0.3
```

### Responsibilities
1. Perform semantic search on runbooks
2. Find similar historical incidents
3. Retrieve remediation procedures
4. Rank by relevance
5. Extract key steps
6. Provide contextual guidance

### Tools Available
```
1. search_runbooks_by_embedding(incident_summary, top_k=5)
   Semantic similarity search using pgvector
   Returns: {runbooks: [], similarity_scores: []}
   
2. search_runbooks_by_keyword(keywords[], top_k=5)
   Keyword-based search (fallback)
   Returns: {runbooks: [], match_scores: []}
   
3. search_incident_history(incident_summary, top_k=3)
   Find similar historical incidents
   Returns: {past_incidents: [], solutions_applied: []}
   
4. extract_remediation_steps(runbook_id)
   Get structured steps from runbook
   Returns: {steps: [], time_estimate: int, risks: []}
```

### Output Example
```json
{
  "incident_id": "inc_uuid_123",
  "retrieved_runbooks": [
    {
      "id": "runbook_uuid_1",
      "title": "Database Connection Pool Exhaustion Recovery",
      "similarity_score": 0.92,
      "applicable_services": ["auth-api", "user-api", "payment-api"],
      "estimated_time_minutes": 10,
      "remediation_steps": [
        {
          "step": 1,
          "action": "Check connection pool status",
          "command": "SELECT count(*) FROM pg_stat_activity;",
          "expected_result": "Connection count and idle time"
        },
        {
          "step": 2,
          "action": "Kill long-running idle queries",
          "command": "SELECT pg_terminate_backend(pid) WHERE ...",
          "risk": "medium"
        }
      ],
      "risks": [
        "May interrupt active transactions",
        "Requires SRE approval"
      ]
    }
  ],
  "similar_past_incidents": [
    {
      "date": "2026-08-15",
      "title": "Database connection pool exhaustion",
      "root_cause": "Unoptimized query in recent deployment",
      "resolution_applied": "Rolled back deployment",
      "resolution_time_minutes": 8,
      "success": true
    }
  ],
  "knowledge_gaps": [],
  "summary": "Found highly relevant runbook and similar incident with successful resolution",
  "confidence": 0.96
}
```

---

## ROOT CAUSE ANALYZER AGENT

### Purpose
Correlate findings to identify root cause

### Configuration
```
Model:              Claude 3.5 Sonnet (extended context)
Temperature:        0.3
Max Tokens:         3000
Response Format:    JSON
Timeout:            2 minutes
```

### Responsibilities
1. Correlate all findings
2. Identify root cause
3. Assign confidence score
4. Classify incident type
5. Identify contributing factors
6. Provide evidence chain

### Classification Categories
```
DEPLOYMENT     - Code change introduced bug
RESOURCE       - CPU/Memory/Disk exhaustion
DATABASE       - Connection pool, locks, query
EXTERNAL       - Third-party service failure
CONFIG         - Configuration error/misconfiguration
CASCADE        - Cascading failure from other service
HARDWARE       - Infrastructure/hardware issue
UNKNOWN        - Unable to determine
```

### Output Example
```json
{
  "incident_id": "inc_uuid_123",
  "root_cause": "Database connection pool exhaustion caused by unoptimized query in auth-api v2.3.4 deployment",
  "root_cause_category": "DEPLOYMENT",
  "confidence_score": 0.88,
  "contributing_factors": [
    "auth-api v2.3.4 deployed 15 min ago with new payment validation logic",
    "New code queries database without timeout parameters",
    "Peak traffic coincided with deployment timing",
    "Database connection pool at max capacity (500)"
  ],
  "evidence_chain": {
    "deployment_evidence": {
      "source": "git_history",
      "detail": "PaymentProcessor.cs changed in v2.3.4",
      "confidence": 0.95
    },
    "logs_evidence": {
      "source": "elasticsearch",
      "detail": "NullReferenceException and database timeout errors",
      "confidence": 0.92
    },
    "metrics_evidence": {
      "source": "prometheus",
      "detail": "Connection pool utilization jumped from 15% to 97%",
      "confidence": 0.95
    },
    "runbook_evidence": {
      "source": "incident_history",
      "detail": "Similar incident (2026-08-15) resolved by rollback",
      "confidence": 0.90
    }
  },
  "similar_incidents": [
    {
      "date": "2026-08-10",
      "title": "Payment API failure - database pool",
      "resolution": "Rollback deployment to v2.3.3"
    }
  ],
  "severity_assessment": "HIGH - Core payment functionality affected",
  "affected_users_estimate": "5000-10000"
}
```

---

## REMEDIATION PROPOSER AGENT

### Purpose
Generate remediation options with risk assessment

### Configuration
```
Model:              Claude 3.5 Sonnet
Temperature:        0.2
Max Tokens:         3000
Response Format:    JSON
Timeout:            2 minutes
```

### Responsibilities
1. Generate 2-3 remediation options
2. Assess risk for each option
3. Estimate execution time
4. Determine approval requirement
5. Generate pre/post-flight checks
6. Provide implementation details

### Output Example
```json
{
  "incident_id": "inc_uuid_123",
  "root_cause_summary": "Database connection pool exhaustion from unoptimized query in v2.3.4",
  "remediation_options": [
    {
      "option_id": 1,
      "title": "Immediate: Kill Long-Running Queries",
      "description": "Identify and terminate queries blocking the connection pool",
      "implementation_steps": [
        "Query: SELECT * FROM pg_stat_activity WHERE state='active' AND query_start < now() - '5 min'",
        "Kill: SELECT pg_terminate_backend(pid) WHERE ..."
      ],
      "risk_level": "medium",
      "risk_factors": [
        "May interrupt active transactions",
        "Potential data consistency risk if transaction not atomic"
      ],
      "estimated_time_minutes": 3,
      "requires_approval": true,
      "approval_level": "lead",
      "success_probability": 0.65,
      "preflight_checks": [
        "Verify no critical transactions in progress",
        "Confirm database replication lag < 1 second"
      ],
      "postflight_checks": [
        "Verify connection pool utilization < 70%",
        "Confirm error rate returns to baseline",
        "Check application logs for new errors"
      ]
    },
    {
      "option_id": 2,
      "title": "Intermediate: Rollback Deployment",
      "description": "Rollback auth-api from v2.3.4 to v2.3.3",
      "implementation_steps": [
        "Initiate rollback via deployment system",
        "Wait for pods to restart with v2.3.3",
        "Monitor error rates"
      ],
      "risk_level": "low",
      "risk_factors": [],
      "estimated_time_minutes": 5,
      "requires_approval": false,
      "success_probability": 0.95,
      "recommended": true
    }
  ]
}
```

---

## AUTO-EXECUTOR AGENT

### Purpose
Execute low-risk remediation actions with safety guarantees

### Configuration
```
Model:              Claude 3.5 Sonnet
Temperature:        0.1 (minimal randomness)
Max Tokens:         2000
Response Format:    JSON
Timeout:            5 minutes (per command)
Execution Context:  Docker container (sandboxed)
```

### Responsibilities
1. Execute approved remediation
2. Run pre-flight checks
3. Execute step-by-step
4. Monitor during execution
5. Run post-flight checks
6. Rollback if validation fails

### Execution Guardrails
```
BLOCKED COMMANDS (Never Execute):
  - rm -rf /
  - shutdown / reboot
  - kill -9 <critical_pid>
  - dd if=/dev/zero
  - mkfs
  - Any sudo without explicit approval
  - Any access to /etc/shadow
  
RATE LIMITS:
  - Max 1 remediation per incident
  - Max 5 concurrent across incidents
  - Max 3 retries per command
  - 5 minute timeout per command
```

### Output Example
```json
{
  "incident_id": "inc_uuid_123",
  "remediation_id": "rem_uuid_1",
  "action": "Kill long-running database queries",
  "status": "success",
  "execution_details": {
    "preflight_checks": [
      {"check": "No critical transactions", "status": "passed"},
      {"check": "Replication lag acceptable", "status": "passed"}
    ],
    "execution_steps": [
      {
        "step": 1,
        "command": "SELECT * FROM pg_stat_activity WHERE ...",
        "status": "success",
        "duration_ms": 245
      },
      {
        "step": 2,
        "command": "SELECT pg_terminate_backend(...)",
        "status": "success",
        "queries_killed": 3,
        "connections_freed": 42
      }
    ],
    "postflight_checks": [
      {"check": "Pool utilization < 70%", "status": "passed", "value": "62%"},
      {"check": "Error rate normalized", "status": "passed", "value": "0.2%"}
    ]
  },
  "metrics_before": {"pool_utilization": 0.97, "error_rate": 0.15},
  "metrics_after": {"pool_utilization": 0.62, "error_rate": 0.002},
  "outcome": "SUCCESS",
  "total_duration_seconds": 45
}
```

---

## APPROVAL GATE AGENT

### Purpose
Manage human-in-the-loop approval workflow

### Configuration
```
Model:              Claude 3.5 Sonnet
Temperature:        0.1
Max Tokens:         1500
Response Format:    JSON
Timeout:            15 minutes (approval wait)
```

### Responsibilities
1. Determine if approval needed
2. Format approval request
3. Send notifications
4. Track approval workflow
5. Handle timeout escalation
6. Log all decisions

### Output Example
```json
{
  "incident_id": "inc_uuid_123",
  "approval_request_id": "appr_uuid_1",
  "action": "Kill long-running database queries",
  "risk_level": "medium",
  "approval_needed": true,
  "approval_level": "lead",
  "timeout_minutes": 15,
  "notification_sent": true,
  "notification_channels": [
    "slack:#incident-response",
    "email:sre-lead@company.com",
    "pagerduty"
  ],
  "summary_for_approver": "Database connection pool exhausted. Proposing to kill 3 long-running queries (5+ min idle). Estimated time: 3 min. Success probability: 65%."
}
```

---

## AGENT ORCHESTRATION

### Execution Order
```
1. INCIDENT_RECEIVED
   └─> Incident Coordinator created

2. PHASE 1: INVESTIGATION (Parallel)
   ├─> Log Analyzer starts
   ├─> Metrics Querier starts
   └─> Runbook Retriever starts

3. PHASE 2: ANALYSIS (Sequential)
   ├─> Wait for all Phase 1 agents
   └─> Root Cause Analyzer starts

4. PHASE 3: REMEDIATION PLANNING
   └─> Remediation Proposer starts

5. PHASE 4: APPROVAL/EXECUTION
   ├─> IF high-risk: Approval Gate + await approval
   └─> IF low-risk: Auto-Executor starts

6. PHASE 5: MONITORING
   └─> Track incident resolution

7. PHASE 6: CLOSURE
   └─> Generate post-mortem
```

### Agent Communication
```
Message Broker: RabbitMQ
  ├─ Agent request queues
  ├─ Response channels
  ├─ Error channels
  └─ Audit channels

State Sync: Redis
  ├─ Incident state
  ├─ Agent status
  ├─ Approval status
  └─ Execution progress
```

---

## AGENT ERROR RECOVERY

### Error Handling Strategy
```
Detection:
  - Agent timeout (5 min)
  - Tool call failure
  - Validation error
  - Rate limit
  
Recovery:
  1. Retry with exponential backoff (max 3 attempts)
  2. If still fails: escalate to human team
  3. Log error with full context
  4. Update incident status: ESCALATED
```

---

**See deployment guide for agent deployment instructions.**
