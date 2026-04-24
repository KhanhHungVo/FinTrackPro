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
dotnet test --filter "Category!=Integration"          # unit tests only (no external dependencies)
dotnet test --filter "Category=Integration"           # integration tests (requires local PostgreSQL — set TEST_DB_CONNECTION_STRING)
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
npm test         # Vitest unit tests
npm run test:e2e # Playwright E2E (requires E2E_TOKEN — use scripts/e2e-local.sh instead)
```

### E2E tests (Playwright)
```bash
# Git Bash / WSL — mints token from local Keycloak and runs all Playwright specs
bash scripts/e2e-local.sh
bash scripts/e2e-local.sh --ui                             # UI mode
bash scripts/e2e-local.sh tests/e2e/budgets.spec.ts        # single spec
```

> Full prerequisites, token-flow internals, and troubleshooting: `docs/guides/dev-setup.md` (Mode E).

### API E2E tests (Newman)
Newman-based suite that hits a real running API process with a real Keycloak-issued JWT.

```bash
# Prerequisites: Docker up (postgres + keycloak), API on :5018, Newman installed globally
npm install -g newman

bash scripts/api-e2e-local.sh                                     # full suite
bash scripts/api-e2e-local.sh --folder "Trades — Full lifecycle"  # single folder
bash scripts/api-e2e-local.sh --verbose                           # full output
```

> Full prerequisites, env var overrides, token-flow internals, and troubleshooting: `docs/guides/dev-setup.md` (Mode F).  
> Collection structure, CI job diagram, and GitHub secrets: `docs/postman/api-e2e-plan.md`.

### External API schema checks (Newman)
Verifies third-party API schemas (CoinGecko, Fear & Greed, ExchangeRate-API, Binance). No running API or Keycloak required.

```bash
# No API keys (free-tier endpoints; ExchangeRate step is skipped)
newman run docs/postman/FinTrackPro.external-schema.postman_collection.json \
  -e docs/postman/FinTrackPro.postman_environment.json \
  --reporters cli

# With API keys
newman run docs/postman/FinTrackPro.external-schema.postman_collection.json \
  -e docs/postman/FinTrackPro.postman_environment.json \
  --env-var "coinGeckoApiKey=<your-key>" \
  --env-var "exchangeRateApiKey=<your-key>" \
  --reporters cli
```

### Infrastructure
```bash
docker compose up -d postgres keycloak    # hybrid dev (recommended)
docker compose up --build                 # full docker

