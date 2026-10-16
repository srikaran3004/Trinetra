# Trinetra — Implementation Plan

> **Stack:** ASP.NET Core 8 · Semantic Kernel · Angular 17 · PostgreSQL 15+pgvector · Redis · SignalR · xUnit  
> **Timeline:** 5 phases (≈5 weeks)  
> **Metrics marked ⚠️ are aspirational targets, not validated numbers.**

---

## Phase 1 — Foundation (Week 1)

**Goal:** Working repository structure, Docker infrastructure, database schema with EF Core migrations, core CRUD API, auth endpoints, health check, Swagger, and a skeleton Angular app.

### Tasks

#### 1.1 — Repository & Project Structure
- [x] Read all specs and record issues/decisions in PROGRESS.md  
- [x] Initialise solution: `Trinetra.sln` with projects:
  - `src/Trinetra.API` — ASP.NET Core 8 Minimal/Controller API  
  - `src/Trinetra.Core` — Domain entities, interfaces, enums  
  - `src/Trinetra.Infrastructure` — EF Core DbContext, repositories, Redis, SK wiring  
  - `src/Trinetra.Agents` — Semantic Kernel agent base classes (stubs for Phase 2)  
  - `tests/Trinetra.UnitTests` — xUnit unit tests  
  - `tests/Trinetra.IntegrationTests` — xUnit integration tests (TestContainers)  
- [x] `.env.example` with all placeholders; `.gitignore` covers `.env`  
- [x] Commit: `phase1: scaffold solution structure`

#### 1.2 — Docker Compose (local infra)
- [x] `docker-compose.yml` with services:
  - `postgres:15` with `pgvector` extension enabled  
  - `redis:7-alpine`  
  - `elasticsearch:8.x` (single-node, xpack security off)  
  - `kibana:8.x`  
  - `jaeger` (all-in-one, for OpenTelemetry traces)  
- [x] Health-check `docker-compose ps` passes for all services  
- [x] Commit: `phase1: add docker-compose infrastructure`

#### 1.3 — Database Schema (EF Core Code-First)
- [x] Entity models in `Trinetra.Core` mirroring all 8 tables from spec 03:
  - `Incident`, `IncidentEvent`, `Runbook`, `LogsSnapshot`, `MetricsSnapshot`, `IncidentHistory`, `ApprovalPolicy`, `User`  
- [x] `ApplicationDbContext` in `Trinetra.Infrastructure` with pgvector support  
- [x] EF Core migrations: `InitialSchema`, `AddPgvector`  
- [x] Seed migration: 2 default `ApprovalPolicy` rows, 1 seed admin user  
- [x] `dotnet ef database update` succeeds against Docker Postgres  
- [x] Unit tests: entity constraints, navigation property configs  
- [x] Commit: `phase1: database schema and EF core migrations`

#### 1.4 — Core API — Incidents & Events
- [x] `POST /api/v1/incidents` — create incident  
- [x] `GET /api/v1/incidents` — list with filtering (status, severity, pagination)  
- [x] `GET /api/v1/incidents/{id}` — detail with events embedded  
- [x] `PATCH /api/v1/incidents/{id}` — update (assign, notes)  
- [x] `GET /api/v1/incidents/{id}/events` — paginated audit trail  
- [x] `POST /api/v1/webhooks/alerts` — Prometheus & DataDog formats  
- [x] All writes produce an `IncidentEvent` row (audit trail ground rule)  
- [x] Integration tests: each endpoint happy path + error cases  
- [x] Commit: `phase1: incident CRUD and webhook endpoints`

#### 1.5 — Auth Endpoints (JWT)
- [x] `POST /api/v1/auth/login` — validates credentials, returns JWT + refresh token  
- [x] `POST /api/v1/auth/refresh` — rotates refresh token  
- [x] JWT bearer middleware configured; 15-min access token, 7-day refresh  
- [x] RBAC middleware: roles `admin | lead | manager | sre | viewer`  
- [x] Unit tests: token generation, RBAC attribute enforcement  
- [x] Commit: `phase1: JWT auth and RBAC middleware`

#### 1.6 — Approvals & Runbooks API (stubs)
- [x] `GET /api/v1/approvals/pending`  
- [x] `POST /api/v1/approvals/{id}/approve`  
- [x] `POST /api/v1/approvals/{id}/deny`  
- [x] `GET/POST /api/v1/runbooks` — CRUD  
- [x] `GET /api/v1/runbooks/{id}`  
- [x] `GET /api/v1/dashboard/summary`  
- [x] `GET /api/v1/dashboard/metrics`  
- [x] Swagger / OpenAPI auto-generated and accessible at `/swagger`  
- [x] Commit: `phase1: approvals, runbooks, dashboard stubs + swagger`

