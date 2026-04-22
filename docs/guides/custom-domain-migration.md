# Custom Domain Migration — fintrackpilot.com

Migrate FinTrackPro production from `*.onrender.com` / `dev-n0o3nkoltcw46fiu.us.auth0.com` to custom domains under `fintrackpilot.com` (registered on Cloudflare).

## Domain Map

| Service | Old URL | New URL |
|---|---|---|
| Frontend (`fintrackpro-ui`) | `https://fintrackpro-ui.onrender.com` | `https://www.fintrackpilot.com` |
| API (`fintrackpro-api`) | `https://fintrackpro-api.onrender.com` | `https://api.fintrackpilot.com` |
| Auth0 tenant | `https://dev-n0o3nkoltcw46fiu.us.auth0.com` | `https://auth.fintrackpilot.com` |

> The Auth0 API audience identifier (`https://api.fintrackpro.dev`) does **not** change — it is a logical identifier, not a hostname. Changing it would invalidate all existing tokens.

---

## Phase 1 — Cloudflare DNS

In the Cloudflare dashboard for `fintrackpilot.com` → **DNS** → **Records**, add:

| Type | Name | Target | Proxy status |
|---|---|---|---|
| `CNAME` | `www` | `fintrackpro-ui.onrender.com` | **Proxied** (orange cloud) |
| `CNAME` | `api` | `fintrackpro-api.onrender.com` | **Proxied** (orange cloud) |
| `CNAME` | `auth` | `<value Auth0 provides in Phase 3>` | **DNS only** (grey cloud) |

> Add the `auth` CNAME **after** completing Phase 3 — Auth0 provides the exact target value.
> Auth0 custom domain verification **requires** DNS-only (grey cloud). Proxied will fail verification.

---

## Phase 2 — Render: Add Custom Domains

### fintrackpro-ui (Static Site)

1. Render dashboard → `fintrackpro-ui` → **Settings** → **Custom Domains** → **+ Add Custom Domain**
2. Enter `www.fintrackpilot.com` → **Save**
3. Render shows a CNAME verification record — it should match `fintrackpro-ui.onrender.com` (already added in Phase 1)
4. Wait for Render to show the domain as **Verified** (usually < 5 min once DNS propagates)

### fintrackpro-api (Web Service)

1. Render dashboard → `fintrackpro-api` → **Settings** → **Custom Domains** → **+ Add Custom Domain**
2. Enter `api.fintrackpilot.com` → **Save**
3. Render verifies against the CNAME added in Phase 1

---

## Phase 3 — Auth0: Add Custom Domain

> Auth0 custom domains require a **paid Auth0 plan** (Essentials or above).
> The screenshots confirm this is available ("Early Access" — feature is enabled on this tenant).

1. Auth0 dashboard → **Branding** → **Custom Domains** → **+ Add Custom Domain**
2. Enter `auth.fintrackpilot.com`
3. Certificate Type: **Auth0-managed certificates**
4. Click **Add Domain**
5. Auth0 displays a CNAME record, e.g.:
   ```
   auth.fintrackpilot.com  →  <hash>.edge.tenants.auth0.com
   ```
6. Add that CNAME to Cloudflare as **grey cloud (DNS only)**
7. Back in Auth0 → click **Verify** — wait until status turns green (5–15 min)

### Google Social Login — Fix redirect_uri_mismatch

If Google login is configured on this Auth0 tenant, Google Cloud Console must whitelist Auth0's callback URL. Without this, Google returns `400: redirect_uri_mismatch`.