# Terraform (Render deployment)
cd infra/terraform
terraform init && terraform plan && terraform apply
```

## Architecture

### Backend — Clean Architecture
Strict layer isolation with inward-only dependencies:

- **Domain** (`FinTrackPro.Domain`) — entities, value objects, repository interfaces. No external dependencies.
- **Application** (`FinTrackPro.Application`) — CQRS via MediatR (commands/queries/handlers), DTOs, FluentValidation validators, MediatR pipeline behaviors: `ValidationBehavior` → `LoggingBehavior` → `EnsureUserBehavior` (auto-provisions `AppUser` on first login).
- **Infrastructure** (`FinTrackPro.Infrastructure`) — EF Core (Code-First, migrations here), repository implementations, IAM provider abstraction (`IIamProviderService` / `IClaimsTransformation` — selected via `IdentityProvider:Provider` config: `"keycloak"` uses `KeycloakAdminService` + `KeycloakClaimsTransformer`; `"auth0"` uses `Auth0ManagementService` + `Auth0ClaimsTransformer`), Telegram.Bot, Skender.Stock.Indicators.
- **API** (`FinTrackPro.API`) — ASP.NET Core controllers, DI registration, middleware (exception handling → maps domain exceptions to RFC 7807 Problem Details: 400/403/404/409/500), Scalar API docs.
- **BackgroundJobs** (`FinTrackPro.BackgroundJobs`) — Hangfire job definitions: `MarketSignalJob` (every 4h), `BudgetOverrunJob` (daily), `IamUserSyncJob` (daily — deactivates deleted/disabled users from the active IAM provider in the local DB).

DTOs use explicit `operator` conversions instead of AutoMapper. `ICurrentUserService` extracts the authenticated user from the JWT claim. Add new features as a command/query + handler pair in Application, then expose via a thin controller action.

### Frontend — Feature-Sliced Design (FSD)
Strict top-down layer hierarchy (`app → pages → widgets → features → entities → shared`). Server state: React Query. Client state: Zustand. Axios injects Bearer token and handles 401. See [frontend/fintrackpro-ui/README.md](frontend/fintrackpro-ui/README.md) for details.

### Auth
The IAM provider is selected by a single config key: `IdentityProvider:Provider = "keycloak" | "auth0"` (backend) and `VITE_AUTH_PROVIDER=keycloak|auth0` (frontend). Both providers issue JWT Bearer tokens; the backend and frontend use provider-specific adapters behind a shared interface.

**Keycloak** (local dev, Docker): realm `fintrackpro` is auto-provisioned from `infra/docker/keycloak-realm.json` on first `docker compose up`. Import is idempotent. Default dev credentials: `admin@fintrackpro.dev` / `Admin1234!`. The `User` role is assigned by Default Roles; `Admin` is assigned manually.

**Auth0** (cloud, free tier): requires one-time dashboard setup (API, SPA app, M2M app, roles, post-login Action). See `docs/guides/auth-setup.md` for the full Auth0 setup guide.

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
| `ExchangeRate__ApiKey` | `appsettings.Development.json` / env — ExchangeRate-API v6 key; required for fiat rate sync |
| `PaymentGateway__Provider` | `appsettings.json` — `"stripe"` (default); swap to add future providers |
| `PaymentGateway__PriceId` | `appsettings.json` / env — logical Pro plan price identifier (provider-neutral) |
| `Stripe__SecretKey` | env var / `dotnet user-secrets` — Stripe API secret key |
| `Stripe__WebhookSecret` | env var / `dotnet user-secrets` — Stripe webhook endpoint signing secret |
| `LoggingBehavior__SlowHandlerThresholdMs` | `appsettings.json` — MediatR handler warning threshold in ms (default `500`) |
| `VITE_AUTH_PROVIDER` | `frontend/fintrackpro-ui/.env` (`"keycloak"` or `"auth0"`) |
| `VITE_API_BASE_URL` | `frontend/fintrackpro-ui/.env` |
| `VITE_KEYCLOAK_URL/REALM/CLIENT_ID` | `frontend/fintrackpro-ui/.env` (Keycloak mode) |
| `VITE_AUTH0_DOMAIN/CLIENT_ID/AUDIENCE` | `frontend/fintrackpro-ui/.env` (Auth0 mode) |
| `VITE_ADMIN_TELEGRAM` | `frontend/fintrackpro-ui/.env` — Telegram handle shown in bank transfer modal |
| `VITE_ADMIN_EMAIL` | `frontend/fintrackpro-ui/.env` — admin email shown in bank transfer modal |
| `VITE_BANK_NAME` | `frontend/fintrackpro-ui/.env` — bank name displayed in transfer details |
| `VITE_BANK_ACCOUNT_NUMBER` | `frontend/fintrackpro-ui/.env` — account number for bank transfer |
| `VITE_BANK_ACCOUNT_NAME` | `frontend/fintrackpro-ui/.env` — account holder name |
| `VITE_BANK_TRANSFER_AMOUNT` | `frontend/fintrackpro-ui/.env` — monthly Pro price in VND (default `99000`) |
| `VITE_FREE_TRANSACTIONS_LIMIT` | `frontend/fintrackpro-ui/.env` — Free plan transaction cap (default `50`); shown on landing page pricing section |
| `VITE_FREE_HISTORY_DAYS` | `frontend/fintrackpro-ui/.env` — Free plan history window in days (default `60`) |
| `VITE_FREE_BUDGETS_LIMIT` | `frontend/fintrackpro-ui/.env` — Free plan active-budget cap (default `3`) |
| `VITE_FREE_TRADES_LIMIT` | `frontend/fintrackpro-ui/.env` — Free plan stored-trade cap (default `20`) |
| `VITE_FREE_WATCHLIST_LIMIT` | `frontend/fintrackpro-ui/.env` — Free plan watchlist symbol cap (default `1`) |
| `VITE_PRO_TRANSACTIONS_LIMIT` | `frontend/fintrackpro-ui/.env` — Pro plan transaction cap (default `500`) |
| `VITE_PRO_BUDGETS_LIMIT` | `frontend/fintrackpro-ui/.env` — Pro plan active-budget cap (default `20`) |
| `VITE_PRO_TRADES_LIMIT` | `frontend/fintrackpro-ui/.env` — Pro plan stored-trade cap (default `200`) |
| `VITE_PRO_WATCHLIST_LIMIT` | `frontend/fintrackpro-ui/.env` — Pro plan watchlist symbol cap (default `20`) |

Copy `frontend/fintrackpro-ui/.env.example` → `.env` before first run.

**Local dev secrets:** use `dotnet user-secrets` (Development environment only) to store sensitive values outside the repo. Secrets override `appsettings.Development.json` at runtime:
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection-string>" --project backend/src/FinTrackPro.API
dotnet user-secrets set "IdentityProvider:AdminClientSecret" "<secret>" --project backend/src/FinTrackPro.API
dotnet user-secrets set "CoinGecko:ApiKey" "<key>" --project backend/src/FinTrackPro.API
dotnet user-secrets set "Stripe:SecretKey" "<sk_test_...>" --project backend/src/FinTrackPro.API
dotnet user-secrets set "Stripe:WebhookSecret" "<whsec_...>" --project backend/src/FinTrackPro.API
```

