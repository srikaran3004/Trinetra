# Trinetra Guardrails & Safety Mechanisms
## Enterprise-Grade Safety & Compliance

**Version:** 1.0  
**Framework:** NeMo Guardrails + Custom Policy Engine

---

## COMMAND EXECUTION SAFETY

### Blocked Commands (Never Execute)
```
Destructive Operations:
  - rm -rf /          (recursive delete from root)
  - shutdown          (system shutdown without approval)
  - reboot            (system restart without approval)
  - halt              (halt system)
  - kill -9 <pid>     (kill critical processes)
  - dd if=/dev/zero   (zero out disk)
  - mkfs              (format filesystem)
  - dropdb            (drop database)
  - drop database     (SQL database drop)
  - truncate table    (SQL table truncation)

Data Access:
  - cat /etc/shadow   (access password hashes)
  - cat /etc/passwd   (access user information)
  - sudo without approval

Network:
  - iptables          (firewall modification without review)
  - ip route          (route modification without review)
```

### Rate Limits
```
Per Incident:
  - Max 1 remediation action per incident
  - Max 3 retries per command
  - 5 minute timeout per command execution

Across System:
  - Max 5 concurrent remediation executions
  - Max 20 commands per minute system-wide
  - Max 100 commands per hour system-wide

Per User:
  - Max 10 approvals per day for any single user
  - Approval authority can be revoked
```

### Pre-Execution Validation
```
Checks Required:
  1. Command syntax validation
  2. Service not in maintenance mode
  3. No active critical transactions
  4. Database replication lag < 5 seconds
  5. Backup recently completed
  6. Team capacity available
  7. Approval obtained (if required)
```

---

## APPROVAL WORKFLOW

### Risk-Based Approval Matrix

| Action | Risk | Approval | Timeout |
|--------|------|----------|---------|
| Query logs | Low | None | N/A |
| Query metrics | Low | None | N/A |
| Kill idle query | Medium | Lead | 15 min |
| Scale down pods | High | Manager | 10 min |
| Database failover | High | Director | 5 min |
| Deployment rollback | Medium | Lead | 15 min |

### Approval Request Format
```json
{
  "incident_id": "inc_uuid_123",
  "action": "Kill long-running queries",
  "risk_level": "medium",
  "requester": "auto-executor-agent",
  "required_level": "lead",
  "timeout_minutes": 15,
  "summary": {
    "what": "Terminate 3 queries blocking connection pool",
    "why": "Database connection pool at 97% capacity",
    "impact": "May interrupt non-critical transactions",
    "rollback": "Automatic connection pool recovery"
  },
  "approvers": ["user_lead_1", "user_lead_2"],
  "created_at": "2026-08-26T14:25:35Z"
}
```

---

## HALLUCINATION PREVENTION

### Structured Output Enforcement
```python
from pydantic import BaseModel, Field

class RemediationAction(BaseModel):
    """Enforce strict structure for remediation"""
    action_id: str = Field(..., description="Unique ID")
    title: str = Field(..., description="Action title")
    steps: List[str] = Field(..., description="Execution steps")
    risk_level: str = Field(..., description="low/medium/high")
    estimated_time: int = Field(..., description="Minutes")
    pre_checks: List[str] = Field(..., description="Validation before")
    post_checks: List[str] = Field(..., description="Validation after")

# Parse with strict validation
parser = PydanticOutputParser(pydantic_object=RemediationAction)
action = parser.parse(llm_output)  # Raises error if invalid
```

### Confidence Scoring
```
Action Confidence Thresholds:
  - Root cause: >= 0.75 for execution recommendation
  - Remediation: >= 0.80 for auto-execution (low-risk)
  - Remediation: >= 0.60 for approval recommendation (high-risk)
  
Below threshold:
  - Flag for manual review
  - Escalate to human SRE
  - Log low confidence incidents for analysis
```