1. [Google Cloud Console](https://console.cloud.google.com/) → **APIs & Services** → **Credentials** → click your OAuth 2.0 Client ID
2. Under **Authorized redirect URIs**, add both:
   ```
   https://dev-n0o3nkoltcw46fiu.us.auth0.com/login/callback
   https://auth.fintrackpilot.com/login/callback
   ```
3. Click **Save** — takes ~5 min to propagate

> Keep both URIs during the migration transition. Once `VITE_AUTH0_DOMAIN` is switched to `auth.fintrackpilot.com` and verified end-to-end (Phase 7), the original tenant URI can be removed.

---

## Phase 4 — Auth0: Update SPA Allowed URLs

Auth0 dashboard → **Applications** → `fintrackpro-spa` → **Settings** tab.

Add the new URLs to each field (comma-separated, keep existing `onrender.com` entries until verified):

| Field | Add |
|---|---|
| **Allowed Callback URLs** | `https://www.fintrackpilot.com, https://www.fintrackpilot.com/callback` |
| **Allowed Logout URLs** | `https://www.fintrackpilot.com` |
| **Allowed Web Origins** | `https://www.fintrackpilot.com` |

Click **Save Changes**.

> After verifying the custom domain works end-to-end (Phase 7), you can remove the `onrender.com` entries.

---

## Phase 5 — Terraform Cloud: Update Workspace Variables

In Terraform Cloud workspace `fintrackpro-prod` → **Variables** tab, update:

| Variable | Old value | New value |
|---|---|---|
| `cors_origins` | `https://fintrackpro-ui.onrender.com` | `https://www.fintrackpilot.com` |
| `vite_api_base_url` | `https://fintrackpro-api.onrender.com` | `https://api.fintrackpilot.com` |
| `vite_auth0_domain` | `dev-n0o3nkoltcw46fiu.us.auth0.com` | `auth.fintrackpilot.com` |
| `auth0_domain` | `dev-n0o3nkoltcw46fiu.us.auth0.com` | `auth.fintrackpilot.com` |

> `vite_auth0_domain` and `auth0_domain` (backend `Auth0__Domain`) **must be updated together**. The backend validates the JWT `iss` claim against `Auth0__Domain` — if they differ, every API request returns 401.

Then re-deploy:

```bash
cd infra/terraform
terraform apply
```

Terraform will redeploy both services with the new environment variables. The frontend rebuild injects the new `VITE_*` values at build time.

---

## Phase 6 — GitHub Secrets: Update DB Rotation Workflow

In GitHub → **Settings** → **Secrets and Variables** → **Actions**, update:

| Name | Old value | New value |
|---|---|---|
| `API_HEALTH_URL` | `https://fintrackpro-api.onrender.com/health` | `https://api.fintrackpilot.com/health` |

This affects the automated DB rotation workflow (`.github/workflows/db-rotation.yml`).

---

## Phase 7 — Verification Checklist

Run these checks in order after all phases are complete:

| Check | How | Expected |
|---|---|---|
| Frontend loads | Open `https://www.fintrackpilot.com` | App landing page renders |
| Auth redirect | Click **Login** | Browser redirects to `https://auth.fintrackpilot.com/...` (not `dev-n0o3...auth0.com`) |
| Login completes | Complete login flow | Lands back on `https://www.fintrackpilot.com/dashboard` |
| API calls | Open browser DevTools → Network tab | All API calls go to `https://api.fintrackpilot.com/...` |
| API health | `curl https://api.fintrackpilot.com/health` | `{"status":"healthy"}` |
| Cloudflare proxy | Check response headers | `cf-ray` header present on `www` and `api` |
| Auth0 domain | Check login page URL | Shows `auth.fintrackpilot.com`, not `auth0.com` subdomain |

---

## Phase 8 — Cleanup (after verification)

Once the custom domain is fully verified and stable:

1. **Auth0 SPA settings** — remove the old `onrender.com` entries from Allowed Callback URLs, Logout URLs, and Web Origins
2. **Render Subdomain** — optionally disable `*.onrender.com` subdomain on each service (Render → Settings → Render Subdomain → toggle off) to serve exclusively from custom domains
3. **Terraform variable `cors_origins`** — remove `https://fintrackpro-ui.onrender.com` if it was kept as a fallback

---

## Files to Update in Repo (post-migration)

| File | What to update |
|---|---|
| `docs/guides/auth-setup.md` | Render Deployment section — replace `fintrackpro-ui.onrender.com` with `www.fintrackpilot.com` |
| `docs/guides/render-deploy.md` | Post-deploy wiring tables, Auth0 SPA settings table, verification table |
| `docs/guides/dev-setup.md` | No changes needed (local dev uses `localhost`) |
| `docs/guides/security-hardening.md` | Any production domain references |
