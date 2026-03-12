# Dev Setup — End-to-End

Two ways to run the full stack locally. **Hybrid mode** is recommended for day-to-day development.

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
1. `sqlserver` starts and passes its health check
2. `migrator` (SDK image) runs `dotnet ef database update` and exits
3. `api` starts only after `migrator` completes successfully

No local .NET toolchain required. Then start the frontend separately (it is not in the compose file):

```bash
cd frontend/fintrackpro-ui
cp .env.example .env          # VITE_API_BASE_URL=http://localhost:5000 is correct here
npm install
npm run dev
```

| Service | URL |
|---|---|
| Frontend | http://localhost:5173 |
| API | http://localhost:5000 |
| Keycloak | http://localhost:8080 |
| SQL Server | localhost:1433 |

> Keycloak realm and clients still need the one-time setup below before auth works.

---

## Mode B — Hybrid (recommended for development)

Infrastructure runs in Docker; API and frontend run locally so you get hot reload and debugger
support on both.

### Step 1 — Start infrastructure

```bash
# From repo root
docker compose up -d sqlserver keycloak
```

Wait ~15 seconds for SQL Server to be ready and Keycloak to finish booting.

### Step 2 — Keycloak one-time setup

> Skip this step if you have already configured the realm before.

#### 2a — Sign in and create the realm

1. Open http://localhost:8080 and sign in with **`admin`** / **`admin`**.
2. Hover over the realm name in the top-left (defaults to **`master`**) → click **Create Realm**.
3. Set **Realm name** to `fintrackpro` → click **Create**.

You should now be inside the `fintrackpro` realm.

---

#### 2b — Create the API client (`fintrackpro-api`)

This is the confidential backend client the API uses to validate tokens.

1. In the left sidebar go to **Clients** → click **Create client**.
2. **Step 1 — General settings**
   - Client type: `OpenID Connect`
   - Client ID: `fintrackpro-api`
   - Click **Next**.
3. **Step 2 — Capability config**
   - **Client authentication**: turn **On** (this makes it confidential)
   - **Authorization**: leave Off
   - Authentication flow: check only **Service accounts roles**
   - Click **Next**.
4. **Step 3 — Login settings** — leave all fields empty → click **Save**.

---

#### 2c — Create the SPA client (`fintrackpro-spa`)

This is the public client the React frontend uses for the Authorization Code flow.

1. In the left sidebar go to **Clients** → click **Create client**.
2. **Step 1 — General settings**
   - Client type: `OpenID Connect`
   - Client ID: `fintrackpro-spa`
   - Click **Next**.
3. **Step 2 — Capability config**
   - **Client authentication**: leave **Off** (public client — no secret)
   - Authentication flow: check **Standard flow** only
   - Click **Next**.
4. **Step 3 — Login settings**
   - Valid redirect URIs: `http://localhost:5173/*`
   - Valid post-logout redirect URIs: `http://localhost:5173`
   - Web origins: `http://localhost:5173`
   - Click **Save**.

---

#### 2d — Create a test user

1. In the left sidebar go to **Users** → click **Create new user**.
2. Set **Username** (e.g. `testuser`) → click **Create**.
3. Go to the **Credentials** tab → click **Set password**.
4. Enter a password, turn **Temporary** Off → click **Save** → **Save password**.

### Step 3 — Create and apply database migration (first time only)

> The Migrations folder is empty on a fresh clone — run this once before starting the API.
> SQL Server must already be running (Step 1) before applying migrations.

```bash
cd backend

dotnet ef migrations add InitialCreate \
  --project src/FinTrackPro.Infrastructure \
  --startup-project src/FinTrackPro.API

dotnet ef database update \
  --project src/FinTrackPro.Infrastructure \
  --startup-project src/FinTrackPro.API
```

This works because `appsettings.json` targets `localhost,1433` — the port that Docker maps from the `sqlserver` container.

### Step 4 — Run the API

```bash
cd backend
dotnet run --project src/FinTrackPro.API --launch-profile http
```

API listens on **http://localhost:5018** (defined in `Properties/launchSettings.json`).

Optional — set the Telegram bot token if testing notifications:

```bash
export Telegram__BotToken="your-token-here"
dotnet run --project src/FinTrackPro.API --launch-profile http
```

### Step 5 — Run the frontend

```bash
cd frontend/fintrackpro-ui
cp .env.example .env
```

Open `.env` and change the API URL to the local port:

```
VITE_API_BASE_URL=http://localhost:5018   # ← local API port, not 5000
VITE_KEYCLOAK_URL=http://localhost:8080
VITE_KEYCLOAK_REALM=fintrackpro
VITE_KEYCLOAK_CLIENT_ID=fintrackpro-spa
```

```bash
npm install
npm run dev
```

Frontend runs at **http://localhost:5173**.

---

## Port Reference

| Service | Mode | URL |
|---|---|---|
| Frontend | both | http://localhost:5173 |
| API (local) | hybrid | http://localhost:5018 |
| API (Docker) | full Docker | http://localhost:5000 |
| Keycloak | both | http://localhost:8080 |
| SQL Server | both | localhost:1433 |
| Scalar API docs | both | `<api-url>/scalar` |
| Hangfire dashboard | both | `<api-url>/hangfire` |

---

## Verifying the Stack

Run these checks in order after starting everything:

1. **`<api-url>/scalar`** loads → API is up and connected to SQL Server
2. **http://localhost:8080** shows the Keycloak login page → Auth service is ready
3. **http://localhost:5173** redirects to the Keycloak login page → Full E2E path is working
4. Log in with a Keycloak test user → Token is issued and accepted by the API

---

## Stopping the Stack

```bash
# Stop Docker services
docker compose down

# To also delete the SQL Server data volume (full reset)
docker compose down -v
```
