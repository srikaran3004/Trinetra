# Trinetra API Documentation
## Complete REST API Reference

**Version:** 1.0  
**Base URL:** `https://api.trinetra.local/v1`  
**Authentication:** Bearer Token (JWT)  
**Rate Limit:** 1000 requests/hour  
**Response Format:** JSON

---

## AUTHENTICATION

### JWT Token Format
```
Authorization: Bearer {JWT_TOKEN}
```

### Token Example
```json
{
  "sub": "user_uuid_123",
  "email": "user@company.com",
  "role": "sre",
  "iat": 1693046400,
  "exp": 1693050000
}
```

---

## CORE ENDPOINTS

### Incidents

#### POST /incidents
Create a new incident (usually from alert webhook)

```
Request:
{
  "alert_id": "prometheus_alert_123",
  "source": "prometheus",
  "title": "High Error Rate in auth-api",
  "description": "Error rate exceeded 10% threshold",
  "severity": "high",
  "affected_service": "auth-api",
  "alert_data": {
    "error_rate": 0.15,
    "threshold": 0.10,
    "duration_seconds": 300
  }
}

Response (201 Created):
{
  "incident_id": "inc_uuid_123",
  "alert_id": "prometheus_alert_123",
  "status": "triggered",
  "severity": "high",
  "created_at": "2026-08-26T14:23:45Z"
}
```

#### GET /incidents
List incidents with filtering

```
Query Parameters:
  status=investigating,resolved (comma-separated)
  severity=critical,high
  limit=50
  offset=0
  sort_by=triggered_at (asc/desc)

Response (200 OK):
{
  "incidents": [
    {
      "id": "inc_uuid_123",
      "title": "High Error Rate in auth-api",
      "severity": "high",
      "status": "investigating",
      "triggered_at": "2026-08-26T14:23:45Z",
      "root_cause": null,
      "assigned_to": "user_sre_123"
    }
  ],
  "total": 150,
  "limit": 50,
  "offset": 0
}
```

#### GET /incidents/{incident_id}
Get detailed incident information

```
Response (200 OK):
{
  "id": "inc_uuid_123",
  "alert_id": "prometheus_alert_123",
  "title": "High Error Rate in auth-api",
  "severity": "high",
  "status": "awaiting_approval",
  "affected_services": ["auth-api"],
  "affected_hosts": ["auth-api-pod-1", "auth-api-pod-2"],
  "root_cause": "Database connection pool exhaustion",
  "root_cause_confidence": 0.88,
  "proposed_remediation": "Kill long-running queries blocking pool",
  "remediation_risk_level": "medium",
  "triggered_at": "2026-08-26T14:23:45Z",
  "started_investigating_at": "2026-08-26T14:23:50Z",
  "resolved_at": null,
  "events": [
    {
      "event_type": "analysis_started",
      "agent_name": "Incident Coordinator",
      "timestamp": "2026-08-26T14:23:50Z"
    }
  ]
}
```

#### PATCH /incidents/{incident_id}
Update incident (assign, add notes, etc.)

```
Request:
{
  "assigned_to": "user_uuid",
  "status": "investigating",
  "notes": "Priority incident, customer impacted"
}

Response (200 OK):
{
  "id": "inc_uuid_123",
  "updated_at": "2026-08-26T14:30:00Z"
}
```

---

### Events & Audit Trail

#### GET /incidents/{incident_id}/events
Retrieve complete audit trail for an incident

```
Query Parameters:
  event_type=remediation_executed (optional filter)
  limit=100
  offset=0

Response (200 OK):
{
  "events": [
    {
      "id": "evt_uuid_1",
      "incident_id": "inc_uuid_123",
      "event_type": "analysis_started",
      "agent_name": "Incident Coordinator",
      "action_description": "Starting multi-agent investigation",
      "timestamp": "2026-08-26T14:23:50Z"
    },
    {
      "id": "evt_uuid_2",
      "event_type": "remediation_proposed",
      "agent_name": "Remediation Proposer",
      "tool_call": "generate_remediation_options",
      "execution_status": "success",
      "requires_approval": true,
      "timestamp": "2026-08-26T14:25:35Z"
    }
  ],
  "total": 12,
  "limit": 100
}
```

---

### Approvals

#### POST /approvals/{approval_request_id}/approve
Approve a high-risk action

```
Request:
{
  "approved_by_user_id": "user_sre_123",
  "notes": "Looks good, proceed with caution",
  "scheduled_for": "2026-08-26T14:26:00Z" (optional)
}

Response (200 OK):
{
  "approval_id": "appr_uuid",
  "status": "approved",
  "approved_at": "2026-08-26T14:26:00Z",
  "remediation_will_execute_at": "2026-08-26T14:26:00Z"
}
```

#### POST /approvals/{approval_request_id}/deny
Deny a remediation action

```
Request:
{
  "denied_by_user_id": "user_sre_123",
  "reason": "Too risky during peak hours"
}

Response (200 OK):
{
  "approval_id": "appr_uuid",
  "status": "denied",
  "denied_at": "2026-08-26T14:26:00Z",
  "incident_status": "escalated"
}
```

#### GET /approvals/pending
List pending approvals

```
Query Parameters:
  limit=20
  sort_by=created_at (desc)

Response (200 OK):
{
  "approvals": [
    {
      "id": "appr_uuid_1",
      "incident_id": "inc_uuid_123",
      "action": "Kill long-running database queries",
      "risk_level": "medium",
      "approval_level": "lead",
      "timeout_at": "2026-08-26T14:40:35Z",
      "created_at": "2026-08-26T14:25:35Z"
    }
  ],
  "total": 3
}
```

