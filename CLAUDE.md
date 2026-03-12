# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Backend (.NET 10)
```bash
cd backend
dotnet restore
dotnet build
dotnet run --project src/FinTrackPro.API
dotnet test                                        # all tests
dotnet test --filter "FullyQualifiedName~<Test>"  # single test

# EF Core migrations
dotnet ef migrations add <Name> --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API
dotnet ef database update --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API
```

### Frontend (React 19 + Vite)
```bash
cd frontend/fintrackpro-ui
npm install
npm run dev      # dev server with HMR
npm run build    # type-check + production build
npm run lint     # ESLint
npm test         # Vitest
```

### Infrastructure
```bash
docker compose up -d sqlserver keycloak   # hybrid dev (recommended)
docker compose up --build                 # full docker
```

## Architecture

### Backend — Clean Architecture
Strict layer isolation with inward-only dependencies:

- **Domain** (`FinTrackPro.Domain`) — entities, value objects, repository interfaces. No external dependencies.
- **Application** (`FinTrackPro.Application`) — CQRS via MediatR (commands/queries/handlers), DTOs, FluentValidation validators, MediatR pipeline behaviors (validation, logging).
- **Infrastructure** (`FinTrackPro.Infrastructure`) — EF Core (Code-First, migrations here), repository implementations, Keycloak JWT, Telegram.Bot, Skender.Stock.Indicators.
- **API** (`FinTrackPro.API`) — ASP.NET Core controllers, DI registration, middleware (exception handling → maps domain exceptions to HTTP codes), Scalar API docs.
- **BackgroundJobs** (`FinTrackPro.BackgroundJobs`) — Hangfire job definitions: `MarketSignalJob` (every 4h), `BudgetOverrunJob` (daily).

DTOs use explicit `operator` conversions instead of AutoMapper. `ICurrentUserService` extracts the authenticated user from the JWT claim. Add new features as a command/query + handler pair in Application, then expose via a thin controller action.

### Frontend — Feature-Sliced Design (FSD)
Strict top-down layer hierarchy (upper layers may only import from lower):
```
app → pages → widgets → features → entities → shared
```
Server state lives in **React Query** (TanStack). Client-only state (auth, UI flags) lives in **Zustand**. HTTP calls go through an Axios instance that injects the Keycloak Bearer token and redirects on 401.

### Auth
Keycloak 24 is the OIDC provider. The API validates JWT Bearer tokens; the frontend uses the Keycloak JS adapter. Realm: `fintrackpro`.

## Key Configuration

| Variable | Where |
|---|---|
| `ConnectionStrings__DefaultConnection` | `appsettings.json` / env |
| `Keycloak__Authority` | `appsettings.json` (e.g. `http://localhost:8080/realms/fintrackpro`) |
| `Keycloak__Audience` | `appsettings.json` (e.g. `fintrackpro-api`) |
| `Telegram__BotToken` | env var only |
| `VITE_API_BASE_URL` | `frontend/fintrackpro-ui/.env` |
| `VITE_KEYCLOAK_URL/REALM/CLIENT_ID` | `frontend/fintrackpro-ui/.env` |

Copy `frontend/fintrackpro-ui/.env.example` → `.env` before first run.

## Ports (local hybrid dev)
| Service | Port |
|---|---|
| API | 5018 |
| Frontend (Vite) | 5173 |
| Keycloak | 8080 |
| SQL Server | 1433 |
| API (Docker) | 5000 |

## Docs
- `docs/architecture.md` — layer descriptions and design decisions
- `docs/dev-setup.md` — hybrid vs full-Docker setup
- `docs/api-spec.md` — REST endpoints and schemas
- `docs/database.md` — schema, tables, relationships
