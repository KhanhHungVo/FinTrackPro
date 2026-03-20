# FinTrackPro

Personal finance tracking application with budgeting, expense management, and Telegram notifications.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![React 19](https://img.shields.io/badge/React-19-61DAFB?logo=react)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)
![Keycloak](https://img.shields.io/badge/Keycloak-24-4D9BFF?logo=keycloak)
![Telegram](https://img.shields.io/badge/Telegram-Bot-26A5E4?logo=telegram)

## Architecture

Clean Architecture monorepo: React SPA → .NET 10 REST API → SQL Server (local) / Azure SQL (production), with Keycloak or Auth0 handling authentication and Hangfire running background jobs (e.g. scheduled Telegram notifications). See [docs/architecture.md](docs/architecture.md) for the full diagram and layer descriptions.

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| Docker Desktop | Latest | Required for local SQL Server + Keycloak containers |
| .NET SDK | 10.0 | |
| Node.js | 22+ | |
| Azure subscription | — | Required for Azure SQL (production / cloud dev) |

## Quick Start

**Full Docker (no local .NET toolchain required):**

```bash
# 1. Clone
git clone <repo-url>
cd FinTrackPro

# 2. Start everything — migrations run automatically via the migrator init container
docker compose up --build

# 3. Start the frontend (not in compose)
cd frontend/fintrackpro-ui
cp .env.example .env
npm install
npm run dev
```

**Hybrid (recommended for development — hot reload + debugger):**

```bash
# 1. Start infrastructure only
docker compose up -d sqlserver keycloak

# 2. Apply migrations (first time)
cd backend
dotnet ef database update --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API

# 3. Run the API locally
dotnet run --project src/FinTrackPro.API

# 4. Run the frontend
cd frontend/fintrackpro-ui && npm install && npm run dev
```

Open `http://localhost:5173` in your browser.

> Keycloak is provisioned automatically — no manual realm setup needed. Log in with `admin@fintrackpro.dev` / `Admin1234!` or register a new account.

## Repository Structure

```
FinTrackPro/
├── backend/
│   ├── src/
│   │   ├── FinTrackPro.API/           # ASP.NET Core — controllers, middleware, DI
│   │   ├── FinTrackPro.Application/   # Use cases, MediatR handlers, DTOs
│   │   ├── FinTrackPro.Domain/        # Entities, value objects, domain events
│   │   ├── FinTrackPro.Infrastructure/# EF Core, repositories, external clients
│   │   └── FinTrackPro.BackgroundJobs/# Hangfire job definitions
│   ├── tests/
│   │   ├── FinTrackPro.Domain.UnitTests/
│   │   ├── FinTrackPro.Application.UnitTests/
│   │   ├── FinTrackPro.Api.IntegrationTests/
│   │   └── Tests.Common/              # Shared test infrastructure (Testcontainers, fakes, builders)
│   ├── Dockerfile                     # Runtime image (aspnet — lean, no SDK tools)
│   └── Dockerfile.migrator            # Init container — runs EF migrations then exits
├── frontend/
│   └── fintrackpro-ui/                # React 19 + Vite SPA (Feature-Sliced Design)
├── infra/
│   └── docker/
│       └── keycloak-realm.json        # Auto-imported on first `docker compose up`
├── docs/                              # Reference documentation
├── .github/workflows/ci.yml           # CI pipeline
└── docker-compose.yml
```

## Documentation

| Document | Description |
|---|---|
| [docs/dev-setup.md](docs/dev-setup.md) | End-to-end dev setup — Docker, hybrid mode, migrations |
| [docs/auth-setup.md](docs/auth-setup.md) | IAM provider setup — Keycloak manual config, Auth0 dashboard, switching providers |
| [docs/api-spec.md](docs/api-spec.md) | REST API endpoints and request/response schemas |
| [docs/architecture.md](docs/architecture.md) | System architecture, layers, data flow |
| [docs/database.md](docs/database.md) | Database schema, tables, relationships |
| [docs/roadmap.md](docs/roadmap.md) | Planned features and release milestones |
| [backend/README.md](backend/README.md) | Backend developer reference |
| [frontend/fintrackpro-ui/README.md](frontend/fintrackpro-ui/README.md) | Frontend developer reference |

## Manual Setup

**Telegram Bot** *(optional — notifications are silently skipped without it)*
1. Create a bot via [@BotFather](https://t.me/BotFather) and copy the token.
2. Set the token via User Secrets or env var — do **not** commit it:
   ```bash
   dotnet user-secrets set "Telegram:BotToken" "your-token" --project backend/src/FinTrackPro.API
   # or: export Telegram__BotToken="your-token"
   ```

**EF Core migrations**

- **Full Docker:** handled automatically by the `migrator` init container on every `docker compose up --build`.
- **Hybrid mode (local SQL Server):** run once after `docker compose up -d sqlserver`:
  ```bash
  cd backend
  dotnet ef database update --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API
  ```
- **Azure SQL:** set the connection string via User Secrets or env var first, then run the same command:
  ```bash
  dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<azure-sql-string>" --project src/FinTrackPro.API
  cd backend && dotnet ef database update --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API
  ```

**Keycloak** *(zero-touch for dev)*

The `fintrackpro` realm is auto-imported from `infra/docker/keycloak-realm.json` on first `docker compose up`. It includes both clients, audience mapper, roles, self-registration, and a default admin user (`admin@fintrackpro.dev` / `Admin1234!`).

The dev `IdentityProvider__AdminClientSecret` (`dev-secret-change-in-prod`) is pre-set in `appsettings.Development.json` — no environment variable needed for local dev.

> **Production:** rotate the `fintrackpro-api` client secret in Keycloak and set `IdentityProvider__AdminClientSecret` to the new value via environment variable.

For custom Keycloak config (social login, extra users, redirect URIs) see the [manual setup reference](docs/auth-setup.md#manual-setup-reference) in `docs/auth-setup.md`.

**Auth0** *(cloud IAM alternative)*

Requires a one-time dashboard setup: API, SPA app, M2M app, roles, and a post-login Action for role injection. See [docs/auth-setup.md](docs/auth-setup.md#auth0-cloud-iam) for the full guide. Switch with `IdentityProvider:Provider = "auth0"` (backend) and `VITE_AUTH_PROVIDER=auth0` (frontend).

**Azure SQL** *(production database)*

Requires portal provisioning: resource group → logical server → database → firewall rules. See [docs/dev-setup.md — Mode C](docs/dev-setup.md#mode-c--hybrid-dev-against-azure-sql) for the full setup guide and how to connect from local dev machines.
