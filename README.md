# FinTrackPro

Personal finance and trading tracker for managing budgets, expenses, portfolio watchlists, market signals, and notification workflows. It is built as a Clean Architecture monorepo with a .NET API, React SPA, PostgreSQL, Keycloak/Auth0 authentication, and Hangfire background jobs.

![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)
![React 19](https://img.shields.io/badge/React-19-61DAFB?logo=react)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)
![Keycloak](https://img.shields.io/badge/Keycloak-24-4D9BFF?logo=keycloak)
![Telegram](https://img.shields.io/badge/Telegram-Bot-26A5E4?logo=telegram)

## Tech Stack

| Area | Stack |
|---|---|
| Backend | .NET 10, ASP.NET Core, MediatR, EF Core |
| Frontend | React 19, Vite, TypeScript |
| Database | PostgreSQL for local and production; SQL Server optional |
| Authentication | Keycloak for local development; Auth0 supported |
| Background jobs | Hangfire |
| Deployment | Docker, Render, Terraform |

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| Docker Desktop | Latest | Runs PostgreSQL and Keycloak for local development |
| .NET SDK | 10.0 | Runs and debugs the API locally |
| EF Core CLI | Latest | Required for `dotnet ef database update`; install with `dotnet tool install --global dotnet-ef` |
| Node.js | 22+ | Runs the Vite frontend locally |

## Quick Start

This is the recommended local development setup. Docker runs PostgreSQL and Keycloak; the API and frontend run locally for debugging and hot reload.

Start local dependencies:

```bash
docker compose up -d postgres keycloak
```

Run the API:

```bash
cd backend
dotnet ef database update --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API
dotnet run --project src/FinTrackPro.API
```

In a second terminal, run the frontend:

```bash
cd frontend/fintrackpro-ui
cp .env.example .env
npm install
npm run dev
```

The copied `.env` is enough for the default Keycloak setup. No extra secrets are required to start locally.

Useful URLs:

| Service | URL |
|---|---|
| Frontend | `http://localhost:5173` |
| API | `http://localhost:5018` |
| Keycloak | `http://localhost:8080` |

Keycloak is provisioned automatically for local development. Use the seeded development-only login `admin@fintrackpro.dev` / `Admin1234!`.

For full Docker, Auth0, optional integrations, Playwright, Newman, local PostgreSQL, and deployment-oriented workflows, see [docs/guides/dev-setup.md](docs/guides/dev-setup.md). For Keycloak/Auth0 setup details, see [docs/guides/auth-setup.md](docs/guides/auth-setup.md). Never commit secrets, tokens, credentials, or personally identifiable information.

## Development Commands

Backend:

```bash
cd backend
dotnet restore
dotnet build
dotnet test --filter "Category!=Integration"
dotnet run --project src/FinTrackPro.API
```

Frontend:

```bash
cd frontend/fintrackpro-ui
npm install
npm run dev
npm run build
npm run lint
npm run test
```

Integration and E2E test setup is documented in [docs/guides/dev-setup.md](docs/guides/dev-setup.md) and [backend/tests/README.md](backend/tests/README.md).

## Architecture

FinTrackPro uses Clean Architecture with separate API, application, domain, infrastructure, background job, and frontend layers. The main request flow is:

```text
React SPA -> .NET REST API -> Application layer -> Domain model -> EF Core/PostgreSQL
```

See [docs/architecture/overview.md](docs/architecture/overview.md) for the full architecture guide.

## Deployment

Deployments are documented in [docs/guides/render-deploy.md](docs/guides/render-deploy.md). The current production path uses Render with Terraform, with `render.yaml` kept as a fallback.

## Documentation

| Category | Documents |
|---|---|
| Development | [Dev setup](docs/guides/dev-setup.md), [Auth setup](docs/guides/auth-setup.md), [Database rotation](docs/guides/db-rotation.md) |
| Architecture | [Overview](docs/architecture/overview.md), [API spec](docs/architecture/api-spec.md), [Database](docs/architecture/database.md), [Auth](docs/architecture/auth.md), [Background jobs](docs/architecture/background-jobs.md), [UI flows](docs/architecture/ui-flows.md), [Configuration](docs/architecture/configuration.md) |
| Deployment | [Render deploy](docs/guides/render-deploy.md) |
| Testing | [Backend tests](backend/tests/README.md), [Newman API E2E plan](docs/postman/api-e2e-plan.md), [Newman API E2E test cases](docs/postman/api-e2e-test-cases.md) |
| Product | [Features](docs/features.md), [Roadmap](docs/roadmap.md) |