#### 1.7 — Observability Baseline
- [x] Serilog → Elasticsearch structured logging  
- [x] OpenTelemetry traces wired to Jaeger  
- [x] Prometheus `/metrics` endpoint  
- [x] `GET /health` liveness + readiness checks  
- [x] Commit: `phase1: observability - serilog, otel, prometheus`

#### 1.8 — Angular Skeleton App
- [x] `src/frontend/` — Angular 17 workspace, NgRx, Angular Material  
- [x] Proxy config → `http://localhost:5000`  
- [x] Stub pages: Dashboard, Incidents list, Incident detail, Approval queue  
- [x] SignalR client wired (hub exists but no events yet)  
- [x] `npm run build` succeeds; `npm test` passes stub tests  
- [x] Commit: `phase1: angular skeleton app`

#### 1.9 — CI Pipeline (GitHub Actions)
- [x] `.github/workflows/ci.yml`:
  - `dotnet build`  
  - `dotnet test` (unit + integration with TestContainers)  
  - `npm run build` (frontend)  
  - Coverage ≥ 85% gate (Coverlet)  
- [x] Commit: `phase1: github actions CI pipeline`

### Success Criteria
- [x] `docker-compose up -d` starts all services with no errors  
- [x] `dotnet ef database update` applies all migrations  
- [x] `dotnet run` — API starts and Swagger is accessible  
- [x] `GET /health` returns 200  
- [x] All core incident endpoints return correct HTTP status codes  
- [x] Auth endpoints issue and validate JWT  
- [x] Angular app loads at `localhost:4200`  
- [x] `dotnet test` passes; coverage ≥ 85%  
- [x] CI pipeline green  

---

## Phase 2 — Agent Infrastructure (Week 2)

**Goal:** Semantic Kernel integrated; Incident Coordinator Agent functional; SignalR hub emitting real-time events; Redis state management; full agent lifecycle logging.

### Tasks

#### 2.1 — Semantic Kernel Setup
- [x] `Trinetra.Agents` project: SK NuGet packages  
- [x] `AgentBase` abstract class: SK kernel factory, tool registration, audit-event emission  
- [x] `AgentConfiguration` loaded from `appsettings.json` (model, temperature, maxTokens per agent)  
- [x] Unit tests: kernel creation, config loading  
- [x] Commit: `phase2: semantic kernel base agent infrastructure`

#### 2.2 — Incident Coordinator Agent
- [x] `IncidentCoordinatorAgent`: implements full workflow state machine  
- [x] Tools: `create_incident`, `update_incident_status`, `invoke_agent`, `wait_for_agents`, `escalate_incident`  
- [x] Async parallel agent fan-out + fan-in  
- [x] Every state transition writes `IncidentEvent` row  
- [x] Integration tests: coordinator drives stub child agents  
- [x] Commit: `phase2: incident coordinator agent state machine`

#### 2.3 — Redis State Management
- [x] `IIncidentStateStore` backed by Redis hashes (incident state, agent status)  
- [x] Redis Streams for agent task queuing (replaces RabbitMQ)  
- [x] `IRedisStreamConsumer` base for agent workers  
- [x] Unit tests: state store CRUD, stream publish/consume  
- [x] Commit: `phase2: redis state management and streams`

#### 2.4 — SignalR Real-Time Hub
- [ ] `IncidentHub` — clients subscribe to incident groups  
- [ ] Events: `IncidentCreated`, `IncidentStatusChanged`, `AgentActionCompleted`, `ApprovalRequired`  
- [ ] Angular `IncidentHubService` receives events and dispatches NgRx actions  
- [ ] Integration test: hub emits events on DB write  
- [ ] Commit: `phase2: signalr hub and angular real-time integration`

#### 2.5 — OpenTelemetry Agent Tracing
- [ ] Activity spans per agent execution (start, tool call, completion)  
- [ ] Trace context propagated across async boundaries  
- [ ] Integration test: trace visible in Jaeger  
- [ ] Commit: `phase2: opentelemetry agent tracing`

### Success Criteria
- [ ] Incident Coordinator starts on webhook alert  
- [ ] Coordinator invokes stub child agents in parallel  
- [ ] All state transitions write audit events  
- [ ] SignalR events visible in browser console on status change  
- [ ] Redis correctly tracks incident + agent state  
- [ ] Traces appear in Jaeger UI  
- [ ] `dotnet test` passes  

---

## Phase 3 — Core Investigation Agents (Week 3)

**Goal:** Log Analyzer, Metrics Querier, Runbook Retriever (RAG) agents all operational; parallel investigation completes in < 2 minutes.

### Tasks

