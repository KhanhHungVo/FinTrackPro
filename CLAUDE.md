# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Commands

### Backend (.NET 10)
```bash
cd backend
dotnet restore && dotnet build
dotnet run --project src/FinTrackPro.API

dotnet test                                           # all tests
dotnet test --filter "Category!=Integration"          # unit only
dotnet test --filter "Category=Integration"           # requires PostgreSQL (TEST_DB_CONNECTION_STRING)
dotnet test --filter "FullyQualifiedName~<Test>"      # single test

# EF Core migrations
dotnet ef migrations add <Name> --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API
dotnet ef database update  --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API
```

### Frontend (React 19 + Vite)
```bash
cd frontend/fintrackpro-ui
npm install
npm run dev       # HMR dev server → :5173
npm run build     # type-check + production build
npm run lint
npm test          # Vitest unit tests
npm run test:e2e  # Playwright (use scripts/e2e-local.sh instead — needs E2E_TOKEN)
```

### E2E & API tests
```bash
# Playwright — mints Keycloak token automatically
bash scripts/e2e-local.sh                                    # all specs
bash scripts/e2e-local.sh --ui
bash scripts/e2e-local.sh tests/e2e/budgets.spec.ts

# Newman API E2E (Docker up + API on :5018 required)
npm install -g newman
bash scripts/api-e2e-local.sh                                # full suite
bash scripts/api-e2e-local.sh --folder "Trades — Full lifecycle"
bash scripts/api-e2e-local.sh --verbose

# External API schema checks (no running API needed)
newman run docs/postman/FinTrackPro.external-schema.postman_collection.json \
  -e docs/postman/FinTrackPro.postman_environment.json --reporters cli
```
> Details: `docs/guides/dev-setup.md` (Mode E = Playwright, Mode F = Newman). CI job diagram: `docs/postman/api-e2e-plan.md`.

### Infrastructure
```bash
docker compose up -d postgres keycloak    # hybrid dev (recommended)
docker compose up --build                 # full Docker stack
docker compose -f docker-compose.auth0.yml up -d   # Auth0 variant

# Rotate Render managed DB
bash scripts/rotate-render-db.sh

# Terraform (Render)
cd infra/terraform && terraform init && terraform plan && terraform apply
```

## Architecture

### Backend — Clean Architecture
Strict inward-only dependency flow:

| Layer | Project | Key contents |
|---|---|---|
| Domain | `FinTrackPro.Domain` | Entities (`BaseEntity`), value objects, domain events (`BaseEvent`), repository interfaces, domain exceptions |
| Application | `FinTrackPro.Application` | CQRS (MediatR commands/queries/handlers), DTOs, FluentValidation, MediatR pipeline |
| Infrastructure | `FinTrackPro.Infrastructure` | EF Core (Code-First + migrations), repository impls, IAM adapters, `IDataSeeder` seeders, interceptors (audit timestamps), Telegram.Bot, Skender.Stock.Indicators |
| API | `FinTrackPro.API` | `BaseApiController`, controllers, DI registration, `ExceptionHandlingMiddleware`, `HangfireBasicAuthFilter`, health checks (`/health`), Scalar docs |
| BackgroundJobs | `FinTrackPro.BackgroundJobs` | Hangfire: `MarketSignalJob` (4h), `BudgetOverrunJob` (daily), `IamUserSyncJob` (daily) |

**MediatR pipeline order:** `ValidationBehavior` → `LoggingBehavior` → `EnsureUserBehavior` (auto-provisions `AppUser` on first login).

**Key conventions:**
- DTOs use explicit `operator` conversions (no AutoMapper)
- `ICurrentUserService` — extracts authenticated user from JWT
- New features → command/query + handler in Application, thin controller action in API
- `NotificationService` / `NullNotificationChannel` — null-object notification pattern

**Domain exceptions** (mapped to Problem Details by middleware):

| Exception | HTTP |
|---|---|
| `NotFoundException` | 404 |
| `AuthorizationException` | 403 |
| `ConflictException` | 409 |
| `PlanLimitExceededException` | 403 (freemium gate) |
| Unhandled | 500 |

