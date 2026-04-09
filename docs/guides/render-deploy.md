# Deploy FinTrackPro to Render

Terraform is the authoritative deployment tool. State is stored in Terraform Cloud (free tier).
The configuration lives in [infra/terraform/](../infra/terraform/).

The `render.yaml` Blueprint remains in the repo as a fallback for manual one-click deploys.

---

## Prerequisites

- [Terraform CLI >= 1.7](https://developer.hashicorp.com/terraform/install) installed
- A [Render account](https://render.com) with the repo connected
- Auth0 tenant configured — see [auth-setup.md](auth-setup.md)
- **No external database required** — PostgreSQL is provisioned by Terraform on Render free tier

---

## Option A — Terraform (recommended)

### Step 1 — Update the repo URL in render.tf

In [infra/terraform/render.tf](../infra/terraform/render.tf), replace both placeholder URLs:

```hcl
repo_url = "https://github.com/your-org/FinTrackPro"
```

with your actual GitHub repo URL. There are two occurrences — one per service resource.

### Step 2 — Create Terraform Cloud org and workspace

1. Sign up at [app.terraform.io](https://app.terraform.io)
2. Create organization **`fintrackpro`** (must match `main.tf` → `organization = "fintrackpro"`, or change both)
3. Create workspace **`fintrackpro-prod`** → run type: **API-driven**

### Step 3 — Add variables to the workspace

In workspace → **Variables** tab, add each as a **Terraform variable**.

| Variable | Value | Sensitive? |
|---|---|---|
| `render_api_key` | Render → Account → API Keys | Yes |
| `render_owner_id` | Run `curl` command below to get `tea-...` / `usr-...` ID | No |
| `auth0_domain` | `dev-xxxx.us.auth0.com` | No |
| `auth0_m2m_client_id` | Auth0 M2M app client ID | No |
| `auth0_m2m_client_secret` | Auth0 M2M app client secret | Yes |
| `cors_origins` | `https://fintrackpro-ui.onrender.com` (update after first deploy) | No |
| `coingecko_api_key` | CoinGecko Demo or Pro API key | Yes |
| `exchangerate_api_key` | ExchangeRate-API v6 key | Yes |
| `telegram_bot_token` | Telegram bot token — leave `""` if unused | Yes |
| `hangfire_dashboard_password` | Strong password for Hangfire Basic Auth | Yes |
| `stripe_secret_key` | Stripe secret API key (`sk_live_...` or `sk_test_...`) | Yes |
| `stripe_webhook_secret` | Stripe webhook endpoint signing secret (`whsec_...`) | Yes |
| `stripe_price_id` | Stripe Price ID for the Pro plan (`price_...`) | No |
| `vite_api_base_url` | `https://fintrackpro-api.onrender.com` (update after first deploy) | No |
| `vite_auth0_domain` | Same as `auth0_domain` | No |
| `vite_auth0_client_id` | Auth0 SPA app client ID | No |
| `vite_admin_telegram` | Telegram handle shown in bank transfer modal | No |
| `vite_admin_email` | Admin email shown in bank transfer modal | No |
| `vite_bank_name` | Bank name displayed in transfer details (e.g. `Techcombank`) | No |
| `vite_bank_account_number` | Bank account number displayed in transfer details | No |
| `vite_bank_account_name` | Account holder name displayed in transfer details | No |
| `vite_bank_transfer_amount` | Monthly Pro price in VND — defaults to `99000` if omitted | No |
| `vite_bank_qr_url` | Direct URL to the bank QR image (postimages.org or GitHub release asset) | No |

> `db_connection_string` is no longer required — the PostgreSQL instance is provisioned by
> Terraform and its internal connection string is auto-injected into the API.

**Finding your `render_owner_id`:**

```bash
curl -s -H "Authorization: Bearer <your_render_api_key>" \
  https://api.render.com/v1/owners?limit=1
# Look for "id": "tea-..." or "usr-..." in the response
```

### Step 4 — Authenticate and deploy

```bash
terraform login                  # one-time — opens browser to generate API token

cd infra/terraform
terraform init                   # downloads render-oss/render provider, connects to TF Cloud
terraform plan                   # preview: should show 4 resources to create
terraform apply                  # type "yes" — deploys project, DB, API, and frontend
```

Terraform outputs the deployed URLs:

```
api_url      = "https://fintrackpro-api.onrender.com"
frontend_url = "https://fintrackpro-ui.onrender.com"
```

### Step 5 — Run database migrations

The Render PostgreSQL instance is empty after provisioning. Apply migrations from your local machine using the external DB URL:

```bash
# Retrieve the external DB URL (sensitive — not shown in plan output)
cd infra/terraform
terraform output -raw db_external_url

# DatabaseProvider:Provider is not sensitive — set as env var
export DatabaseProvider__Provider=postgresql

# Connection string contains credentials — use user-secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<external-url>" \
  --project backend/src/FinTrackPro.API

# Run migrations
cd backend
dotnet ef database update \
  --project src/FinTrackPro.Infrastructure \
  --startup-project src/FinTrackPro.API
```

### Step 6 — Post-deploy wiring

**Update CORS and frontend API URL** — go back to TF Cloud workspace variables:

| Variable | New value |
|---|---|
| `cors_origins` | value of `api_url` output |
| `vite_api_base_url` | value of `api_url` output |

Then re-apply: `terraform apply`

**Update Auth0 SPA settings** — Auth0 dashboard → your SPA app → Settings:

| Field | Value |
|---|---|
| Allowed Callback URLs | `https://fintrackpro-ui.onrender.com/callback` |
| Allowed Logout URLs | `https://fintrackpro-ui.onrender.com` |
| Allowed Web Origins | `https://fintrackpro-ui.onrender.com` |

---

## Option B — render.yaml Blueprint (fallback)

Use this if you prefer a one-click Render dashboard deploy without Terraform.

### Prerequisites

- Auth0 configured (see [auth-setup.md](auth-setup.md))
- `render.yaml` committed to `main`

### Deploy steps

1. Push to `main`
2. Render dashboard → **New** → **Blueprint** → connect the repo
3. Render detects the `databases:` section and both services
4. The `fintrackpro-db` PostgreSQL instance is created automatically
5. `ConnectionStrings__DefaultConnection` is wired from the database via `fromDatabase`
6. Enter all `sync: false` secret values when prompted:

   | Variable | Value |
   |---|---|
   | `IdentityProvider__AdminClientId` | Auth0 M2M app client ID |
   | `IdentityProvider__AdminClientSecret` | Auth0 M2M app client secret |
   | `Auth0__Domain` | e.g. `dev-abc123.us.auth0.com` |
   | `Cors__Origins` | Frontend URL — set after first deploy |
   | `CoinGecko__ApiKey` | Demo or Pro API key |
   | `Telegram__BotToken` | Optional |
   | `Hangfire__Password` | Strong password |

   **Frontend service** (`VITE_*` are build-time — must be set before `npm run build` runs)

   | Variable | Value |
   |---|---|
   | `VITE_API_BASE_URL` | API Render URL |
   | `VITE_AUTH0_DOMAIN` | e.g. `dev-abc123.us.auth0.com` |
   | `VITE_AUTH0_CLIENT_ID` | Auth0 SPA client ID |
   | `VITE_ADMIN_TELEGRAM` | Telegram handle shown in bank transfer modal |
   | `VITE_ADMIN_EMAIL` | Admin email shown in bank transfer modal |
   | `VITE_BANK_NAME` | Bank name (e.g. `Techcombank`) |
   | `VITE_BANK_ACCOUNT_NUMBER` | Bank account number |
   | `VITE_BANK_ACCOUNT_NAME` | Account holder name |
   | `VITE_BANK_TRANSFER_AMOUNT` | Monthly Pro price in VND (default `99000`) |
   | `VITE_BANK_QR_URL` | Direct URL to the bank QR image |

7. Click **Apply** — all resources build in parallel (~3–5 min for .NET Docker build)
8. After deploy: copy the frontend URL → update `Cors__Origins` → redeploy API
9. Run migrations (see Step 5 above using `db_external_url` from the Render dashboard → Database → Connections)

---

## Migrations

The API Docker image (`aspnet:10.0`) has no .NET SDK — `dotnet ef` cannot run in the container. Apply migrations **from your local machine** before any schema-changing deploy:

```bash
export DatabaseProvider__Provider=postgresql
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<external-url>" \
  --project backend/src/FinTrackPro.API

cd backend
dotnet ef database update \
  --project src/FinTrackPro.Infrastructure \
  --startup-project src/FinTrackPro.API
```

The external PostgreSQL URL is available via:
- **Terraform**: `terraform output -raw db_external_url`
- **Render dashboard**: Database → `fintrackpro-db` → Connections → External Database URL

| Strategy | When to use |
|---|---|
| **Manual** (above) | Current approach — simple, full control |
| **Startup migration** (`db.Database.Migrate()` in `Program.cs`) | Zero-touch PaaS pattern; idempotent but couples migration to app boot |
| **GitHub Actions** | Best for teams — migration runs in CI before Render deploys |

---

## Render Free-Tier PostgreSQL Constraints

| Constraint | Impact |
|---|---|
| DB expires after 90 days unless upgraded | Render emails a warning; upgrade or recreate before expiry |
| 256 MB RAM, 1 GB storage | Suitable for dev / low-traffic prod |
| No high availability | Acceptable for free tier |
| Internal URL only reachable within Render | Use external URL for local `dotnet ef` migrations |

---

## Verification

| Check | How |
|---|---|
| API health | `curl https://fintrackpro-api.onrender.com/health` → `{"status":"healthy"}` |
| Frontend loads | Open `https://fintrackpro-ui.onrender.com` |
| Auth0 login | Complete login flow, land on dashboard |
| SPA routing | Hard refresh on `/dashboard` → no 404 |

---

## Ongoing Deployments

Code pushes to `main` trigger auto-deploys on Render (both services have `auto_deploy = true`).
`terraform apply` is only needed for infrastructure changes (env vars, plan tier, new services).
