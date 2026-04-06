# Backend API E2E Tests — Plan & Reference

## Two-Collection Strategy

The Postman collections are split into two files with distinct purposes:

| | `FinTrackPro.e2e.postman_collection.json` | `FinTrackPro.dev.postman_collection.json` |
|---|---|---|
| **Purpose** | CI gate — must be green to merge | Developer exploration and manual testing |
| **Run by** | `scripts/api-e2e-local.sh`, CI `backend-api-e2e` job | Manually in Postman desktop or Newman ad hoc |
| **Organized by** | Flow (lifecycle, guards, contracts) | Resource (Trades, Transactions, Budgets, etc.) |
| **Assertions** | Full flow correctness, 403 ownership guards, CoinGecko contract | Lightweight: status code + schema shape only |
| **Token minting** | Auth folder test script (preserves 401-first ordering) | Collection-level prerequest with expiry cache |
| **Validation tests** | No (moved to dev) | Yes — `Validation & Error Cases` folder |
| **`--bail`** | Yes | Optional |

### When to run each

- **E2E collection**: Run before every merge. `bash scripts/api-e2e-local.sh`
- **Dev collection**: Run when adding a new endpoint, debugging a response shape, or verifying a specific request in isolation.

```bash
# Dev collection — full run
newman run docs/postman/FinTrackPro.dev.postman_collection.json \
  -e docs/postman/FinTrackPro.postman_environment.json \
  --env-var "baseUrl=http://localhost:5018" \
  --env-var "keycloakUrl=http://localhost:8080" \
  --env-var "testUsername=admin@fintrackpro.dev" \
  --env-var "testPassword=Admin1234!" \
  -r cli

# Dev collection — single folder
newman run docs/postman/FinTrackPro.dev.postman_collection.json \
  -e docs/postman/FinTrackPro.postman_environment.json \
  --folder "Trades" \
  --env-var "baseUrl=http://localhost:5018" \
  --env-var "keycloakUrl=http://localhost:8080" \
  --env-var "testUsername=admin@fintrackpro.dev" \
  --env-var "testPassword=Admin1234!"
```

> **Retired:** `FinTrackPro.postman_collection.json` is superseded by the two collections above.

---

## Context

The project has solid unit and integration tests (`WebApplicationFactory` + Respawn + real PostgreSQL) but no automated test suite that hits a **real running API process with a real Keycloak-issued JWT**. That gap means the full auth stack (JWT signing, `iss`/`aud` validation, `KeycloakClaimsTransformer`, middleware ordering) is only exercised manually.

This plan adds a Newman-based API E2E test suite using the existing Postman collection, wires it into CI as a dedicated `Backend — API E2E` job.

### How this differs from existing integration tests

| | `FinTrackPro.Api.IntegrationTests` | Newman (this suite) |
|---|---|---|
| API process | In-process (`WebApplicationFactory`) | Real running Docker container |
| JWT | Locally minted (`AuthTokenFactory`) | Real Keycloak-issued token |
| Auth stack tested | No (bypassed via test middleware) | Yes — full `iss`/`aud`/claims validation |
| External services | Mocked | Real (market endpoints) |
| Speed | Fast | Slower (Keycloak + Docker startup) |
| When to run | Every PR | After `Backend — Build & Test` passes |

---

## Decisions

### IAM client: reuse `fintrackpro-e2e`

`infra/docker/keycloak-realm.json` already has a `fintrackpro-e2e` client with:
- `directAccessGrantsEnabled: true` (Resource Owner Password Credentials flow)
- Audience mapper for `https://api.fintrackpro.dev`
- No browser redirect flows

This is already used by Playwright E2E. No new client needed — one dedicated E2E client per environment is standard practice.

### Two test users

The realm has one user: `admin@fintrackpro.dev` (Admin + User roles).
A second user `user2@fintrackpro.dev` (User role only) is added for 403 ownership guard tests.

---

## Files changed