### Frontend — Feature-Sliced Design (FSD)
Layer hierarchy (strict top-down): `app → pages → widgets → features → entities → shared`

- Server state: React Query. Client state: Zustand. Axios injects Bearer token, handles 401.
- Auth adapter pattern: `IAuthAdapter` → `KeycloakAdapter` | `Auth0Adapter` (switched via `VITE_AUTH_PROVIDER`)
- Key shared utilities: `useGuardedMutation` (prevents duplicate fast-click mutations), `apiError.ts`, `formatCurrency.ts`, `useDebounce`
- Error UI: `ErrorBoundary.tsx`, `ErrorPage.tsx`, `AuthErrorScreen.tsx`, `AuthDegradedBanner.tsx`

See [frontend/fintrackpro-ui/README.md](frontend/fintrackpro-ui/README.md) for full FSD details.

### Auth
Single config key switches providers: `IdentityProvider:Provider = "keycloak" | "auth0"` (backend), `VITE_AUTH_PROVIDER` (frontend).

**Keycloak** (local/Docker): realm `fintrackpro` auto-provisioned from `infra/docker/keycloak-realm.json`. Dev credentials: `admin@fintrackpro.dev` / `Admin1234!`. `User` role via Default Roles; `Admin` assigned manually.

**Auth0** (cloud): one-time dashboard setup (API, SPA, M2M apps, roles, post-login Action). See `docs/guides/auth-setup.md`.

Roles stored only in the IAM provider. `AppUser.ExternalUserId` = JWT `sub`; `AppUser.Provider` = issuer.

### CI/CD
GitHub Actions (`.github/workflows/`):
- `ci.yml` — backend build/test (unit + integration), frontend build/test, Newman API E2E, Playwright E2E; triggers on push to `main`/`develop` and PRs to `main`
- `external-schema-check.yml` — third-party API schema validation
- `db-rotation.yml` — Render DB rotation

Render deployment: `render.yaml` (manifest) + `infra/terraform/` (Terraform). See `docs/guides/render-deploy.md`.

## Key Configuration

### Backend (`appsettings.json` / env / `dotnet user-secrets`)

| Variable | Notes |
|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `IdentityProvider__Provider` | `"keycloak"` (default) or `"auth0"` |
| `IdentityProvider__Audience` | JWT `aud` — `https://api.fintrackpro.dev` |
| `IdentityProvider__AdminClientId` | M2M client ID for IAM admin API |
| `IdentityProvider__AdminClientSecret` | Secret — gitignored / env var in prod |
| `Keycloak__Authority` | Validates `iss` claim |
| `Keycloak__MetadataAddress` | Overridden in `docker-compose.yml` for container DNS |
| `Auth0__Domain` | Auth0 tenant domain |
| `Telegram__BotToken` | Env var only |
| `CoinGecko__ApiKey` | Demo or Pro key; required for `/market/trending` |
| `ExchangeRate__ApiKey` | ExchangeRate-API v6; required for fiat rate sync |
| `PaymentGateway__Provider` | `"stripe"` (default) |
| `PaymentGateway__PriceId` | Provider-neutral Pro plan price ID |
| `Stripe__SecretKey` | `dotnet user-secrets` / env var |
| `Stripe__WebhookSecret` | `dotnet user-secrets` / env var |
| `LoggingBehavior__SlowHandlerThresholdMs` | Default `500` ms |

```bash
# Set local dev secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<conn>" --project backend/src/FinTrackPro.API
dotnet user-secrets set "IdentityProvider:AdminClientSecret" "<secret>" --project backend/src/FinTrackPro.API
dotnet user-secrets set "Stripe:SecretKey" "<sk_test_...>" --project backend/src/FinTrackPro.API
dotnet user-secrets set "Stripe:WebhookSecret" "<whsec_...>" --project backend/src/FinTrackPro.API
dotnet user-secrets set "CoinGecko:ApiKey" "<key>" --project backend/src/FinTrackPro.API
```

### Frontend (`frontend/fintrackpro-ui/.env` — copy from `.env.example`)

