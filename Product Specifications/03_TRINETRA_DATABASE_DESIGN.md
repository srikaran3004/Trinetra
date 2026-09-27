# Trinetra Database Design Document
## Complete Database Schema & Data Models

**Version:** 1.0  
**Database:** PostgreSQL 15+ with pgvector extension  
**Last Updated:** August 26, 2026

---

## TABLE OF CONTENTS

1. [Database Overview](#database-overview)
2. [Core Tables](#core-tables)
3. [Relationships](#relationships)
4. [Indexes & Performance](#indexes--performance)
5. [Migration Strategy](#migration-strategy)
6. [Backup & Recovery](#backup--recovery)

---

## DATABASE OVERVIEW

### PostgreSQL Extensions Required
```sql
-- Enable UUID generation
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Enable vector similarity search (for RAG)
CREATE EXTENSION IF NOT EXISTS "vector";

-- Enable JSON functions
CREATE EXTENSION IF NOT EXISTS "jsonb";

-- Enable full-text search
CREATE EXTENSION IF NOT EXISTS "pg_trgm";
```

### Database Configuration
```
Database Name:      trinetra_prod
Encoding:           UTF-8
Locale:             en_US.UTF-8
Timezone:           UTC (all timestamps)
Max Connections:    100
Shared Buffers:     256MB (production: 4GB)
Effective Cache:    1GB (production: 16GB)
```

---

## CORE TABLES

### 1. `incidents` Table
**Purpose:** Main incident records

```sql
CREATE TABLE incidents (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  alert_id VARCHAR(255) UNIQUE NOT NULL,
  source VARCHAR(50) NOT NULL, -- 'prometheus', 'datadog', 'pagerduty'
  
  -- Basic Info
  title VARCHAR(500) NOT NULL,
  description TEXT,
  
  -- Classification
  severity VARCHAR(20) NOT NULL CHECK (severity IN ('critical', 'high', 'medium', 'low')),
  status VARCHAR(50) NOT NULL DEFAULT 'triggered' CHECK (
    status IN ('triggered', 'investigating', 'analysis_complete', 
               'awaiting_approval', 'executing', 'monitoring', 'resolved', 'escalated')
  ),
  
  -- Affected Resources
  affected_services TEXT[] NOT NULL DEFAULT '{}',
  affected_hosts TEXT[] NOT NULL DEFAULT '{}',
  affected_components TEXT[] DEFAULT '{}',
  
  -- Analysis Results
  root_cause TEXT,
  root_cause_category VARCHAR(50), -- 'deployment', 'resource', 'database', etc.
  root_cause_confidence DECIMAL(3,2) CHECK (root_cause_confidence >= 0 AND root_cause_confidence <= 1),
  
  -- Remediation
  proposed_remediation TEXT,
  remediation_risk_level VARCHAR(20) CHECK (remediation_risk_level IN ('low', 'medium', 'high')),
  remediation_estimated_time_minutes INT CHECK (remediation_estimated_time_minutes > 0),
  
  -- Timeline
  triggered_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  started_investigating_at TIMESTAMP,
  analysis_completed_at TIMESTAMP,
  remediation_started_at TIMESTAMP,
  resolved_at TIMESTAMP,
  escalated_at TIMESTAMP,
  
  -- Assignment
  assigned_to UUID REFERENCES users(id) ON DELETE SET NULL,
  assigned_team VARCHAR(100),
  
  -- Metadata
  created_by_agent VARCHAR(100),
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  
  CONSTRAINT check_resolved_after_triggered CHECK (resolved_at IS NULL OR resolved_at >= triggered_at)
);

-- Indexes
CREATE INDEX idx_incidents_status ON incidents(status);
CREATE INDEX idx_incidents_severity ON incidents(severity);
CREATE INDEX idx_incidents_triggered_at ON incidents(triggered_at DESC);
CREATE INDEX idx_incidents_assigned_to ON incidents(assigned_to);
CREATE INDEX idx_incidents_root_cause_category ON incidents(root_cause_category);
CREATE INDEX idx_incidents_alert_id ON incidents(alert_id);
CREATE INDEX idx_incidents_affected_services ON incidents USING GIN(affected_services);
```

### 2. `incident_events` Table
**Purpose:** Complete audit trail of all actions

```sql
CREATE TABLE incident_events (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  incident_id UUID NOT NULL REFERENCES incidents(id) ON DELETE CASCADE,
  
  -- Event Classification
  event_type VARCHAR(50) NOT NULL,
  agent_name VARCHAR(100),
  
  -- Event Details
  action_description TEXT,
  tool_call VARCHAR(255),
  tool_params JSONB,
  tool_result TEXT,
  
  -- Execution Status
  execution_status VARCHAR(20) DEFAULT 'pending' CHECK (
    execution_status IN ('pending', 'in_progress', 'success', 'failed', 'timeout')
  ),
  execution_error TEXT,
  execution_time_ms INT,
  
  -- Guardrails & Safety
  guardrail_triggered BOOLEAN DEFAULT FALSE,
  guardrail_name VARCHAR(255),
  guardrail_reason TEXT,
  
  -- Approval Info (if applicable)
  requires_approval BOOLEAN DEFAULT FALSE,
  approved_by UUID REFERENCES users(id) ON DELETE SET NULL,
  approval_timestamp TIMESTAMP,
  approval_notes TEXT,
  approval_timeout_minutes INT,
  
  -- Audit Trail
  created_by_user_id UUID REFERENCES users(id) ON DELETE SET NULL,
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  
  CONSTRAINT check_approval_after_request CHECK (
    (NOT requires_approval) OR (approval_timestamp IS NULL OR approval_timestamp >= created_at)
  )
);

-- Indexes for quick audit trail access
CREATE INDEX idx_incident_events_incident_id ON incident_events(incident_id);
CREATE INDEX idx_incident_events_event_type ON incident_events(event_type);
CREATE INDEX idx_incident_events_created_at ON incident_events(created_at DESC);
CREATE INDEX idx_incident_events_agent_name ON incident_events(agent_name);
CREATE INDEX idx_incident_events_execution_status ON incident_events(execution_status);
CREATE INDEX idx_incident_events_guardrail ON incident_events(guardrail_triggered);
```

### 3. `runbooks` Table
**Purpose:** Operational runbooks repository with embeddings for RAG

```sql
CREATE TABLE runbooks (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  
  -- Metadata
  title VARCHAR(500) NOT NULL,
  description TEXT,
  category VARCHAR(100),
  tags TEXT[] DEFAULT '{}',
  
  -- Applicability
  applicable_services TEXT[] DEFAULT '{}',
  applicable_hosts TEXT[] DEFAULT '{}',
  applicable_error_patterns TEXT[] DEFAULT '{}',
  
  -- Content
  content TEXT NOT NULL, -- Markdown format
  remediation_steps JSONB, -- Structured steps
  estimated_time_minutes INT CHECK (estimated_time_minutes > 0),
  prerequisites TEXT[],
  risks_and_warnings TEXT[],
  
  -- Vector Embedding for RAG
  content_embedding vector(1536), -- OpenAI/Claude embeddings (1536 dims)
  
  -- Versioning
  version INT NOT NULL DEFAULT 1,
  created_by UUID REFERENCES users(id) ON DELETE SET NULL,
  updated_by UUID REFERENCES users(id) ON DELETE SET NULL,
  
  -- Status
  is_active BOOLEAN DEFAULT TRUE,
  is_deprecated BOOLEAN DEFAULT FALSE,
  
  -- Timestamps
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  
  CONSTRAINT check_embedding_not_null CHECK (content_embedding IS NOT NULL)
);

-- Indexes
CREATE INDEX idx_runbooks_title ON runbooks USING GIN(to_tsvector('english', title));
CREATE INDEX idx_runbooks_tags ON runbooks USING GIN(tags);
CREATE INDEX idx_runbooks_services ON runbooks USING GIN(applicable_services);
CREATE INDEX idx_runbooks_is_active ON runbooks(is_active);
CREATE INDEX idx_runbooks_content_embedding ON runbooks USING ivfflat (content_embedding vector_cosine_ops);
```

### 4. `logs_snapshot` Table
**Purpose:** Snapshot of logs queried during investigation

```sql
CREATE TABLE logs_snapshot (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  incident_id UUID NOT NULL REFERENCES incidents(id) ON DELETE CASCADE,
  
  -- Query Details
  source VARCHAR(50) NOT NULL, -- 'elasticsearch', 'splunk', 'datadog'
  query_executed VARCHAR(1000),
  time_range_start TIMESTAMP,
  time_range_end TIMESTAMP,
  
  -- Raw Data
  log_data JSONB NOT NULL,
  log_count INT NOT NULL DEFAULT 0,
  
  -- Analysis
  error_patterns TEXT[],
  stack_traces TEXT[],
  anomalies_detected TEXT[],
  severity_distribution JSONB, -- {error: 50, warning: 100, info: 200}
  
  -- Metadata
  retrieved_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  analyzed_at TIMESTAMP
);

CREATE INDEX idx_logs_snapshot_incident ON logs_snapshot(incident_id);
CREATE INDEX idx_logs_snapshot_source ON logs_snapshot(source);
CREATE INDEX idx_logs_snapshot_retrieved_at ON logs_snapshot(retrieved_at DESC);
```

### 5. `metrics_snapshot` Table
**Purpose:** Snapshot of metrics queried during investigation

```sql
CREATE TABLE metrics_snapshot (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  incident_id UUID NOT NULL REFERENCES incidents(id) ON DELETE CASCADE,
  
  -- Query Details
  source VARCHAR(50) NOT NULL, -- 'prometheus', 'datadog', 'newrelic'
  query_executed VARCHAR(1000),
  time_range_start TIMESTAMP NOT NULL,
  time_range_end TIMESTAMP NOT NULL,
  
  -- Raw Data
  metrics_data JSONB NOT NULL,
  metric_names TEXT[] DEFAULT '{}',
  
  -- Analysis
  anomalies_detected TEXT[],
  resource_saturation JSONB, -- {cpu: 0.92, memory: 0.88, disk: 0.45}
  baseline_deviations JSONB, -- {cpu_spike: 0.27, memory_spike: 0.16}
  
  -- Metadata
  retrieved_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  analyzed_at TIMESTAMP
);

CREATE INDEX idx_metrics_snapshot_incident ON metrics_snapshot(incident_id);
CREATE INDEX idx_metrics_snapshot_source ON metrics_snapshot(source);
CREATE INDEX idx_metrics_snapshot_retrieved_at ON metrics_snapshot(retrieved_at DESC);
```

### 6. `incident_history` Table
**Purpose:** Historical incidents for pattern learning and similarity search

```sql
CREATE TABLE incident_history (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  
  -- Summary
  title VARCHAR(500),
  description TEXT,
  root_cause VARCHAR(500),
  root_cause_category VARCHAR(50),
  remediation_action TEXT,
  remediation_time_minutes INT,
  
  -- Classification
  severity VARCHAR(20),
  affected_services TEXT[],
  tags TEXT[],
  
  -- Embedding for similarity search
  history_embedding vector(1536),
  
  -- Timeline
  occurred_at TIMESTAMP,
  resolved_at TIMESTAMP,
  resolution_time_minutes INT CHECK (resolution_time_minutes > 0),
  
  -- Reference
  original_incident_id UUID,
  
  -- Metadata
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_incident_history_root_cause ON incident_history(root_cause_category);
CREATE INDEX idx_incident_history_embedding ON incident_history USING ivfflat (history_embedding vector_cosine_ops);
CREATE INDEX idx_incident_history_services ON incident_history USING GIN(affected_services);
CREATE INDEX idx_incident_history_resolved_at ON incident_history(resolved_at DESC);
```

### 7. `approval_policies` Table
**Purpose:** Policy rules for determining when approval is required

```sql
CREATE TABLE approval_policies (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  
  -- Policy Definition
  rule_name VARCHAR(255) NOT NULL UNIQUE,
  description TEXT,
  priority INT NOT NULL DEFAULT 100,
  
  -- Matching Conditions
  applies_to_commands TEXT[] DEFAULT '{}',
  applies_to_services TEXT[] DEFAULT '{}',
  min_risk_level VARCHAR(20), -- 'low', 'medium', 'high'
  applies_to_incident_severity TEXT[], -- 'critical', 'high', etc.
  
  -- Approval Requirements
  requires_approval BOOLEAN DEFAULT FALSE,
  approval_level VARCHAR(50), -- 'lead', 'manager', 'director'
  escalation_timeout_minutes INT DEFAULT 15,
  can_execute_during_hours VARCHAR(100), -- 'business_hours', 'always'
  
  -- Execution Constraints
  max_concurrent_executions INT DEFAULT 1,
  max_per_hour INT DEFAULT 10,
  
  -- Status
  is_active BOOLEAN DEFAULT TRUE,
  created_by UUID REFERENCES users(id) ON DELETE SET NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_approval_policies_active ON approval_policies(is_active);
CREATE INDEX idx_approval_policies_rule_name ON approval_policies(rule_name);
CREATE INDEX idx_approval_policies_services ON approval_policies USING GIN(applies_to_services);
```

### 8. `users` Table
**Purpose:** User management and RBAC

```sql
CREATE TABLE users (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  
  -- Basic Info
  username VARCHAR(255) UNIQUE NOT NULL,
  email VARCHAR(255) UNIQUE NOT NULL,
  full_name VARCHAR(255),
  
  -- RBAC
  role VARCHAR(50) NOT NULL CHECK (role IN ('admin', 'lead', 'manager', 'sre', 'viewer')),
  team VARCHAR(100),
  department VARCHAR(100),
  
  -- Notification Preferences
  slack_user_id VARCHAR(255),
  slack_channel VARCHAR(255),
  email_notifications BOOLEAN DEFAULT TRUE,
  slack_notifications BOOLEAN DEFAULT TRUE,
  approval_notifications BOOLEAN DEFAULT TRUE,
  
  -- API Access
  api_key_hash VARCHAR(255),
  api_key_last_used TIMESTAMP,
  
  -- Status & Audit
  is_active BOOLEAN DEFAULT TRUE,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  last_login TIMESTAMP,
  last_password_change TIMESTAMP,
  
  CONSTRAINT check_api_key_required_for_access CHECK (
    (NOT is_active) OR (api_key_hash IS NOT NULL)
  )
);

CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_role ON users(role);
CREATE INDEX idx_users_is_active ON users(is_active);
CREATE UNIQUE INDEX idx_users_api_key_hash ON users(api_key_hash) WHERE is_active = TRUE;
```

---

## RELATIONSHIPS

### Entity Relationship Diagram
```
┌──────────────┐
│   incidents  │◄────────┐
│              │         │
│  - id (PK)   │         │
│  - alert_id  │    1 to many
│  - status    │         │
│  - severity  │    ┌────┴──────────────┐
└──────────────┘    │                   │
       ▲            ▼                   ▼
       │      ┌──────────────┐    ┌──────────────┐
       │      │incident_events│   │logs_snapshot │
       │      │               │   │              │
       └──────┤ - incident_id │   │- incident_id │
              │ - event_type  │   └──────────────┘
              └──────────────┘
                     ▲
                     │
                     │ 1 to many
                     │
              ┌──────────────┐
              │ metrics_snap │
              │              │
              │-incident_id  │
              └──────────────┘

Runbooks:
┌──────────────┐
│  runbooks    │ (standalone, referenced by agents)
│              │
│  - id (PK)   │
│  - embedding │ (for RAG similarity search)
└──────────────┘

Users:
┌──────────────┐
│   users      │ (referenced by incidents, events, policies)
│              │
│  - id (PK)   │
│  - role      │
│  - email     │
└──────────────┘
```

---

## INDEXES & PERFORMANCE

### Performance Optimization Strategies

**1. Query Optimization**
```sql
-- Frequently used queries have dedicated indexes
-- Example: Finding incidents by status
SELECT * FROM incidents WHERE status = 'investigating';
CREATE INDEX idx_incidents_status ON incidents(status);

-- Composite indexes for multi-column queries
SELECT * FROM incidents 
WHERE status = 'investigating' 
  AND severity = 'critical'
  AND triggered_at > NOW() - INTERVAL '24 hours';
CREATE INDEX idx_incidents_status_severity_time ON incidents(status, severity, triggered_at DESC);
```

**2. Full-Text Search**
```sql
-- Search runbooks by title and content
SELECT * FROM runbooks 
WHERE to_tsvector('english', title || ' ' || description) @@ plainto_tsquery('database timeout');
```

**3. Vector Similarity Search (RAG)**
```sql
-- Find similar runbooks using embeddings
SELECT * FROM runbooks 
ORDER BY content_embedding <=> incident_embedding LIMIT 5;
-- <=> is cosine distance operator
```

**4. JSON Queries**
```sql
-- Query nested JSON data
SELECT * FROM incident_events 
WHERE tool_params->>'service' = 'auth-api';
```

---

## MIGRATION STRATEGY

### Initial Schema Creation
```bash
# 1. Apply migrations in order
psql -U postgres -d trinetra_prod < migrations/001_create_tables.sql

# 2. Add extensions
psql -U postgres -d trinetra_prod < migrations/002_create_extensions.sql

# 3. Create indexes
psql -U postgres -d trinetra_prod < migrations/003_create_indexes.sql

# 4. Seed initial data
psql -U postgres -d trinetra_prod < migrations/004_seed_initial_data.sql
```

### Using Flyway for Migrations
```
migrations/
├── V1__Initial_schema.sql
├── V2__Add_pgvector_extension.sql
├── V3__Create_indexes.sql
└── V4__Seed_policies.sql
```

---

## BACKUP & RECOVERY

### Backup Strategy
```bash
# Daily full backup
pg_dump -Fc trinetra_prod > backup_$(date +%Y%m%d).dump

# Hourly incremental backups (WAL archiving)
wal_level = archive
archive_mode = on
archive_command = 'cp %p /mnt/backup/wal_archive/%f'
```

### Recovery Procedure
```bash
# Full restore
pg_restore -d trinetra_prod backup_20260826.dump

# Point-in-time recovery
recovery_target_timeline = 'latest'
recovery_target_time = '2026-08-26 14:25:00'
```

---

## CONCLUSION

This database design supports:
- ✅ Complete audit trail (incident_events table)
- ✅ RAG for intelligent retrieval (embeddings)
- ✅ Policy enforcement (approval_policies)
- ✅ Scalability (proper indexing)
- ✅ Data integrity (constraints)
- ✅ High performance (optimized queries)

---

**See implementation guides for SQL scripts to create all tables.**