| File | Change |
|---|---|
| `docs/postman/FinTrackPro.e2e.postman_collection.json` | **New** — CI-gating E2E collection (Auth, lifecycle flows, Authorization Guards, Market) |
| `docs/postman/FinTrackPro.dev.postman_collection.json` | **New** — Dev reference collection (all endpoints by resource, Validation & Error Cases) |
| `docs/postman/FinTrackPro.postman_collection.json` | **Retired** — superseded by the two collections above |
| `docs/postman/FinTrackPro.postman_environment.json` | Add `keycloakUrl`, `testUsername/2`, `testPassword/2`, `bearerToken2` |
| `scripts/api-e2e-local.sh` | Updated `COLLECTION` to reference `FinTrackPro.e2e.postman_collection.json` |
| `.github/workflows/ci.yml` | Updated Newman `run` path to `FinTrackPro.e2e.postman_collection.json` |
| `infra/docker/keycloak-realm.json` | Add `user2@fintrackpro.dev` to `users` array |
| `.gitignore` | Add `test-results/` (lowercase) |

---

## E2E collection structure (`FinTrackPro.e2e.postman_collection.json`)

Folders run in this order (Newman `--bail` stops on first failure):

```
Auth/
  └─ No token → GET /api/transactions → 401

Trades — Full lifecycle/
  └─ POST   /api/trades              → 201, capture tradeId
  └─ GET    /api/trades              → 200, trade in list, result computed correctly
  └─ PUT    /api/trades/{{tradeId}}  → 200, updated fields + re-computed result
  └─ DELETE /api/trades/{{tradeId}}  → 204
  └─ GET    /api/trades              → 200, trade no longer in list

Budgets + Transactions — Spending flow/
  └─ POST   /api/budgets             → 201, capture budgetId
  └─ POST   /api/transactions        → 201, capture transactionId
  └─ GET    /api/transactions?month= → 200, tx present with correct fields
  └─ PATCH  /api/budgets/{{budgetId}}→ 204
  └─ DELETE /api/transactions/{{id}} → 204
  └─ DELETE /api/budgets/{{id}}      → 204

Watched Symbols — Lifecycle/
  └─ POST   /api/watchedsymbols      → 201, capture watchedSymbolId
  └─ POST   /api/watchedsymbols      → 409 (duplicate — conflict guard)
  └─ GET    /api/watchedsymbols      → 200, symbol present
  └─ DELETE /api/watchedsymbols/{{id}}→ 204

Authorization Guards/              ← uses bearerToken2 (second user)
  └─ PUT    /api/trades/{{tradeId}} → 403
  └─ DELETE /api/trades/{{tradeId}} → 403
  └─ DELETE /api/budgets/{{budgetId}}→ 403

Market/                            ← third-party contract verification (CoinGecko)
  └─ GET /api/market/fear-greed     → 200, validates shape when data available (null-safe)
  └─ GET /api/market/trending       → 200, validates item shape when array non-empty
```

> Validation / negative-input tests live in `FinTrackPro.dev.postman_collection.json` under `Validation & Error Cases/`.

### Dev collection structure (`FinTrackPro.dev.postman_collection.json`)

Organized by resource — requests are standalone (no cross-request flow assertions):

```
Auth/                        ← manual token mint example
Trades/                      ← POST, GET list, GET by ID, PUT, DELETE, GET /open
Transactions/                ← POST, GET, GET ?month=, PUT, DELETE
Budgets/                     ← POST, GET, GET ?month=, PATCH, DELETE
Watched Symbols/             ← POST, GET, DELETE
Market/                      ← fear-greed, trending
Validation & Error Cases/    ← negative tests (400s): entryPrice=-1, lowercase symbol, amount=-50
```

### Token minting — Pre-request Script

The collection-level Pre-request Script auto-mints and caches `bearerToken` before the first request:

```javascript
const expiry = pm.environment.get('_tokenExpiry');
if (!expiry || Date.now() > parseInt(expiry)) {
    pm.sendRequest({
        url: pm.environment.get('keycloakUrl')
            + '/realms/fintrackpro/protocol/openid-connect/token',
        method: 'POST',
        header: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: {
            mode: 'urlencoded',
            urlencoded: [
                { key: 'grant_type', value: 'password' },
                { key: 'client_id',  value: 'fintrackpro-e2e' },
                { key: 'username',   value: pm.environment.get('testUsername') },
                { key: 'password',   value: pm.environment.get('testPassword') }
            ]
        }
    }, (err, res) => {
        const body = res.json();
        pm.environment.set('bearerToken', body.access_token);
        pm.environment.set('_tokenExpiry',
            String(Date.now() + (body.expires_in - 30) * 1000));
    });
}
```

