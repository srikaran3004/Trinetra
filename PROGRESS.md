# Trinetra — Progress Log

## Project Status
- **Phase:** 1 — Foundation (completed) ✅
- **Last Updated:** 2026-10-16
- **Branch:** main

---

## Spec Issues Found

| # | Issue | Location |
|---|-------|----------|
| SI-1 | 00_INDEX references docs 10–15, which do not exist | 00_INDEX §Supporting Documents |
| SI-2 | Agent framework undecided: LangGraph (Python IPC) OR Semantic Kernel (.NET) | 01_SPEC §Technical Stack, 02_ARCH §Agent Layer, 05_AGENTS header |
| SI-3 | Frontend framework undecided: React 18+ OR Angular 17+ | 01_SPEC §Frontend, 02_ARCH |
| SI-4 | Messaging: RabbitMQ listed for agent queuing AND Redis listed for state sync; both appear in Docker Compose | 01_SPEC §Data & Storage, 02_ARCH §Agent Orchestration |
| SI-5 | Guardrails framework: "NeMo Guardrails + Custom Policy Engine" — NeMo is Python-only; conflicts with .NET backend | 07_GUARDRAILS header |
| SI-6 | LLM model hardcoded as "Claude 3.5 Sonnet" throughout agent configs; must be a config setting | 05_AGENTS (each agent config block) |
| SI-7 | Auto-Executor sandboxing stated as "Docker container (sandboxed)" but no concrete approach defined | 05_AGENTS §Auto-Executor, 09_ROADMAP Week 5 |
| SI-8 | Success metrics are aspirational targets, not validated numbers | 01_SPEC §Success Criteria |
| SI-9 | 06_DEPLOYMENT references `helm/` charts and `dotnet ef database update` but 03_DATABASE only shows raw SQL migrations; no EF models mentioned | 06_DEPLOY §K8s, 03_DB §Migration |
| SI-10 | 04_API has no auth endpoints (POST /auth/login, POST /auth/refresh) even though JWT auth is required | 04_API §Authentication |
| SI-11 | `runbooks` table has `CHECK (content_embedding IS NOT NULL)` but embeddings are generated asynchronously post-insert — this will block creation | 03_DB runbooks table |
| SI-12 | `users` table has `CHECK ((NOT is_active) OR (api_key_hash IS NOT NULL))` — blocks creating users before assigning an API key | 03_DB users table |
| SI-13 | No WebSocket/SignalR spec for real-time dashboard updates; only "Socket.io" mentioned in frontend section | 02_ARCH §Presentation Layer |

---

## Decisions

