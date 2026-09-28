# Project Guidelines & Rules: Trinetra

## 1. GitHub Integration & Code Pushing (MANDATORY RULE)
- **Always use Way 1 (GitHub API Direct Push)** to push files to the repository (`https://github.com/srikaran3004/Trinetra`).
- **DO NOT ask the user every time** how to push or ask to install Git/Docker.
- To push changes, use the automated sync utility:
  ```bash
  python scripts/github_sync.py "<commit message>"
  ```
  This script uses the GitHub Personal Access Token configured in `~/.gemini/config/mcp_config.json` and creates atomic Git commits directly on branch `main` via the GitHub Git Database API.
- Do not rely on local `git` or Docker for repository operations unless explicitly instructed by the user.

## 2. Project Overview & Architecture
- **Project**: Trinetra (Autonomous IT Incident Response Platform with Multi-Agent Orchestration)
- **Specifications Location**: `Product Specifications/`
  - Refer to `00_TRINETRA_INDEX_AND_GUIDE.md` and `TRINETRA_START_HERE.md` for project roadmap and architecture.
  - Follow `07_TRINETRA_GUARDRAILS_SAFETY.md` for safety guardrails and approval workflows.
  - Follow `03_TRINETRA_DATABASE_DESIGN.md` for data models.
  - Follow `04_TRINETRA_API_DOCUMENTATION.md` for endpoints.
  - Follow `05_TRINETRA_AGENT_SPECIFICATIONS.md` for all 7 agent definitions.

## 3. Technology Stack & Local Environment
- **Backend**: ASP.NET Core 8.0+ (.NET 10 SDK is installed locally).
- **Agents**: Semantic Kernel or Python-based agent services (Python 3.14 is installed locally).
- **Database**: PostgreSQL 15+ with `pgvector`.
- **Frontend**: React / TypeScript dashboard.