---

## Environment variables

| Variable | Committed value | Set by |
|---|---|---|
| `baseUrl` | `http://localhost:5018` | Override in CI / local env if needed |
| `keycloakUrl` | `http://localhost:8080` | Override in CI / local env if needed |
| `bearerToken` | `` | Pre-request Script |
| `bearerToken2` | `` | `api-e2e-local.sh` |
| `testUsername` | `admin@fintrackpro.dev` | Override in CI / local env if needed |
| `testPassword` | `Admin1234!` | Override in CI / local env if needed |
| `testUsername2` | `user2@fintrackpro.dev` | Override in CI / local env if needed |
| `testPassword2` | `User2Pass!` | Override in CI / local env if needed |
| `budgetId` | `` | Captured at runtime |
| `transactionId` | `` | Captured at runtime |
| `tradeId` | `` | Captured at runtime |
| `watchedSymbolId` | `` | Captured at runtime |

---

## Local run

Prerequisites: Docker up (`postgres` + `keycloak` + `api`), Newman installed (`npm install -g newman`).

```bash
# Full suite
bash scripts/api-e2e-local.sh

# Single folder (use exact folder name prefix)
bash scripts/api-e2e-local.sh --folder "Trades — Full lifecycle"

# Debug output
bash scripts/api-e2e-local.sh --verbose

# Override defaults
KEYCLOAK_URL=http://localhost:8080 \
API_BASE_URL=http://localhost:5018 \
E2E_USERNAME=admin@fintrackpro.dev \
E2E_PASSWORD=Admin1234! \
E2E_USERNAME2=user2@fintrackpro.dev \
E2E_PASSWORD2=User2Pass! \
bash scripts/api-e2e-local.sh
```

### First-time setup after `docker compose up`

The Keycloak realm import runs **only on the first container start** (idempotent — skipped if realm already exists). If you started the stack before `user2@fintrackpro.dev` was added to `keycloak-realm.json`, you must wipe the Keycloak data volume and restart so the updated realm is imported:

```bash
# Stop Keycloak and remove its data volume, then restart
docker compose stop keycloak
docker volume rm fintrackpro_keycloak-data
docker compose up -d keycloak

# Wait for Keycloak to be ready (~15–30 s), then verify
curl -sf -X POST http://localhost:8080/realms/fintrackpro/protocol/openid-connect/token \
  -d "grant_type=password&client_id=fintrackpro-e2e&username=user2@fintrackpro.dev&password=User2Pass!" \
  | grep -o '"access_token":"[^"]*"' | head -c 40 && echo " ← user2 OK"
```

> **Note:** Wiping the Keycloak volume also changes the `sub` claim for all existing users. Any `AppUser` rows in the `Users` table will have a mismatched `ExternalUserId`. The `IdentityService` handles this automatically (re-links the identity on next login via the email match + concurrency retry), but if you want a clean slate:
> ```bash
> docker exec fintrackpro-postgres psql -U postgres -d "FinTrackPro" -c 'TRUNCATE "Users" CASCADE;'
> ```

---

## CI job

Runs after `Backend — Build & Test`, in parallel with `Frontend — E2E (Playwright)`:

```
Backend — Build & Test   (unit + integration, WebApplicationFactory, no Keycloak)
        │
        ├──► Backend — API E2E     ← Newman, real Keycloak JWT
        │
        └──► Frontend — E2E        ← Playwright (existing)
```

### GitHub secrets required

| Secret | Value |
|---|---|
| `E2E_USERNAME` | `admin@fintrackpro.dev` |
| `E2E_PASSWORD` | `Admin1234!` (rotate for production) |
| `E2E_USERNAME2` | `user2@fintrackpro.dev` |
| `E2E_PASSWORD2` | `User2Pass!` |

---

## Verification checklist

- [ ] `bash scripts/api-e2e-local.sh` → all tests green locally
- [ ] Push to `develop` → `Backend — API E2E` job appears in GitHub Actions
- [ ] Swap `bearerToken2` with `bearerToken` in Authorization Guards → tests correctly fail (guards are real)
- [ ] `docker volume rm fintrackpro_keycloak-data && docker compose up -d keycloak` → `user2@fintrackpro.dev` present after realm re-import (see First-time setup notes above)
