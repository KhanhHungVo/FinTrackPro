# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Backend (.NET 10)
```bash
cd backend
dotnet restore
dotnet build
dotnet run --project src/FinTrackPro.API
dotnet test                                           # all tests
dotnet test --filter "Category!=Integration"          # unit tests only (no Docker)
dotnet test --filter "Category=Integration"           # integration tests (Docker required)
dotnet test --filter "FullyQualifiedName~<Test>"      # single test

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
- **Application** (`FinTrackPro.Application`) — CQRS via MediatR (commands/queries/handlers), DTOs, FluentValidation validators, MediatR pipeline behaviors: `ValidationBehavior` → `LoggingBehavior` → `EnsureUserBehavior` (auto-provisions `AppUser` on first login).
- **Infrastructure** (`FinTrackPro.Infrastructure`) — EF Core (Code-First, migrations here), repository implementations, Keycloak JWT, `KeycloakClaimsTransformer` (flattens `realm_access.roles` → `ClaimTypes.Role`), Telegram.Bot, Skender.Stock.Indicators.
- **API** (`FinTrackPro.API`) — ASP.NET Core controllers, DI registration, middleware (exception handling → maps domain exceptions to HTTP codes), Scalar API docs.
- **BackgroundJobs** (`FinTrackPro.BackgroundJobs`) — Hangfire job definitions: `MarketSignalJob` (every 4h), `BudgetOverrunJob` (daily), `KeycloakUserSyncJob` (daily — deactivates deleted/disabled Keycloak users in the local DB).

DTOs use explicit `operator` conversions instead of AutoMapper. `ICurrentUserService` extracts the authenticated user from the JWT claim. Add new features as a command/query + handler pair in Application, then expose via a thin controller action.

### Frontend — Feature-Sliced Design (FSD)
Strict top-down layer hierarchy (upper layers may only import from lower):
```
app → pages → widgets → features → entities → shared
```
Server state lives in **React Query** (TanStack). Client-only state (auth, UI flags) lives in **Zustand**. HTTP calls go through an Axios instance that injects the Keycloak Bearer token and redirects on 401.

### Auth
Keycloak 24 is the OIDC provider. The API validates JWT Bearer tokens; the frontend uses the Keycloak JS adapter. Realm: `fintrackpro`.

The realm is **auto-provisioned** from `infra/docker/keycloak-realm.json` on first `docker compose up` (via `--import-realm`). Import is idempotent — skipped if the realm already exists. Default dev credentials: `admin@fintrackpro.dev` / `Admin1234!`.

Users self-register via Keycloak's login page (local accounts, Google, or Azure AD — configured in Keycloak, not the app). The `User` realm role is assigned automatically to every registrant via Keycloak Default Roles; `Admin` is assigned manually. Roles are stored only in Keycloak — never in the database.

## Key Configuration

| Variable | Where |
|---|---|
| `ConnectionStrings__DefaultConnection` | `appsettings.json` / env |
| `Keycloak__Authority` | `appsettings.json` (e.g. `http://localhost:8080/realms/fintrackpro`) |
| `Keycloak__Audience` | `appsettings.json` (e.g. `fintrackpro-api`) |
| `Keycloak__AdminClientId` | `appsettings.json` (e.g. `fintrackpro-api`) |
| `Keycloak__AdminClientSecret` | `appsettings.Development.json` (dev: `dev-secret-change-in-prod`); env var for production |
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

## Documentation Sync Rules

After any change to the **backend** (API endpoints, configuration, project structure, test setup, environment variables):
- Review and update as needed: `README.md`, `backend/README.md`, `backend/tests/README.md`, and any affected file under `docs/`

After any change to the **frontend**:
- Additionally review and update: `frontend/fintrackpro-ui/README.md`