---

### Runbooks

#### POST /runbooks
Create new runbook

```
Request:
{
  "title": "Database Connection Pool Recovery",
  "description": "Steps to recover from connection pool exhaustion",
  "category": "database",
  "tags": ["database", "postgres", "connection-pool"],
  "applicable_services": ["auth-api", "user-api"],
  "content": "## Overview\nThis runbook...",
  "remediation_steps": [
    {
      "step": 1,
      "action": "Check pool status",
      "command": "SELECT * FROM pg_stat_activity;"
    }
  ],
  "estimated_time_minutes": 10
}

Response (201 Created):
{
  "id": "runbook_uuid",
  "created_at": "2026-08-26T14:30:00Z"
}
```

#### GET /runbooks
List runbooks with search/filter

```
Query Parameters:
  tags=database,postgres
  services=auth-api
  search=connection%20pool
  limit=20

Response (200 OK):
{
  "runbooks": [
    {
      "id": "runbook_uuid_1",
      "title": "Database Connection Pool Recovery",
      "category": "database",
      "tags": ["database", "postgres"],
      "applicable_services": ["auth-api"],
      "estimated_time_minutes": 10,
      "is_active": true,
      "version": 2
    }
  ],
  "total": 45
}
```

#### GET /runbooks/{runbook_id}
Get runbook details

```
Response (200 OK):
{
  "id": "runbook_uuid_1",
  "title": "Database Connection Pool Recovery",
  "description": "Steps to recover from connection pool exhaustion",
  "content": "## Overview\n...",
  "remediation_steps": [...]
}
```

---

### Dashboard & Analytics

#### GET /dashboard/summary
Get dashboard summary data

```
Response (200 OK):
{
  "active_incidents": 5,
  "critical_incidents": 2,
  "avg_mttr_minutes": 12.5,
  "auto_resolution_rate": 0.68,
  "incidents_this_week": 35,
  "most_common_root_cause": "deployment",
  "recent_incidents": [...]
}
```

#### GET /dashboard/metrics
Get performance metrics

```
Response (200 OK):
{
  "incident_trends": {
    "labels": ["Mon", "Tue", "Wed", "Thu", "Fri"],
    "data": [12, 15, 8, 10, 14]
  },
  "severity_distribution": {
    "critical": 5,
    "high": 15,
    "medium": 25,
    "low": 10
  },
  "resolution_time_distribution": {
    "under_5_min": 20,
    "5_to_15_min": 15,
    "15_to_30_min": 8,
    "over_30_min": 2
  }
}
```

---

## WEBHOOK ENDPOINTS

### Alert Ingestion Webhook

#### POST /webhooks/alerts

**Prometheus Format:**
```json
{
  "alerts": [
    {
      "status": "firing",
      "labels": {
        "alertname": "HighErrorRate",
        "service": "auth-api",
        "severity": "high"
      },
      "annotations": {
        "description": "Error rate is 15%, threshold is 10%",
        "summary": "High error rate detected"
      },
      "startsAt": "2026-08-26T14:23:45Z",
      "endsAt": "0001-01-01T00:00:00Z"
    }
  ]
}
```

**DataDog Format:**
```json
{
  "alert": {
    "id": 123456,
    "metric": "trace.web.request.errors",
    "title": "High Error Rate",
    "metric_query": "avg:trace.web.request.errors{service:auth-api}",
    "threshold_value": 10,
    "current_value": 15,
    "timestamp": "2026-08-26T14:23:45Z"
  }
}
```

**Response:**
```json
{
  "incident_id": "inc_uuid_123",
  "status": "triggered",
  "investigation_starting": true
}
```

---

## ERROR HANDLING

### Standard Error Response
```json
{
  "error": {
    "code": "INCIDENT_NOT_FOUND",
    "message": "Incident with ID 'invalid_id' not found",
    "status": 404,
    "timestamp": "2026-08-26T14:30:00Z",
    "request_id": "req_uuid_123"
  }
}
```

### Common Error Codes
```
400 BAD_REQUEST       - Invalid request format
401 UNAUTHORIZED      - Missing or invalid token
403 FORBIDDEN         - Insufficient permissions
404 NOT_FOUND         - Resource not found
409 CONFLICT          - Resource already exists
429 RATE_LIMITED      - Too many requests
500 INTERNAL_ERROR    - Server error
503 SERVICE_UNAVAIL   - Service temporarily down
```

---

## RATE LIMITING

### Rate Limit Headers
```
X-RateLimit-Limit:       1000
X-RateLimit-Remaining:   999
X-RateLimit-Reset:       1693046400
```

### Limits by Endpoint
```
GET /incidents:              100 req/min
POST /incidents:             10 req/min
GET /approvals/pending:      50 req/min
POST /approvals/*/approve:   5 req/min
POST /webhooks/alerts:       1000 req/min (unlimited for alerts)
```

---

## EXAMPLE CURL COMMANDS

### Create Incident
```bash
curl -X POST https://api.trinetra.local/v1/incidents \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "alert_id": "prometheus_123",
    "source": "prometheus",
    "title": "High Error Rate",
    "severity": "high",
    "affected_service": "auth-api"
  }'
```

### Approve Remediation
```bash
curl -X POST https://api.trinetra.local/v1/approvals/appr_uuid/approve \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "approved_by_user_id": "user_sre_123",
    "notes": "Approved for execution"
  }'
```

### List Incidents
```bash
curl -X GET "https://api.trinetra.local/v1/incidents?status=investigating&limit=20" \
  -H "Authorization: Bearer $TOKEN"
```

---

**See frontend and agent specifications for integrated documentation.**