#### 3.1 — Log Analyzer Agent
- [ ] Elasticsearch client integration (`NEST` / `Elastic.Clients.Elasticsearch`)  
- [ ] Tools: `query_logs`, `extract_stack_traces`, `detect_anomalies`, `correlate_with_deployment`  
- [ ] Writes `LogsSnapshot` row; writes `IncidentEvent` row  
- [ ] Unit tests: log parsing, pattern extraction  
- [ ] Integration test: agent runs against real Elasticsearch (TestContainers)  
- [ ] Commit: `phase3: log analyzer agent`

#### 3.2 — Metrics Querier Agent
- [ ] Prometheus HTTP client integration  
- [ ] Tools: `query_metrics`, `get_baseline_metrics`, `detect_saturation`, `correlate_with_incident_time`  
- [ ] Writes `MetricsSnapshot` row; writes `IncidentEvent` row  
- [ ] Unit + integration tests  
- [ ] Commit: `phase3: metrics querier agent`

#### 3.3 — Runbook Retriever Agent (RAG)
- [ ] Anthropic embedding generation (or text-embedding-3-small fallback) stored in `content_embedding`  
- [ ] Async embedding job on runbook create/update  
- [ ] Tools: `search_runbooks_by_embedding`, `search_runbooks_by_keyword`, `search_incident_history`, `extract_remediation_steps`  
- [ ] pgvector cosine similarity query with `<=>` operator  
- [ ] Unit tests: embedding pipeline, similarity search  
- [ ] Integration tests: RAG returns ranked results  
- [ ] Commit: `phase3: runbook retriever agent with RAG`

#### 3.4 — Parallel Investigation Orchestration
- [ ] Coordinator fans out to all three agents simultaneously; awaits Task.WhenAll  
- [ ] Timeout handling: 5-minute total; partial results used on timeout  
- [ ] E2E test: all three agents complete in < 2 minutes on test data  
- [ ] Commit: `phase3: parallel investigation orchestration and timeout handling`

### Success Criteria
- [ ] Log Analyzer produces `LogsSnapshot` with error patterns  
- [ ] Metrics Querier produces `MetricsSnapshot` with saturation data  
- [ ] Runbook Retriever returns ≥ 1 matching runbook via RAG  
- [ ] Parallel execution completes in < 2 minutes (⚠️ aspirational)  
- [ ] Audit events written for every agent action  
- [ ] `dotnet test` passes  

---

## Phase 4 — Analysis & Remediation (Week 4)

**Goal:** Root Cause Analyzer, Remediation Proposer, Approval Gate Agent; Guardrail engine (tests FIRST); end-to-end approval workflow.

### Tasks

#### 4.1 — Guardrail Engine (tests written first)
- [ ] `GuardrailService` with three guard types: Blocker, Approval-Required, Validation  
- [ ] Blocked command list from spec 07  
- [ ] Confidence threshold checks (≥0.75 root cause, ≥0.80 auto-exec, ≥0.60 approval)  
- [ ] Rate limiting: max 5 concurrent remediations, max 20 cmd/min  
- [ ] **Tests written before implementation:** `GuardrailTests` covering all blocked commands + approval matrix  
- [ ] Commit: `phase4: guardrail engine tests`  
- [ ] Commit: `phase4: guardrail engine implementation`

#### 4.2 — Root Cause Analyzer Agent
- [ ] Receives findings from all three Phase 3 agents  
- [ ] Evidence chaining + confidence scoring  
- [ ] Classification categories: DEPLOYMENT / RESOURCE / DATABASE / EXTERNAL / CONFIG / CASCADE / HARDWARE / UNKNOWN  
- [ ] Updates `incidents.root_cause`, `root_cause_category`, `root_cause_confidence`  
- [ ] Writes `IncidentEvent` row  
- [ ] Unit tests: category classification, confidence scoring, evidence chain  
- [ ] Commit: `phase4: root cause analyzer agent`

#### 4.3 — Remediation Proposer Agent
- [ ] Generates 2–3 options with risk levels  
- [ ] Pre/post-flight check lists per option  
- [ ] Routes to Approval Gate (medium/high risk) or directly to Executor (low risk)  
- [ ] Updates `incidents.proposed_remediation`, `remediation_risk_level`  
- [ ] Writes `IncidentEvent`  
- [ ] Unit tests: risk classification, option generation  
- [ ] Commit: `phase4: remediation proposer agent`

#### 4.4 — Approval Gate Agent & Workflow
- [ ] Creates `approval_requests` (stored in `incident_events` with `requires_approval=true`)  
- [ ] Sends placeholder notifications (Slack webhook + email stub)  
- [ ] 15-minute timeout → auto-escalate  
- [ ] `POST /api/v1/approvals/{id}/approve` and `/deny` fully wired  
- [ ] SignalR push: `ApprovalRequired` event to dashboard  
- [ ] Integration test: full approve lifecycle  
- [ ] Commit: `phase4: approval gate agent and workflow`