| Variable | Notes |
|---|---|
| `VITE_AUTH_PROVIDER` | `"keycloak"` or `"auth0"` |
| `VITE_API_BASE_URL` | Backend API URL |
| `VITE_KEYCLOAK_URL/REALM/CLIENT_ID` | Keycloak mode |
| `VITE_AUTH0_DOMAIN/CLIENT_ID/AUDIENCE` | Auth0 mode |
| `VITE_ADMIN_TELEGRAM` | Shown in bank transfer modal |
| `VITE_ADMIN_EMAIL` | Shown in bank transfer modal |
| `VITE_BANK_NAME` | Bank transfer UI |
| `VITE_BANK_ACCOUNT_NUMBER` | Bank transfer UI |
| `VITE_BANK_ACCOUNT_NAME` | Bank transfer UI |
| `VITE_BANK_TRANSFER_AMOUNT` | Monthly Pro price in VND (default `99000`) |
| `VITE_BANK_QR_URL` | QR code image URL for bank transfer |
| `VITE_FREE_TRANSACTIONS_LIMIT` | Default `50` |
| `VITE_FREE_HISTORY_DAYS` | Default `60` |
| `VITE_FREE_BUDGETS_LIMIT` | Default `3` |
| `VITE_FREE_TRADES_LIMIT` | Default `20` |
| `VITE_FREE_WATCHLIST_LIMIT` | Default `1` |
| `VITE_PRO_TRANSACTIONS_LIMIT` | Default `500` |
| `VITE_PRO_BUDGETS_LIMIT` | Default `20` |
| `VITE_PRO_TRADES_LIMIT` | Default `200` |
| `VITE_PRO_WATCHLIST_LIMIT` | Default `20` |

## Ports (hybrid dev)

| Service | Port |
|---|---|
| API | 5018 |
| Frontend (Vite) | 5173 |
| Keycloak | 8080 |
| PostgreSQL | 5432 |
| API (Docker) | 5000 |

## Docs Index

### Architecture
- `docs/architecture/overview.md` — layers and design decisions
- `docs/architecture/auth.md` — IAM flows, provider switching
- `docs/architecture/api-spec.md` — REST endpoints and schemas
- `docs/architecture/database.md` — schema, tables, migrations
- `docs/architecture/background-jobs.md` — Hangfire job details
- `docs/architecture/ui-flows.md` — frontend user flows

### Guides
- `docs/guides/dev-setup.md` — hybrid/Docker setup, Modes E & F
- `docs/guides/auth-setup.md` — Keycloak and Auth0 setup
- `docs/guides/render-deploy.md` — Terraform + render.yaml deploy
- `docs/guides/testing.md` — manual E2E across provider × mode
- `docs/guides/security-hardening.md` — rate limiting, headers, HTTPS, Cloudflare

### Decisions (implemented)
- `docs/decisions/postgres-migration.md`
- `docs/decisions/integration-test-refactor.md`
- `docs/decisions/preventing-duplicate-calls-on-fast-clicks.md` — `useGuardedMutation`
- `docs/decisions/transaction-category-system.md`
- `docs/decisions/open-positions-trade-status.md`
- `docs/decisions/monetisation-subscription-design.md`
- `docs/decisions/landing-page-fsd-integration.md`
- `docs/decisions/nav-avatar-dropdown-tabbed-settings.md`
- `docs/decisions/market-dashboard-phase1.md`
- `docs/decisions/watchlist-pro-only-gate.md`
- `docs/planned/dashboard-command-center.md`

### Planned
- `docs/planned/identity-linking-refactor.md`
- `docs/planned/auth0-config-as-code.md`
- `docs/planned/frontend-error-handling.md`
- `docs/planned/health-checks-external-services.md`

### Other
- `docs/features.md` — feature registry
- `docs/roadmap.md` — product roadmap
- `docs/postman/api-e2e-plan.md` — Newman collection structure, CI diagram, secrets

## Documentation Sync Rules

After **backend** changes (endpoints, config, env vars, project structure):
- Update `README.md`, `backend/README.md`, `backend/tests/README.md`, affected `docs/` files

After **frontend** changes:
- Also update `frontend/fintrackpro-ui/README.md`