### Evidence-Based Reasoning
```
Every finding must have:
  - Source reference (log, metric, runbook)
  - Timestamp
  - Confidence score
  - Alternative explanations (if applicable)

Example:
  "Root Cause: Database connection pool exhaustion (confidence: 0.88)"
  Evidence:
    - Logs: pg_stat_activity shows 485/500 connections (confidence: 0.95)
    - Metrics: Connection pool spike at 14:23:45 (confidence: 0.95)
    - Runbook: Similar incident 2026-08-15 (confidence: 0.90)
    - Deployment: v2.3.4 deployed 15 min before (confidence: 0.92)
```

---

## AUDIT & COMPLIANCE

### Event Audit Trail
```sql
-- Every action logged with full context
INSERT INTO incident_events (
  incident_id,
  event_type,           -- 'remediation_executed'
  agent_name,           -- 'auto_executor_agent'
  tool_call,            -- 'execute_command'
  tool_params,          -- {command: "...", timeout: 300}
  execution_status,     -- 'success', 'failed', 'timeout'
  guardrail_triggered,  -- true if guardrail blocked
  guardrail_name,       -- 'block_destructive_commands'
  approved_by,          -- user_uuid or null
  approval_timestamp,   -- when approved
  created_at            -- when action occurred
)
```

### User Attribution
```
All actions tracked to:
  - User ID (who made decision)
  - Agent name (what action)
  - Timestamp (when)
  - IP address (where from)
  - Reason/notes (why)

No anonymous actions allowed.
```

### Compliance Reports
```
Available reports:
  - All remediation actions (with approvals)
  - All guardrail violations
  - Approval audit trail
  - User activity log
  - Incident resolution audit

All exportable for compliance audits.
```

---

## GUARDRAIL TYPES

### Type 1: Blockers
```
Conditions: NEVER execute if:
  - Command in blocked list
  - Guardrail triggered
  
Action: Reject and escalate
  └─> Log violation
  └─> Notify SRE
  └─> Update incident: ESCALATED
```

### Type 2: Approvals
```
Conditions: Require approval if:
  - Risk level >= medium
  - High-risk command
  - Policy violation potential
  
Action: Create approval request
  └─> Notify approvers
  └─> Wait for response
  └─> Timeout → escalate
```

### Type 3: Validations
```
Conditions: Always check:
  - Pre-flight checks pass
  - System in valid state
  - No conflicting operations
  
Action: Validate and proceed
  └─> Log validation results
  └─> Abort if validation fails
```

---

## MONITORING SAFETY

### Safety Metrics
```
Track:
  - Guardrail violations (count, types)
  - Approval request metrics (volume, approval rate)
  - Command execution success rate
  - False positive rate
  - Escalation frequency
  
Alerts:
  - Any guardrail violations → immediate alert
  - Approval timeout > 15 min → escalate
  - Execution failures > 30% → manual review
```

### Safety Dashboards
```
Dashboard: Trinetra Safety Monitor
  - Guardrail violations (real-time)
  - Pending approvals (with countdown)
  - Execution failures (by type)
  - User approval patterns
  - Escalation trends
```

---

## DISASTER RECOVERY

### Rollback Capabilities
```
Every remediation action:
  - Generates inverse command (if applicable)
  - Stores rollback state
  - Can be rolled back within 1 hour
  - Requires SRE approval for rollback
  
Example:
  Action: "Scale deployment to 3 replicas"
  Rollback: "Scale deployment to 5 replicas"
```

### Incident Recovery
```
If execution fails:
  1. Post-flight checks fail
  2. Automatic rollback triggered
  3. Incident status set to ESCALATED
  4. SRE notified immediately
  5. Manual recovery initiated
```

---

## COMPLIANCE

### Standards Compliance
```
SOX:   Audit trail, segregation of duties
GDPR:  Data access logging, user attribution
ISO:   Incident response procedures documented
PCI:   Secure execution, access controls
HIPAA: Audit trails, approval workflows
```

### Access Control
```
RBAC:
  - Admin: Full access (rare)
  - Director: Approve high-risk (database, rollback)
  - Manager: Approve medium-risk
  - Lead: Approve low-risk
  - SRE: Execute low-risk only
  - Viewer: Read-only access
```

---

**All guardrails are mandatory and cannot be disabled.**

**See testing guide for guardrail testing procedures.**
