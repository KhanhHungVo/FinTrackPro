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
| SQL Server | localhost:1433 |

> Keycloak realm is imported automatically on first start — no manual setup needed.
> Log in with `admin@fintrackpro.dev` / `Admin1234!` (Admin role) or register a new account.

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

> The `fintrackpro` realm is **automatically provisioned** from `infra/docker/keycloak-realm.json`
> on first start. No manual Keycloak configuration is required. You can log in immediately with
> `admin@fintrackpro.dev` / `Admin1234!`, or register a new account at http://localhost:5173.
>
> The import is idempotent — if the realm already exists (volume persisted from a previous run)
> the JSON is silently skipped, so any manual changes you made are preserved.

<details>
<summary>Manual Keycloak setup reference (only needed for custom configs or starting from scratch)</summary>

If you ever need to re-create the realm manually (e.g. wiped the volume, added a new social login
provider, or changed redirect URIs), follow these steps.

#### Sign in and create the realm

1. Open http://localhost:8080 and sign in with **`admin`** / **`admin`**.
2. Hover over the realm name in the top-left (defaults to **`master`**) → click **Create Realm**.
3. Set **Realm name** to `fintrackpro` → click **Create**.

---

#### Create the API client (`fintrackpro-api`)

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
5. Go to the **Credentials** tab → copy the **Client secret** value.
   For dev use `dev-secret-change-in-prod` (matches `appsettings.Development.json`).

---

#### Create the SPA client (`fintrackpro-spa`)

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
   - Valid redirect URIs: `http://localhost:5173` and `http://localhost:5173/*`
   - Valid post-logout redirect URIs: `http://localhost:5173/`
   - Web origins: `http://localhost:5173`
   - Click **Save**.

---

#### Create a test user

1. In the left sidebar go to **Users** → click **Create new user**.
2. Set **Username** (e.g. `testuser`) → click **Create**.
3. Go to the **Credentials** tab → click **Set password**.
4. Enter a password, turn **Temporary** Off → click **Save** → **Save password**.

---

#### Add the audience mapper to `fintrackpro-spa`

By default Keycloak only includes `aud: account` in the access token. The API validates
that the token contains `aud: fintrackpro-api` — this mapper adds it.

1. Go to **Clients** → click `fintrackpro-spa`.
2. Click the **Client scopes** tab → click the `fintrackpro-spa-dedicated` link.
3. Click **Add mapper** → **By configuration** → choose **Audience**.
4. Fill in:
   - **Name**: `fintrackpro-api-audience`
   - **Included Custom Audience**: `fintrackpro-api`
   - **Add to access token**: On
5. Click **Save**.

> Without this step every API call returns `401 Audience validation failed`.

---

#### Enable self-registration and social login

**Self-registration:**

1. Go to **Realm settings** → **Login** tab.
2. Turn on **User registration** → click **Save**.

**Assign the `User` role automatically to every new registrant:**

1. Go to **Realm settings** → **User registration** tab.
2. Under **Default roles**, click **Add roles** → select `User` → **Assign**.

> **Assign Admin role manually:** Go to **Users** → select a user → **Role mappings** → assign the `Admin` realm role. Admin users can access the Hangfire dashboard at `/hangfire`.

**Google login (optional):**

1. Go to **Identity providers** → **Add provider** → **Google**.
2. Enter the **Client ID** and **Client Secret** from your [Google Cloud Console](https://console.cloud.google.com/) OAuth 2.0 credentials.
3. Click **Save**.

**Azure AD login (optional):**

1. Go to **Identity providers** → **Add provider** → **Microsoft**.
2. Enter the **Client ID** and **Client Secret** from your Azure App Registration.
3. Click **Save**.

> Social login providers are configured entirely in Keycloak. No changes to the application are needed.

</details>

### Step 2 — Create and apply database migration (first time only)

> The Migrations folder is empty on a fresh clone — run this once before starting the API.
> SQL Server must already be running (Step 1) before applying migrations.

```bash
cd backend

dotnet ef migrations add InitialCreate --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API

dotnet ef database update --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API
```

This works because `appsettings.json` targets `localhost,1433` — the port that Docker maps from the `sqlserver` container.

### Step 3 — Run the API

```bash
cd backend
dotnet run --project src/FinTrackPro.API --launch-profile http
```

API listens on **http://localhost:5018** (defined in `Properties/launchSettings.json`).

The dev Keycloak admin secret is already set in `appsettings.Development.json` so the nightly
`KeycloakUserSyncJob` works without any extra environment variables.

Optional — set environment variables before starting if you need Telegram notifications:

```bash
export Telegram__BotToken="your-token-here"
dotnet run --project src/FinTrackPro.API --launch-profile http
```

### Step 4 — Run the frontend

```bash
cd frontend/fintrackpro-ui
cp .env.example .env
```

Open `.env` and change the API URL to the local port:

```
VITE_API_BASE_URL=http://localhost:5018   # local API port
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
| API (Docker) | full Docker | http://localhost:5018 |
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
4. Log in as `admin@fintrackpro.dev` / `Admin1234!` → Token is issued and accepted by the API

---

## Stopping the Stack

```bash
# Stop Docker services
docker compose down

# To also delete the SQL Server data volume (full reset)
docker compose down -v
```
