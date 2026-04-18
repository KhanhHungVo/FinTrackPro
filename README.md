# FinTrackPro

Personal finance tracking application with budgeting, expense management, and Telegram notifications. Fully responsive across mobile, tablet, and desktop.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![React 19](https://img.shields.io/badge/React-19-61DAFB?logo=react)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)
![Keycloak](https://img.shields.io/badge/Keycloak-24-4D9BFF?logo=keycloak)
![Telegram](https://img.shields.io/badge/Telegram-Bot-26A5E4?logo=telegram)

## Architecture

Clean Architecture monorepo: React SPA → .NET 10 REST API → PostgreSQL (local + Render production; SQL Server optional), with Keycloak or Auth0 for authentication and Hangfire for background jobs. See [docs/architecture.md](docs/architecture.md) for the full diagram and layer descriptions.

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| Docker Desktop | Latest | Required for local PostgreSQL + Keycloak containers |
| .NET SDK | 10.0 | |
| Node.js | 22+ | |

## Quick Start

**Full Docker (no local .NET toolchain required):**

```bash
git clone <repo-url> && cd FinTrackPro
docker compose up --build

cd frontend/fintrackpro-ui
cp .env.example .env && npm install && npm run dev
```

**Hybrid (recommended — hot reload + debugger):**

```bash
docker compose up -d postgres keycloak

cd backend
dotnet ef database update --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API
dotnet run --project src/FinTrackPro.API

cd frontend/fintrackpro-ui && npm install && npm run dev
```

Open `http://localhost:5173`. Keycloak is provisioned automatically — log in with `admin@fintrackpro.dev` / `Admin1234!`.

See [docs/dev-setup.md](docs/dev-setup.md) for all modes including local PostgreSQL and Render deployment.

## Repository Structure

```
FinTrackPro/
├── backend/
│   ├── src/
│   │   ├── FinTrackPro.API/            # ASP.NET Core — controllers, middleware, DI
│   │   ├── FinTrackPro.Application/    # CQRS via MediatR, DTOs, validators
│   │   ├── FinTrackPro.Domain/         # Entities, value objects, domain events
│   │   ├── FinTrackPro.Infrastructure/ # EF Core, repositories, external clients
│   │   └── FinTrackPro.BackgroundJobs/ # Hangfire job definitions
│   ├── tests/
│   │   ├── FinTrackPro.Domain.UnitTests/
│   │   ├── FinTrackPro.Application.UnitTests/
│   │   ├── FinTrackPro.Api.IntegrationTests/
│   │   └── Tests.Common/               # Shared infrastructure (Testcontainers, fakes, builders)
│   ├── Dockerfile                      # Runtime image (aspnet — lean, no SDK tools)
│   └── Dockerfile.migrator             # Init container — runs EF migrations then exits
├── frontend/
│   └── fintrackpro-ui/                 # React 19 + Vite SPA (Feature-Sliced Design)
├── infra/
│   ├── docker/
│   │   └── keycloak-realm.json         # Auto-imported on first `docker compose up`
│   └── terraform/                      # Terraform IaC — Render API + frontend (TF Cloud state)
├── scripts/
│   ├── e2e-local.sh                    # Mint E2E token + run Playwright (Git Bash / WSL)
│   └── api-e2e-local.sh               # Newman API E2E suite (Git Bash / WSL)
├── docs/                               # Reference documentation
├── .github/workflows/ci.yml            # CI pipeline
└── docker-compose.yml
```

## Deployment — Render.com

Both services (API + frontend) are deployed to Render via **Terraform** (`infra/terraform/`). The `render.yaml` Blueprint remains as a fallback for manual one-click deploys.

See [docs/render-terraform-deploy.md](docs/render-terraform-deploy.md) for the full deploy guide (Terraform + render.yaml fallback + migration strategies).

> **Migrations** must be applied from your local machine before any schema-changing deploy — the production Docker image has no .NET SDK.

## Documentation

| Document | Description |
|---|---|
| [docs/dev-setup.md](docs/dev-setup.md) | End-to-end dev setup — Docker, hybrid, local PostgreSQL, Render |
| [docs/render-terraform-deploy.md](docs/render-terraform-deploy.md) | Render deployment — Terraform (primary) + render.yaml (fallback) + migration strategies |
| [docs/auth-setup.md](docs/auth-setup.md) | IAM provider setup — Keycloak and Auth0, switching providers |
| [docs/architecture.md](docs/architecture.md) | System architecture, layers, data flow, infrastructure |
| [docs/api-spec.md](docs/api-spec.md) | REST API endpoints and request/response schemas |
| [docs/database.md](docs/database.md) | Database schema, tables, relationships, migration commands |
| [docs/roadmap.md](docs/roadmap.md) | Feature phases and release milestones |
| [backend/tests/README.md](backend/tests/README.md) | Backend test projects, integration test setup, CI filter strategy |
| [docs/postman/api-e2e-test-cases.md](docs/postman/api-e2e-test-cases.md) | Newman API E2E test inventory — all 31 requests and 44 assertions |
| [docs/postman/api-e2e-plan.md](docs/postman/api-e2e-plan.md) | Newman API E2E suite — collection structure, CI job, GitHub secrets |
| [docs/security-hardening.md](docs/security-hardening.md) | Security hardening guide — rate limiting, headers, HTTPS, input validation, JWT storage, XSS, quotas |
| [docs/auth0-config-as-code-plan.md](docs/auth0-config-as-code-plan.md) | Plan: Auth0 config-as-code via `auth0 deploy` CLI (not yet implemented) |
| [backend/README.md](backend/README.md) | Backend developer reference |
| [frontend/fintrackpro-ui/README.md](frontend/fintrackpro-ui/README.md) | Frontend developer reference |

## Manual Setup

**Telegram Bot** *(optional — notifications are silently skipped without it)*

Create a bot via [@BotFather](https://t.me/BotFather), then set the token — do **not** commit it:
```bash
dotnet user-secrets set "Telegram:BotToken" "your-token" --project backend/src/FinTrackPro.API
# or: export Telegram__BotToken="your-token"
```

**Keycloak** — zero-touch for dev. The `fintrackpro` realm auto-imports from `infra/docker/keycloak-realm.json` on first `docker compose up`. See [docs/auth-setup.md](docs/auth-setup.md) for custom config or production rotation.

**Auth0** — requires one-time dashboard setup (API, SPA app, M2M app, roles, post-login Action). See [docs/auth-setup.md](docs/auth-setup.md#auth0-cloud-iam). Switch with `IdentityProvider:Provider = "auth0"` and `VITE_AUTH_PROVIDER=auth0`.

**EF Core migrations** — Full Docker runs them automatically via the `migrator` init container. For all other modes, run manually:
```bash
cd backend
dotnet ef database update --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API
```
See [docs/database.md](docs/database.md) for adding new migrations.

**Playwright E2E tests** — requires Docker (Keycloak + PostgreSQL for local dev), the API, and the frontend dev server running. Then:
```bash
bash scripts/e2e-local.sh      # Git Bash / WSL
```
See [docs/dev-setup.md — Mode E](docs/dev-setup.md#mode-e--running-playwright-e2e-tests-locally) for full steps and troubleshooting.

**Newman API E2E tests** — hits a real running API with a real Keycloak-issued JWT. Requires Docker (`postgres` + `keycloak`) and the API on :5018:
```bash
npm install -g newman          # one-time
bash scripts/api-e2e-local.sh
```
See [docs/postman/api-e2e-plan.md](docs/postman/api-e2e-plan.md) for collection structure, CI job, and required GitHub secrets.
