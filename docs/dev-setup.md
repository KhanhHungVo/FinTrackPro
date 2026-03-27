# Dev Setup — End-to-End

Two ways to run the full stack locally. **Hybrid mode** is recommended for day-to-day development.

## Contents

- [Prerequisites](#prerequisites)
- [Mode A — Full Docker](#mode-a--full-docker-quick-smoke-test)
- [Mode B — Hybrid (recommended)](#mode-b--hybrid-recommended-for-development)
  - [Step 1 — Start infrastructure](#step-1--start-infrastructure)
  - [Step 2 — Database migration](#step-2--create-and-apply-database-migration-first-time-only)
  - [Step 3 — Run the API](#step-3--run-the-api)
  - [Step 4 — Run the frontend](#step-4--run-the-frontend)
- [Mode C — Hybrid dev against local PostgreSQL](#mode-c--hybrid-dev-against-local-postgresql)
- [Mode D — Hybrid dev against Render PostgreSQL](#mode-d--hybrid-dev-against-render-postgresql-production-db)
- [Mode E — Running Playwright E2E Tests Locally](#mode-e--running-playwright-e2e-tests-locally)
- [Port Reference](#port-reference)
- [Verifying the Stack](#verifying-the-stack)
- [Stopping the Stack](#stopping-the-stack)

> For IAM provider configuration (Keycloak manual setup, Auth0 dashboard config, switching providers) see [docs/auth-setup.md](auth-setup.md).

---

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| Docker Desktop | Latest | Must be running |
| .NET SDK | 10.0 | |
| Node.js | 22+ | |
| EF Core CLI | Latest | `dotnet tool install --global dotnet-ef` |

---

## Mode A — Full Docker (quick smoke test)

Starts infrastructure and the API in containers. Use this to verify the whole stack compiles and
connects without touching local toolchains.

```bash
# From repo root
docker compose up --build
```

Compose handles startup order automatically:
1. `postgres` starts and passes its health check
2. `keycloak` starts and auto-imports the `fintrackpro` realm from `infra/docker/keycloak-realm.json`
3. `migrator` (SDK image) runs `dotnet ef database update` and exits
4. `api` starts only after `migrator` completes successfully

No local .NET toolchain required. Then start the frontend separately (it is not in the compose file):

```bash
cd frontend/fintrackpro-ui
cp .env.example .env          # VITE_API_BASE_URL=http://localhost:5018 is correct here
npm install
npm run dev
```

| Service | URL |
|---|---|
| Frontend | http://localhost:5173 |
| API | http://localhost:5018 |
| Keycloak | http://localhost:8080 |
| PostgreSQL | localhost:5432 |

> Keycloak realm is imported automatically on first start — no manual setup needed.
> Log in with `admin@fintrackpro.dev` / `Admin1234!` (Admin role) or register a new account.

---

## Mode B — Hybrid (recommended for development)

Infrastructure runs in Docker; API and frontend run locally so you get hot reload and debugger
support on both.

### Step 1 — Start infrastructure

```bash
# From repo root
docker compose up -d postgres keycloak
```

Wait ~10 seconds for PostgreSQL to be ready and Keycloak to finish booting.

> The `fintrackpro` realm is **automatically provisioned** from `infra/docker/keycloak-realm.json`
> on first start. No manual Keycloak configuration is required. Log in immediately with
> `admin@fintrackpro.dev` / `Admin1234!`, or register a new account at http://localhost:5173.
>
> The import is idempotent — if the realm already exists (volume persisted from a previous run)
> the JSON is silently skipped, so any manual changes you made are preserved.

If you need to recreate the realm manually or configure custom settings (social login, redirect URIs, etc.), see the [Keycloak manual setup reference](auth-setup.md#manual-setup-reference).

---

### Step 2 — Create and apply database migration (first time only)

> The Migrations folder is empty on a fresh clone — run this once before starting the API.
> PostgreSQL must already be running (Step 1) before applying migrations.

```bash
cd backend

dotnet ef migrations add InitialCreate --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API

dotnet ef database update --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API
```

This works because `appsettings.json` targets `localhost:5432` — the port that Docker maps from the `postgres` container.

---

### Step 3 — Run the API

```bash
cd backend
dotnet run --project src/FinTrackPro.API --launch-profile http
```

API listens on **http://localhost:5018** (defined in `Properties/launchSettings.json`).

The dev `IdentityProvider:AdminClientSecret` is already set in `appsettings.Development.json` so the nightly
`IamUserSyncJob` works without any extra environment variables.

**HTTP resilience and logging** — defaults in `appsettings.json` are production-safe. Override in
`appsettings.Development.json` to tune for local dev (e.g., disable masking to see raw payloads):

```json
{
  "HttpLogging": {
    "MaskSensitiveData": false
  },
  "HttpResilience": {
    "RetryCount": 1,
    "RetryBaseDelayMs": 200,
    "TimeoutSeconds": 10
  }
}
```

| Key | Default | Purpose |
|---|---|---|
| `HttpLogging:MaskSensitiveData` | `true` | Redact sensitive headers/body fields before logging |
| `HttpResilience:RetryCount` | `3` | Max retry attempts on transient failures |
| `HttpResilience:RetryBaseDelayMs` | `500` | Base exponential back-off delay (ms) |
| `HttpResilience:TimeoutSeconds` | `30` | Total request timeout covering all retries |
| `HttpResilience:CircuitBreakerFailurePercent` | `50` | % failure threshold that opens the circuit |
| `HttpResilience:CircuitBreakerBreakDurationSeconds` | `30` | How long the circuit stays open |
| `HttpResilience:CircuitBreakerSamplingDurationSeconds` | `60` | Sliding window for failure ratio measurement |
| `HttpResilience:CircuitBreakerMinimumThroughput` | `5` | Min requests before circuit can open |

Optional — set environment variables before starting if you need Telegram notifications or CoinGecko data. `Telegram__BotToken` is optional; without it, notifications are skipped instead of failing startup:

```bash
export Telegram__BotToken="your-token-here"
export CoinGecko__ApiKey="your-demo-or-pro-key-here"   # required — CoinGecko free tier now requires an API key
dotnet run --project src/FinTrackPro.API --launch-profile http
```

> Get a free Demo API key at https://www.coingecko.com/en/api — sign up and copy the key from the
> Developer Dashboard. Set it in `appsettings.Development.json` under `CoinGecko:ApiKey` or via
> the `CoinGecko__ApiKey` environment variable. If using the Pro plan, replace the header name
> `x-cg-demo-api-key` with `x-cg-pro-api-key` in
> `FinTrackPro.Infrastructure/DependencyInjection.cs`.

---

### Step 4 — Run the frontend

```bash
cd frontend/fintrackpro-ui
cp .env.example .env
```

Open `.env` and set the values for your chosen IAM provider.

**Keycloak (default):**

```
VITE_API_BASE_URL=http://localhost:5018
VITE_AUTH_PROVIDER=keycloak
VITE_KEYCLOAK_URL=http://localhost:8080
VITE_KEYCLOAK_REALM=fintrackpro
VITE_KEYCLOAK_CLIENT_ID=fintrackpro-spa
```

**Auth0:**

```
VITE_API_BASE_URL=http://localhost:5018
VITE_AUTH_PROVIDER=auth0
VITE_AUTH0_DOMAIN=your-tenant.auth0.com
VITE_AUTH0_CLIENT_ID=your-spa-client-id
VITE_AUTH0_AUDIENCE=https://api.fintrackpro.dev
```

> Auth0 requires a one-time dashboard setup before first use. See [docs/auth-setup.md](auth-setup.md#auth0-cloud-iam).
> To run the full stack in Docker with Auth0 (no Keycloak), see [Auth0 with Full Docker](auth-setup.md#auth0-with-full-docker).

```bash
npm install
npm run dev
```

Frontend runs at **http://localhost:5173**.

---

---

## Mode C — Hybrid dev against SQL Server (optional)

Run the API locally using the `sqlserver` Docker service instead of the default PostgreSQL. Useful if you need
to test SQL Server-specific behaviour.

### Prerequisites

- Docker Desktop running

### Step 1 — Start SQL Server and Keycloak

```bash
docker compose --profile sqlserver up -d sqlserver keycloak
```

Wait ~15 seconds for SQL Server to be ready.

### Step 2 — Set the provider and connection string

Add to `backend/src/FinTrackPro.API/appsettings.Development.json` (gitignored — edit directly):

```json
{
  "DatabaseProvider": {
    "Provider": "sqlserver"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=FinTrackPro;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;"
  }
}
```

### Step 3 — Apply migrations

```bash
cd backend
dotnet ef database update \
  --project src/FinTrackPro.Infrastructure \
  --startup-project src/FinTrackPro.API
```

### Step 4 — Run the API and frontend

Same as Mode B Steps 3 and 4.

---

## Mode D — Hybrid dev against Render PostgreSQL (production DB)

Run the API locally but target the Render production database. Useful when debugging production schema issues.

### Prerequisites

- Terraform applied (`terraform apply` completed)

### Step 1 — Get the external DB URL

```bash
cd infra/terraform
terraform output -raw db_external_url
```

### Step 2 — Set the provider and connection string

```bash
export DatabaseProvider__Provider=postgresql
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<external-url>" --project backend/src/FinTrackPro.API
```

### Step 3 — Run the API

```bash
cd backend
dotnet run --project src/FinTrackPro.API --launch-profile http
```

> Avoid running migrations against production unless intentional — the Render DB is shared with live traffic.

---

## Port Reference

| Service | Mode | URL |
|---|---|---|
| Frontend | both | http://localhost:5173 |
| API (local) | hybrid | http://localhost:5018 |
| API (Docker) | full Docker | http://localhost:5018 |
| Keycloak | both | http://localhost:8080 |
| PostgreSQL | both | localhost:5432 |
| SQL Server | Mode C (optional) | localhost:1433 |
| Scalar API docs | both | `<api-url>/scalar` |
| Hangfire dashboard | both | `<api-url>/hangfire` (Basic Auth: `hangfire-admin` / `Dev-Hangfire@1234!`) |

---

## Verifying the Stack

Run these checks in order after starting everything:

1. **`<api-url>/scalar`** loads → API is up and connected to the database
2. **http://localhost:8080** shows the Keycloak login page → Auth service is ready
3. **http://localhost:5173** redirects to the Keycloak login page → Full E2E path is working
4. Log in as `admin@fintrackpro.dev` / `Admin1234!` → Token is issued and accepted by the API

---

## Stopping the Stack

```bash
# Stop Docker services
docker compose down

# To also delete all data volumes (full reset)
docker compose down -v
```

---

## Mode E — Running Playwright E2E Tests Locally

The Playwright suite (`frontend/fintrackpro-ui/tests/e2e/`) requires a valid JWT to bypass the
Keycloak login redirect. A helper script at the repo root handles token minting and test execution.

### Prerequisites

- Docker running with `postgres` and `keycloak` containers up
- API running on `http://localhost:5018`
- Frontend dev server running on `http://localhost:5173` (`npm run dev`)
- `curl` and `grep -P` available (Git Bash / WSL / Linux — no `jq` or Node required)

### Run the tests

Use the script that matches your terminal:

```bash
# Git Bash / WSL / Linux
bash scripts/e2e-local.sh
```

```powershell
# PowerShell
.\scripts\e2e-local.ps1
```

The script:
1. Mints a short-lived JWT from the `fintrackpro-e2e` Keycloak client (direct access grant)
2. Passes it as `E2E_TOKEN` to Playwright
3. Playwright's `auth.setup.ts` injects `localStorage['access_token']` and `localStorage['e2e_bypass'] = '1'` via `addInitScript` before each spec
4. `AuthProvider` detects the `e2e_bypass` flag and skips the Keycloak SDK init entirely, using the cached token in degraded mode

> **Why `e2e_bypass`?** Keycloak's `onLoad: 'login-required'` redirects the browser before any JS
> catch handler fires — the degraded-mode path is unreachable when Keycloak is accessible. The
> `e2e_bypass` flag is an explicit opt-in that is never set by the app itself; real users are
> unaffected. The backend still validates every JWT independently.

### Pass Playwright flags

Any extra arguments are forwarded to `playwright test`:

```bash
bash scripts/e2e-local.sh --ui                          # Playwright UI mode
bash scripts/e2e-local.sh --debug                       # step through with inspector
bash scripts/e2e-local.sh tests/e2e/budgets.spec.ts     # single spec
```

```powershell
.\scripts\e2e-local.ps1 --ui
.\scripts\e2e-local.ps1 tests/e2e/budgets.spec.ts
```

### Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `invalid_client` from curl | `fintrackpro-e2e` client not in realm | `docker compose restart keycloak` (re-imports realm JSON) |
| Token is empty / curl fails | Keycloak not ready | Wait ~30s after `docker compose up` and retry |
| Keycloak login page appears in test | `e2e_bypass` flag not set | Check `auth.setup.ts` — both `access_token` and `e2e_bypass` must be in `addInitScript` |
| API 401 errors in tests | Store fallback not working | Check `client.ts` interceptor catch reads `useAuthStore.getState().accessToken` |

### CI (GitHub Actions)

The `e2e` job in `.github/workflows/ci.yml` mints the token inline using the same
`fintrackpro-e2e` client and injects it as `E2E_TOKEN`. No changes to the script are needed for CI.

---

## Mode D — Deploy to Render.com

Both services (API + frontend) are deployed to Render via **Terraform** (`infra/terraform/`).
The `render.yaml` Blueprint remains as a fallback for manual one-click deploys.

See **[docs/render-terraform-deploy.md](render-terraform-deploy.md)** for the complete guide covering:
- Terraform step-by-step (primary)
- render.yaml Blueprint (fallback)
- Migration strategies
- Post-deploy Auth0 wiring and verification checklist