| # | Issue | Decision | Reasoning |
|---|-------|----------|-----------|
| D-1 | SI-1: Missing docs 10–15 | Treat as not-yet-written; no blocking dependency | Specs 01–09 are fully self-contained for MVP implementation |
| D-2 | SI-2: Agent framework | **Semantic Kernel (.NET)** | Keeps the entire stack in one language (C#/ASP.NET Core 8); eliminates Python IPC complexity for MVP |
| D-3 | SI-3: Frontend | **Angular 17+** with NgRx | Consistent with enterprise-grade directive in spec |
| D-4 | SI-4: Messaging | **Redis only for MVP** — drop RabbitMQ | Redis streams/pub-sub handle agent coordination; RabbitMQ adds operational overhead without MVP benefit. Agent invocation will use in-process Semantic Kernel orchestration + Redis for distributed state |
| D-5 | SI-5: Guardrails | **Custom C# policy engine** only | NeMo is Python; implementing a custom `GuardrailService` in .NET gives identical guarantees with no cross-language hop |
| D-6 | SI-6: Model name | **Config setting `Ai:ModelId` in appsettings.json**; never hardcoded | Default value: `claude-sonnet-4-5` (current Sonnet). Overridable per environment |
| D-7 | SI-7: Sandboxing | **Process-level sandbox**: Executor runs commands via `System.Diagnostics.Process` with strict env isolation, no shell expansion, and a whitelist-only command set. Docker-in-Docker deferred to post-MVP | Whitelist approach is safer than blacklist; process isolation is sufficient for read-only diagnostic commands allowed in MVP |
| D-8 | SI-8: Metrics | Mark all success criteria as **"aspirational targets — not validated"** in PLAN.md | Honest labeling; validated in testing phase |
| D-9 | SI-9: DB migrations | **EF Core 8 Code-First** migrations; DB schema from 03_DESIGN used as ground truth for entity models | `dotnet ef database update` then works as documented in 06_DEPLOY |
| D-10 | SI-10: Auth endpoints | Add `POST /api/auth/login` and `POST /api/auth/refresh` to API | Required for JWT flow; modeled on standard ASP.NET Core identity patterns |
| D-11 | SI-11: Embedding constraint | Remove NOT NULL check from `content_embedding`; generate embedding async after insert | Allows runbook creation without blocking on embedding generation |
| D-12 | SI-12: Users API key | Remove the CHECK constraint; API key assigned post-creation via separate endpoint | Allows user creation workflow |
| D-13 | SI-13: Real-time | **ASP.NET Core SignalR** (not Socket.io) | Native .NET; Angular has first-class `@microsoft/signalr` client |
| D-14 | User Workflow Instruction | **Do not check git status upon session continuation** | Rely purely on PROGRESS.md and PLAN.md to determine implementation state, as commits/pushes may not occur every time |

---

## Task Log

| Date | Phase | Task | Status | Notes |
|------|-------|------|--------|-------|
| 2026-10-16 | Setup | Read all 9 spec docs | ✅ Done | |
| 2026-10-16 | Setup | Create PROGRESS.md | ✅ Done | |
| 2026-10-16 | Setup | Create PLAN.md | ✅ Done | |
| 2026-10-16 | Phase 1 | 1.1 Scaffold solution & project structure | ✅ Done | Trinetra.sln, 6 projects, .env.example, gitignore |
| 2026-10-16 | Phase 1 | 1.2 Docker Compose local infra | ✅ Done | postgres (pgvector), redis, elasticsearch, kibana, jaeger |
| 2026-10-16 | Phase 1 | 1.3 Database schema & EF migrations | ✅ Done | 8 entities, ApplicationDbContext, InitialSchema, seed data, unit tests |
| 2026-10-16 | Phase 1 | 1.4 Core Incident & Event API | ✅ Done | Incident CRUD, webhook (Prometheus/DataDog), audit trail enforcement, integration tests |
| 2026-10-16 | Phase 1 | 1.5 Auth Endpoints (JWT & RBAC) | ✅ Done | /login, /refresh, 15m/7d expiry, PBKDF2 hashing, unit & integration tests |
| 2026-10-16 | Phase 1 | 1.6 Approvals, Runbooks, Dashboard + Swagger | ✅ Done | Approvals, Runbooks CRUD, Dashboard summary/metrics, OpenAPI at /swagger |
| 2026-10-16 | Phase 1 | 1.7 Observability baseline | ✅ Done | Serilog (Elasticsearch), OpenTelemetry (Jaeger), Prometheus /metrics, /health |
| 2026-10-16 | Phase 1 | 1.8 Angular skeleton app | ✅ Done | Angular 17, NgRx, Material, SignalR IncidentHubService, proxy, dashboard/incidents/approvals |
| 2026-10-16 | Phase 1 | 1.9 GitHub Actions CI pipeline | ✅ Done | .github/workflows/ci.yml with .NET 8, tests + coverage, Angular production build |

---

## Blockers

_None currently._

---

## Architecture Snapshot (Decided)

```
Stack:
  Backend:    ASP.NET Core 8 (C# 12), Minimal APIs + Controllers
  Agents:     Microsoft Semantic Kernel 1.x
  Frontend:   Angular 17+ / NgRx / Angular Material
  Database:   PostgreSQL 15 + pgvector (EF Core 8 Code-First)
  Cache:      Redis 7 (StackExchange.Redis)
  Messaging:  Redis Streams (agent coordination) — no RabbitMQ
  Real-time:  ASP.NET Core SignalR
  LLM:        Configured via Ai:ModelId in appsettings (default: claude-sonnet-4-5)
  Guardrails: Custom C# GuardrailService (no NeMo)
  Sandbox:    Process-level with command whitelist
  Tests:      xUnit, Moq, TestContainers
  Local infra: Docker Compose
  Observability: Serilog → Elasticsearch, OpenTelemetry, Prometheus
```
