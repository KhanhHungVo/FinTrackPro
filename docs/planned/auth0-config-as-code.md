# Plan: Auth0 Config-as-Code via `auth0 deploy`

> ⚠️ **PLAN — not yet implemented.** These files and scripts do not exist yet. See [docs/roadmap.md](../roadmap.md) for implementation status.

## Context

Auth0 currently requires 6 manual dashboard steps to provision from scratch. The goal is to
mirror the Keycloak pattern — where `infra/docker/keycloak-realm.json` auto-provisions
everything on `docker compose up` — by adding an `infra/auth0/` directory that can be
deployed with a single command using the official Auth0 CLI.

---

## Files to Create

### `infra/auth0/tenant.yaml`
Top-level CLI manifest. References all resource files. Includes prereq comments and
post-deploy checklist (secrets must be set manually — Auth0 CLI security restriction).

### `infra/auth0/resource-servers/fintrackpro-api.json`
```json
{
  "name": "fintrackpro-api",
  "identifier": "https://api.fintrackpro.dev",
  "signing_alg": "RS256",
  "enforce_policies": true,
  "token_dialect": "access_token_authz",
  "allow_offline_access": false,
  "skip_consent_for_verifiable_first_party_clients": true
}
```
`enforce_policies: true` = Enable RBAC. `token_dialect: "access_token_authz"` = Add Permissions in Access Token.

### `infra/auth0/clients/fintrackpro-spa.json`
SPA app: `app_type: "spa"`, callbacks `http://localhost:5173` + `/callback`,
logout/web-origins `http://localhost:5173`, `token_endpoint_auth_method: "none"`, RS256.

### `infra/auth0/clients/fintrackpro-m2m.json`
M2M app: `app_type: "non_interactive"`, `grant_types: ["client_credentials"]`, RS256.

### `infra/auth0/client-grants/fintrackpro-m2m-mgmt.json`
```json
{
  "client_id": "fintrackpro-m2m",
  "audience": "https://YOUR_TENANT.auth0.com/api/v2/",
  "scope": ["read:users", "read:roles", "update:users"]
}
```
Uses `YOUR_TENANT` placeholder — substituted by `deploy.sh` via `sed` using `AUTH0_DOMAIN`.

### `infra/auth0/roles/User.json` + `infra/auth0/roles/Admin.json`
Simple `{ "name": "...", "description": "..." }` files.

### `infra/auth0/actions/inject-roles-into-token/code.js`
Exact current action code (auto-assign User role + inject custom claim). Includes
comment header reminding developer to set secrets in dashboard after deploy.

### `infra/auth0/actions/inject-roles-into-token/metadata.json`
```json
{
  "name": "inject-roles-into-token",
  "supported_triggers": [{ "id": "post-login", "version": "v3" }],
  "code": "./code.js",
  "dependencies": [{ "name": "auth0", "version": "4" }],
  "secrets": [],
  "runtime": "node18"
}
```
`secrets: []` is intentional — CLI blocks secret injection by design.

### `infra/auth0/triggers/post-login.json`
```json
[{ "action_name": "inject-roles-into-token", "display_name": "Inject Roles into Token" }]
```
Wires action between Start and Complete in the post-login trigger.

### `infra/auth0/deploy.sh`
Bash wrapper that:
1. Validates `AUTH0_DOMAIN` is set
2. `sed`-substitutes `YOUR_TENANT` → `$AUTH0_DOMAIN` in the grant file (temp copy)
3. Runs `auth0 deploy --input-file infra/auth0/tenant.yaml`
4. Prints post-deploy checklist (secrets + copy Client IDs)

---

## Files to Modify

### `docs/auth-setup.md`
- Update overview table: Auth0 "Setup effort" → `One command (CLI deploy)`
- Replace "One-time Auth0 Dashboard configuration" heading + 6 manual steps with:
  1. Install Auth0 CLI
  2. `auth0 login`
  3. `AUTH0_DOMAIN=your-tenant.auth0.com ./infra/auth0/deploy.sh`
  4. Manual secrets step (cannot be automated)
  5. Copy Client IDs to config files
- Wrap the existing 6-step manual instructions in a `<details>` collapse block as fallback reference

### `README.md`
- Update the repo structure / infra section to mention `infra/auth0/` alongside `infra/docker/`

---

## Known CLI Limitations (cannot be automated)

| Item | Why | Workaround |
|---|---|---|
| Action secrets | Auth0 CLI refuses to write secrets to prevent plaintext in git | Set 3 secrets in dashboard after deploy |
| Client IDs | Auth0 generates them server-side | Copy from dashboard to `.env` / `appsettings` |
| Management API audience | Tenant-specific URL | `deploy.sh` substitutes via `sed` |
| Admin role assignment | Intentionally manual | Same as Keycloak |

---

## Verification

1. Install CLI: `npm install -g auth0-cli`
2. `auth0 login`
3. `AUTH0_DOMAIN=dev-n0o3nkoltcw46fiu.us.auth0.com ./infra/auth0/deploy.sh`
4. Check Auth0 dashboard — all entities present
5. Set action secrets in dashboard → Deploy action
6. Copy `fintrackpro-spa` and `fintrackpro-m2m` Client IDs to config
7. Login via frontend → JWT contains `https://fintrackpro.dev/roles: ["User"]`
8. API returns 200 (not 401)
