# End-to-End Test Guide

Manual test checklist for all four provider × mode combinations. Run in order — each scenario starts from a clean slate.

## Contents

- [Pre-flight Checks](#pre-flight-checks)
- [Test 1 — Keycloak + Full Docker](#test-1--keycloak--full-docker)
- [Test 2 — Keycloak + Hybrid](#test-2--keycloak--hybrid)
- [Test 3 — Auth0 + Hybrid](#test-3--auth0--hybrid)
- [Test 4 — Auth0 + Full Docker](#test-4--auth0--full-docker)

---

## Pre-flight Checks

Do these once before running any test scenario.

- [ ] Docker Desktop is running
- [ ] Auth0 dashboard one-time setup is complete (see [docs/auth-setup.md](auth-setup.md#auth0-cloud-iam))
- [ ] `frontend/fintrackpro-ui/.env` exists (copy from `.env.example` if missing)
- [ ] `backend/src/FinTrackPro.API/appsettings.Development.json` exists with secrets (see config snippets in each test below)
- [ ] Repo-root `.env` exists with `TELEGRAM_BOT_TOKEN` and Auth0 vars (copy from `.env.example` if missing)

---

## Test 1 — Keycloak + Full Docker

**Goal:** Verify the default out-of-the-box experience — zero local toolchain required for the backend.

### Setup

```bash
# 1. Clean slate
docker compose down -v

# 2. Start everything
docker compose up --build

# 3. Frontend (new terminal — not in compose)
cd frontend/fintrackpro-ui
cp .env.example .env    # default VITE_AUTH_PROVIDER=keycloak is correct
npm install
npm run dev
```

No config changes needed. Keycloak realm is auto-imported on first boot.

### Checklist

- [ ] `http://localhost:5018/scalar` loads → API is up
- [ ] `http://localhost:8080` shows Keycloak login page → Auth service ready
- [ ] `http://localhost:5173` redirects to Keycloak login page → E2E path working
- [ ] **Register** a new account → lands on app dashboard, `User` role active
- [ ] **Login** as `admin@fintrackpro.dev` / `Admin1234!` → Admin role active
- [ ] `http://localhost:5018/hangfire` loads → Hangfire dashboard accessible (Admin only)
- [ ] **Create a transaction** via UI → appears in transaction list
- [ ] **Create a budget** → appears in budget list
- [ ] **Log out** → redirected back to Keycloak login page

### Teardown

```bash
docker compose down -v
```

---

## Test 2 — Keycloak + Hybrid

**Goal:** Verify the recommended daily-dev workflow — hot reload on API and frontend with Keycloak running in Docker.

### Setup

**`backend/src/FinTrackPro.API/appsettings.Development.json`** (create if missing — gitignored):

```json
{
  "IdentityProvider": {
    "Provider": "keycloak",
    "AdminClientId": "fintrackpro-api",
    "AdminClientSecret": "dev-secret-change-in-prod"
  },
  "Keycloak": {
    "Authority": "http://localhost:8080/realms/fintrackpro"
  }
}
```

**`frontend/fintrackpro-ui/.env`:**

```
VITE_API_BASE_URL=http://localhost:5018
VITE_AUTH_PROVIDER=keycloak
VITE_KEYCLOAK_URL=http://localhost:8080
VITE_KEYCLOAK_REALM=fintrackpro
VITE_KEYCLOAK_CLIENT_ID=fintrackpro-spa
```

```bash
# 1. Clean slate
docker compose down -v

# 2. Start infrastructure only
docker compose up -d sqlserver keycloak
# Wait ~15 s for both to be ready

# 3. Run migrations (first time only — skip if Migrations/ folder already has files)
cd backend
dotnet ef migrations add InitialCreate \
  --project src/FinTrackPro.Infrastructure \
  --startup-project src/FinTrackPro.API
dotnet ef database update \
  --project src/FinTrackPro.Infrastructure \
  --startup-project src/FinTrackPro.API

# 4. Start API (keep terminal open)
dotnet run --project src/FinTrackPro.API --launch-profile http

# 5. Start frontend (new terminal)
cd frontend/fintrackpro-ui
npm run dev
```

### Checklist

- [ ] `http://localhost:5018/scalar` loads
- [ ] `http://localhost:5173` redirects to Keycloak login
- [ ] Login / register works
- [ ] Create a transaction → appears in list
- [ ] Create a budget → appears in list
- [ ] Edit a source file in the API → dotnet watch reloads without losing the Keycloak session
- [ ] Log out works

### Teardown

```bash
# Stop API: Ctrl+C in that terminal
docker compose down -v
```

---

## Test 3 — Auth0 + Hybrid

**Goal:** Verify Auth0 cloud IAM works with locally running API and frontend — no Keycloak, no Keycloak Docker container.

### Setup

**`backend/src/FinTrackPro.API/appsettings.Development.json`** — switch to Auth0:

```json
{
  "IdentityProvider": {
    "Provider": "auth0",
    "AdminClientId": "<fintrackpro-m2m Client ID from Auth0 dashboard>",
    "AdminClientSecret": "<fintrackpro-m2m Client Secret>"
  },
  "Auth0": {
    "Domain": "your-tenant.auth0.com"
  }
}
```

**`frontend/fintrackpro-ui/.env`:**

```
VITE_API_BASE_URL=http://localhost:5018
VITE_AUTH_PROVIDER=auth0
VITE_AUTH0_DOMAIN=your-tenant.auth0.com
VITE_AUTH0_CLIENT_ID=<fintrackpro-spa Client ID from Auth0 dashboard>
VITE_AUTH0_AUDIENCE=https://api.fintrackpro.dev
```

```bash
# 1. Clean slate
docker compose down -v

# 2. Start SQL Server only (no Keycloak)
docker compose up -d sqlserver
# Wait ~15 s

# 3. Run migrations (if not already applied)
cd backend
dotnet ef database update \
  --project src/FinTrackPro.Infrastructure \
  --startup-project src/FinTrackPro.API

# 4. Start API
dotnet run --project src/FinTrackPro.API --launch-profile http

# 5. Start frontend (new terminal)
cd frontend/fintrackpro-ui
npm run dev
```

### Checklist

- [ ] `http://localhost:5018/scalar` loads — API starts successfully without Keycloak
- [ ] `http://localhost:5173` redirects to **Auth0 Universal Login** (not Keycloak)
- [ ] Login with an Auth0 account → redirected back to app dashboard
- [ ] Verify JWT has `https://fintrackpro.dev/roles` claim:
  - Get token from browser DevTools (Application → Local Storage or Network tab)
  - Paste into [jwt.io](https://jwt.io) and confirm the roles claim is present
- [ ] API calls succeed — create a transaction, create a budget
- [ ] Register a new Auth0 account → `User` role auto-assigned (if Post-Registration Action is wired)
- [ ] Log out → redirected to Auth0 logout page

### Teardown

```bash
docker compose down -v
```

---

## Test 4 — Auth0 + Full Docker

**Goal:** Verify the full stack (SQL Server + migrator + API) runs in Docker while Auth0 handles auth from the cloud.

**Pre-requisite:** `docker-compose.auth0.yml` exists at repo root (already created — see [docs/auth-setup.md](auth-setup.md#auth0-with-full-docker)).

### Setup

**Repo-root `.env`** — add Auth0 secrets (gitignored):

```
TELEGRAM_BOT_TOKEN=your_actual_token_here
AUTH0_DOMAIN=your-tenant.auth0.com
AUTH0_M2M_CLIENT_ID=<fintrackpro-m2m Client ID>
AUTH0_M2M_CLIENT_SECRET=<fintrackpro-m2m Client Secret>
```

**`frontend/fintrackpro-ui/.env`** — same as Test 3 (Auth0 values).

```bash
# 1. Clean slate
docker compose down -v

# 2. Start stack with Auth0 override (no Keycloak started)
docker compose -f docker-compose.yml -f docker-compose.auth0.yml up --build

# 3. Frontend (new terminal — not in compose)
cd frontend/fintrackpro-ui
npm run dev
```

### Checklist

- [ ] Docker starts without a `fintrackpro-keycloak` container (confirm: `docker ps`)
- [ ] `http://localhost:5018/scalar` loads
- [ ] `http://localhost:5173` redirects to Auth0 Universal Login
- [ ] Login works → app dashboard loads
- [ ] Create a transaction → appears in list
- [ ] Create a budget → appears in list
- [ ] Log out works
- [ ] `docker compose -f docker-compose.yml -f docker-compose.auth0.yml down` cleanly stops all containers

### Teardown

```bash
docker compose -f docker-compose.yml -f docker-compose.auth0.yml down -v
```