#### 4.5 — Dashboard — Analysis & Approval UI
- [ ] Angular: Approval queue component with countdown timer  
- [ ] Incident detail page shows root cause, confidence score, evidence  
- [ ] Approve / Deny buttons with confirmation dialog  
- [ ] Real-time updates via SignalR  
- [ ] Commit: `phase4: dashboard analysis and approval UI`

### Success Criteria
- [ ] Guardrail tests pass before any execution code  
- [ ] All blocked commands rejected; logged as guardrail violation  
- [ ] Root cause identified at ≥ 85% confidence on test scenarios (⚠️ aspirational)  
- [ ] Approval workflow works end-to-end in UI  
- [ ] Timeout escalates correctly  
- [ ] `dotnet test` passes  

---

## Phase 5 — Execution & Polish (Week 5)

**Goal:** Auto-Executor Agent with process-level sandboxing; complete dashboard; full test suite at ≥ 85% coverage; production-ready Docker config; demo-ready.

### Tasks

#### 5.1 — Auto-Executor Agent (sandboxed)
- [ ] Whitelist of allowed command categories (DB queries, k8s scale, deployment rollback commands)  
- [ ] `ProcessExecutor` — `System.Diagnostics.Process`, no shell expansion, env-isolated  
- [ ] Pre-flight → execute → post-flight; rollback on post-flight failure  
- [ ] Every step writes `IncidentEvent` (including guardrail checks)  
- [ ] Rate limits enforced by `GuardrailService`  
- [ ] Unit tests: whitelist enforcement, process isolation, rollback trigger  
- [ ] Integration tests: full execute lifecycle on safe test commands  
- [ ] Commit: `phase5: auto-executor agent with process sandbox`

#### 5.2 — Rollback Mechanism
- [ ] Every executed action stores inverse command  
- [ ] `POST /api/v1/incidents/{id}/rollback` endpoint (SRE role required)  
- [ ] Approval required for rollback  
- [ ] Unit + integration tests  
- [ ] Commit: `phase5: rollback mechanism`

#### 5.3 — Dashboard — Complete
- [ ] Real-time incident timeline (agent steps visible as they happen)  
- [ ] Safety monitor: guardrail violation count, pending approvals  
- [ ] Historical analytics charts (MTTR trend, root cause distribution)  
- [ ] Agent tracing visualization panel  
- [ ] Commit: `phase5: complete dashboard UI`

#### 5.4 — Full Test Suite Pass
- [ ] Unit test coverage ≥ 85% (Coverlet report)  
- [ ] Integration tests: all agent pipelines  
- [ ] E2E test: full incident lifecycle (webhook → investigation → RCA → approval → execution → resolved)  
- [ ] Guardrail suite: all blocked commands  
- [ ] Security tests: 401 on missing token, 403 on wrong role, SQL injection rejection  
- [ ] Commit: `phase5: full test suite`

#### 5.5 — Load & Performance
- [ ] Simulate 50 concurrent incidents via test harness  
- [ ] API response time < 500ms (⚠️ aspirational)  
- [ ] Agent queue processing < 5-min delay under load (⚠️ aspirational)  
- [ ] Commit: `phase5: load test results and tuning`

#### 5.6 — Production Readiness
- [ ] `docker-compose.prod.yml` with proper resource limits  
- [ ] `.env.example` complete and accurate  
- [ ] `README.md` — how to run, test, and contribute  
- [ ] Helm chart stubs (`helm/trinetra/`) for future K8s  
- [ ] Commit: `phase5: production docker config and helm stubs`

### Success Criteria
- [ ] All 7 agents fully functional end-to-end  
- [ ] Auto-Executor respects guardrails on every run  
- [ ] Test coverage ≥ 85%  
- [ ] MTTR < 15 minutes on test scenarios (⚠️ aspirational)  
- [ ] Audit trail 100% complete for every incident  
- [ ] Dashboard fully functional with real-time updates  
- [ ] Demo-ready: alert → resolution in < 5 minutes on demo data  

---

## Overall Success Criteria (End of Project)

| Criterion | Target | Status | Note |
|-----------|--------|--------|------|
| All 7 agents implemented | ✅ | - | |
| MTTR < 15 min (testing) | ⚠️ aspirational | - | Not yet validated |
| Auto-resolution rate > 60% | ⚠️ aspirational | - | Not yet validated |
| Test coverage ≥ 85% | Hard target | - | Enforced in CI |
| Zero guardrail bypasses | Hard target | - | Non-negotiable |
| Audit trail 100% complete | Hard target | - | Non-negotiable |
| Agent RCA accuracy > 85% | ⚠️ aspirational | - | Not yet validated |
| Dashboard fully functional | Hard target | - | |
| Demo-ready | Hard target | - | |
