# Project Guidelines & Rules: Trinetra

## 1. GitHub Integration & Autonomous Code Pushing (MANDATORY RULE)
- **Always use Way 1 (GitHub API Direct Push)** to push files to the repository (`https://github.com/srikaran3004/Trinetra`).
- **Autonomous Execution / Zero Friction**:
  - **DO NOT ask the user for permission or approval** before pushing code or committing files.
  - Whenever the user asks to push code / changes, or when changes are ready to be pushed: **immediately execute** the push.
  - Automatically formulate a clear, descriptive, and appropriate commit message summarizing the changes.
  - Run the automated sync utility directly:
    ```bash
    python scripts/github_sync.py "<appropriate descriptive commit message>"
    ```
  - Report the commit SHA and a summary of pushed changes back to the user upon completion.
- **Never prompt the user** to install Git, Docker, or ask which push method they prefer. Way 1 is the permanent default.

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