## Ports (local hybrid dev)
| Service | Port |
|---|---|
| API | 5018 |
| Frontend (Vite) | 5173 |
| Keycloak | 8080 |
| SQL Server | 1433 |
| PostgreSQL | 5432 |
| API (Docker) | 5000 |

## Docs
### Architecture (reference — what the system is)
- `docs/architecture/overview.md` — layer descriptions and design decisions
- `docs/architecture/auth.md` — IAM provider overview, auth flows (sign-up, login, nightly sync), and provider switching reference
- `docs/architecture/api-spec.md` — REST endpoints and schemas
- `docs/architecture/database.md` — schema, tables, relationships, migration commands
- `docs/architecture/background-jobs.md` — Hangfire job details and sequence diagrams
- `docs/architecture/ui-flows.md` — frontend user flows

### Guides (how-to — operational and setup)
- `docs/guides/dev-setup.md` — hybrid vs full-Docker setup, local PostgreSQL, Render deployment
- `docs/guides/auth-setup.md` — IAM provider setup (Keycloak, Auth0, switching providers)
- `docs/guides/render-deploy.md` — Render deploy guide (Terraform primary + render.yaml fallback + migration strategies)
- `docs/guides/testing.md` — manual E2E test guide across provider × mode combinations
- `docs/guides/security-hardening.md` — rate limiting, headers, HTTPS, Cloudflare setup

### Decisions (implemented — why things are the way they are)
- `docs/decisions/postgres-migration.md` — migration from Azure SQL to PostgreSQL
- `docs/decisions/integration-test-refactor.md` — SQL Server → PostgreSQL test infra change
- `docs/decisions/preventing-duplicate-calls-on-fast-clicks.md` — useGuardedMutation hook design rationale
- `docs/decisions/transaction-category-system.md` — structured TransactionCategory entity (system-seeded defaults + user-custom, three-phase migration strategy)
- `docs/decisions/open-positions-trade-status.md` — Open/Closed status model for trades; nullable exitPrice, ClosePositionCommand, realized vs. unrealized P&L split
- `docs/decisions/monetisation-subscription-design.md` — Freemium subscription system with Stripe (backend + frontend)
- `docs/decisions/landing-page-fsd-integration.md` — public landing page at `/`; check-sso auth init, `login()` adapter method, `RequireAuth` guard, pricing limit env vars
- `docs/decisions/nav-avatar-dropdown-tabbed-settings.md` — avatar dropdown nav links; tabbed Settings with URL-persisted active tab; About page
- `docs/decisions/market-dashboard-phase1.md` — Market page upgrade: HybridCache migration, Trending Coins enrichment (top 10 + price/%), Top Market Cap widget, Watchlist RSI Analysis widget
- `docs/planned/dashboard-command-center.md` — Dashboard redesign: personalized command center (expense allocation, budget health, trading intelligence, recent activity, contextual signals); market data moved to `/market`

### Planned (not yet implemented)
- `docs/planned/identity-linking-refactor.md` — multi-provider identity linking via UserContextMiddleware
- `docs/planned/auth0-config-as-code.md` — Auth0 CLI deploy automation
- `docs/planned/frontend-error-handling.md` — consistent error handling and form validation
- `docs/planned/health-checks-external-services.md` — health check endpoints for external services

## Documentation Sync Rules

After any change to the **backend** (API endpoints, configuration, project structure, test setup, environment variables):
- Review and update as needed: `README.md`, `backend/README.md`, `backend/tests/README.md`, and any affected file under `docs/`

After any change to the **frontend**:
- Additionally review and update: `frontend/fintrackpro-ui/README.md`
