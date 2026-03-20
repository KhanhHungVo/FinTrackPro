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
- **Infrastructure** (`FinTrackPro.Infrastructure`) — EF Core (Code-First, migrations here), repository implementations, IAM provider abstraction (`IIamProviderService` / `IClaimsTransformation` — selected via `IdentityProvider:Provider` config: `"keycloak"` uses `KeycloakAdminService` + `KeycloakClaimsTransformer`; `"auth0"` uses `Auth0ManagementService` + `Auth0ClaimsTransformer`), Telegram.Bot, Skender.Stock.Indicators.
- **API** (`FinTrackPro.API`) — ASP.NET Core controllers, DI registration, middleware (exception handling → maps domain exceptions to HTTP codes), Scalar API docs.
- **BackgroundJobs** (`FinTrackPro.BackgroundJobs`) — Hangfire job definitions: `MarketSignalJob` (every 4h), `BudgetOverrunJob` (daily), `IamUserSyncJob` (daily — deactivates deleted/disabled users from the active IAM provider in the local DB).

DTOs use explicit `operator` conversions instead of AutoMapper. `ICurrentUserService` extracts the authenticated user from the JWT claim. Add new features as a command/query + handler pair in Application, then expose via a thin controller action.

### Frontend — Feature-Sliced Design (FSD)
Strict top-down layer hierarchy (upper layers may only import from lower):
```
app → pages → widgets → features → entities → shared
```
Server state lives in **React Query** (TanStack). Client-only state (auth, UI flags) lives in **Zustand**. HTTP calls go through an Axios instance that injects the Bearer token (via `authAdapter.getToken()`) and handles 401 via `authAdapter.refreshToken()`.

### Auth
The IAM provider is selected by a single config key: `IdentityProvider:Provider = "keycloak" | "auth0"` (backend) and `VITE_AUTH_PROVIDER=keycloak|auth0` (frontend). Both providers issue JWT Bearer tokens; the backend and frontend use provider-specific adapters behind a shared interface.

**Keycloak** (local dev, Docker): realm `fintrackpro` is auto-provisioned from `infra/docker/keycloak-realm.json` on first `docker compose up`. Import is idempotent. Default dev credentials: `admin@fintrackpro.dev` / `Admin1234!`. The `User` role is assigned by Default Roles; `Admin` is assigned manually.

**Auth0** (cloud, free tier): requires one-time dashboard setup (API, SPA app, M2M app, roles, post-login Action). See `docs/auth-setup.md` for the full Auth0 setup guide.

Roles (`User`, `Admin`) are stored only in the IAM provider — never in the database. `AppUser.ExternalUserId` stores the JWT `sub` claim; `AppUser.Provider` records which IAM issued it.

## Key Configuration

| Variable | Where |
|---|---|
| `ConnectionStrings__DefaultConnection` | `appsettings.json` / env |
| `IdentityProvider__Provider` | `appsettings.json` (default `"keycloak"`); override to `"auth0"` for cloud |
| `IdentityProvider__Audience` | `appsettings.json` — JWT `aud` claim; URI convention (`https://api.fintrackpro.dev`) |
| `IdentityProvider__AdminClientId` | `appsettings.json` — M2M client ID for the active IAM provider's admin API |
| `IdentityProvider__AdminClientSecret` | `appsettings.Development.json` (gitignored); env var for production |
| `Keycloak__Authority` | `appsettings.json` — validates `iss` claim |
| `Keycloak__MetadataAddress` | `appsettings.json` — overridden in `docker-compose.yml` for container DNS |
| `Auth0__Domain` | `appsettings.Development.json` / env |
| `Telegram__BotToken` | env var only |
| `CoinGecko__ApiKey` | `appsettings.Development.json` / env — Demo or Pro API key; required for `/market/trending` endpoint |
| `VITE_AUTH_PROVIDER` | `frontend/fintrackpro-ui/.env` (`"keycloak"` or `"auth0"`) |
| `VITE_API_BASE_URL` | `frontend/fintrackpro-ui/.env` |
| `VITE_KEYCLOAK_URL/REALM/CLIENT_ID` | `frontend/fintrackpro-ui/.env` (Keycloak mode) |
| `VITE_AUTH0_DOMAIN/CLIENT_ID/AUDIENCE` | `frontend/fintrackpro-ui/.env` (Auth0 mode) |

Copy `frontend/fintrackpro-ui/.env.example` → `.env` before first run.

**Local dev secrets:** use `dotnet user-secrets` (Development environment only) to store sensitive values outside the repo. Secrets override `appsettings.Development.json` at runtime:
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<azure-sql-string>" --project backend/src/FinTrackPro.API
dotnet user-secrets set "IdentityProvider:AdminClientSecret" "<secret>" --project backend/src/FinTrackPro.API
dotnet user-secrets set "CoinGecko:ApiKey" "<key>" --project backend/src/FinTrackPro.API
```

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
- `docs/auth-setup.md` — IAM provider setup (Keycloak manual config, Auth0 dashboard, switching providers)
- `docs/api-spec.md` — REST endpoints and schemas
- `docs/database.md` — schema, tables, relationships

## Documentation Sync Rules

After any change to the **backend** (API endpoints, configuration, project structure, test setup, environment variables):
- Review and update as needed: `README.md`, `backend/README.md`, `backend/tests/README.md`, and any affected file under `docs/`

After any change to the **frontend**:
- Additionally review and update: `frontend/fintrackpro-ui/README.md`
