# API E2E Test Cases — Quick Reference

> This document is a test inventory. For strategy, CI setup, and Keycloak first-time
> configuration see [`api-e2e-plan.md`](api-e2e-plan.md).
>
> Collection file: `FinTrackPro.e2e.postman_collection.json`
> Run command: `bash scripts/api-e2e-local.sh`
> Current totals: **33 requests · 52 assertions**

---

## Auth (2 requests)

| # | Test name | Method | Endpoint | Status | Key assertions |
|---|---|---|---|---|---|
| 1 | No token → 401 | GET | `/api/transactions` | 401 | Status 401 |
| 2 | Mint bearerToken | POST | `{{keycloakUrl}}/realms/fintrackpro/protocol/openid-connect/token` | 200 | `access_token` present; stored as `bearerToken` |

---

## Trades — Full lifecycle (7 requests)

| # | Test name | Method | Endpoint | Status | Key assertions |
|---|---|---|---|---|---|
| 3 | Create trade | POST | `/api/trades` | 201 | Response is a GUID; captured as `tradeId` |
| 4 | List trades — trade present | GET | `/api/trades` | 200 | `body.items` is array; `body.page`, `body.pageSize`, `body.totalCount` present; `tradeId` in `body.items`; `id`, `symbol`, `direction`, `result` fields present |
| 5 | Update trade (exitPrice → 67 000) | PUT | `/api/trades/{{tradeId}}` | 200 | `exitPrice` = 67 000; notes contains "Updated target hit"; `result` ≈ 695 (±1) |
| 6 | Delete trade | DELETE | `/api/trades/{{tradeId}}` | 204 | — |
| 7 | List trades — trade absent | GET | `/api/trades` | 200 | `body.items` is array; `tradeId` not in `body.items` |
| 7a | Trades summary | GET | `/api/trades/summary` | 200 | `totalPnl`, `winRate`, `totalTrades`, `unrealizedPnl` present; `winRate` in [0, 100] |

---

## Budgets + Transactions — Spending flow (9 requests)

| # | Test name | Method | Endpoint | Status | Key assertions |
|---|---|---|---|---|---|
| 8 | Create budget (Food, `{{testMonth}}`) | POST | `/api/budgets` | 201 | GUID returned; captured as `budgetId`; pre-request deletes any stale Food budget for month |
| 9 | Fetch Expense categories | GET | `/api/transaction-categories?type=Expense` | 200 | `food_beverage` slug present; `id` captured as `foodCategoryId` |
| 10 | Create transaction ($120.50) | POST | `/api/transactions` | 201 | GUID returned; captured as `transactionId` |
| 11 | List transactions for month | GET | `/api/transactions?month={{testMonth}}` | 200 | `body.items` is array; `body.page`, `body.pageSize`, `body.totalCount` present; `transactionId` in `body.items`; `amount` ≈ 120.50 (±0.01); `category` is non-empty string; `categoryId` matches `foodCategoryId`; `budgetMonth` present |
| 11a | Transactions summary for month | GET | `/api/transactions/summary?month={{testMonth}}` | 200 | `totalIncome`, `totalExpense`, `netBalance` present; all are numbers |
| 12 | Update budget limit (→ $750) | PATCH | `/api/budgets/{{budgetId}}` | 204 | — |
| 13 | Delete transaction | DELETE | `/api/transactions/{{transactionId}}` | 204 | — |
| 14 | Delete budget | DELETE | `/api/budgets/{{budgetId}}` | 204 | — |

---

## Watched Symbols — Lifecycle (4 requests)

| # | Test name | Method | Endpoint | Status | Key assertions |
|---|---|---|---|---|---|
| 15 | Add watched symbol (BNBUSDT) | POST | `/api/watchedsymbols` | 201 | GUID returned; captured as `watchedSymbolId` |
| 16 | Duplicate symbol → conflict | POST | `/api/watchedsymbols` | 409 | Status 409 |
| 17 | List watched symbols | GET | `/api/watchedsymbols` | 200 | `watchedSymbolId` in list; `id`, `symbol`, `createdAt` present |
| 18 | Delete watched symbol | DELETE | `/api/watchedsymbols/{{watchedSymbolId}}` | 204 | — |

---

## Authorization Guards (9 requests)

All guard 403 tests use `bearerToken2` (user2 — User role only). Seed resources are owned by user1.

| # | Test name | Method | Endpoint | Status | Key assertions |
|---|---|---|---|---|---|
| 19 | Refresh user1 token | POST | `{{keycloakUrl}}/…/token` | 200 | `access_token` present; `bearerToken` refreshed |
| 20 | Mint user2 token | POST | `{{keycloakUrl}}/…/token` | 200 | `access_token` present; stored as `bearerToken2` |
| 21 | Seed guard trade (user1) | POST | `/api/trades` | 201 | GUID captured as `guardTradeId` |
| 22 | Seed guard budget (user1, Entertainment, `{{testMonth}}`) | POST | `/api/budgets` | 201 | GUID captured as `guardBudgetId` |
| 23 | Guard: PUT trade (user2) | PUT | `/api/trades/{{guardTradeId}}` | 403 | Status 403 |
| 24 | Guard: DELETE trade (user2) | DELETE | `/api/trades/{{guardTradeId}}` | 403 | Status 403 |
| 25 | Guard: DELETE budget (user2) | DELETE | `/api/budgets/{{guardBudgetId}}` | 403 | Status 403 |
| 26 | Cleanup: DELETE guard trade (user1) | DELETE | `/api/trades/{{guardTradeId}}` | 204 | — |
| 27 | Cleanup: DELETE guard budget (user1) | DELETE | `/api/budgets/{{guardBudgetId}}` | 204 | — |

---

## Market (2 requests)

| # | Test name | Method | Endpoint | Status | Key assertions |
|---|---|---|---|---|---|
| 28 | Fear & Greed index | GET | `/api/market/fear-greed` | 200 | When data available: `value` in [0, 100]; `label` and `timestamp` present (null-safe) |
| 29 | Trending coins | GET | `/api/market/trending` | 200 | Array; when non-empty: `id`, `name`, `symbol`, `marketCapRank` present |

---

## Coverage Matrix

| Resource | Create | Read list | Summary | Update | Delete | Conflict guard | Ownership guard |
|---|---|---|---|---|---|---|---|
| Trades | 201 (#3) | 200 (#4, #7) — paged | 200 (#7a) | 200 (#5) | 204 (#6) | — | PUT 403 (#23), DELETE 403 (#24) |
| Budgets | 201 (#8, #22) | via pre-req cleanup | — | PATCH 204 (#12) | 204 (#14, #26, #27) | — | DELETE 403 (#25) |
| Transactions | 201 (#10) | 200 (#11) — paged | 200 (#11a) | — | 204 (#13) | — | — |
| Watched Symbols | 201 (#15) | 200 (#17) | — | — | 204 (#18) | 409 (#16) | — |
| Transaction Categories | — | 200 (#9) | — | — | — | — | — |
| Market / Fear-Greed | — | 200 (#28) | — | — | — | — | — |
| Market / Trending | — | 200 (#29) | — | — | — | — | — |

---

## Environment Variables

| Variable | Source | Notes |
|---|---|---|
| `baseUrl` | Environment file / `--env-var` | Default: `http://localhost:5018` |
| `keycloakUrl` | Environment file / `--env-var` | Default: `http://localhost:8080` |
| `testUsername` | Environment file / CI secret `E2E_USERNAME` | Primary user (Admin + User roles) |
| `testPassword` | Environment file / CI secret `E2E_PASSWORD` | Primary user password |
| `testUsername2` | Environment file / CI secret `E2E_USERNAME2` | Second user (User role only) — for guard tests |
| `testPassword2` | Environment file / CI secret `E2E_PASSWORD2` | Second user password |
| `bearerToken` | Minted in #2; refreshed in #19 | JWT for user1; used by collection-level bearer auth |
| `bearerToken2` | Minted in #20 | JWT for user2; used explicitly in requests #23–25 |
| `testMonth` | Reset at run start by collection pre-request | Always current YYYY-MM; never stale between runs |
| `tradeId` | Captured in #3 | Used through #7 |
| `budgetId` | Captured in #8 | Used through #14 |
| `transactionId` | Captured in #10 | Used in #11, #13 |
| `foodCategoryId` | Captured in #9 | Used in #10 request body and verified in #11 |
| `watchedSymbolId` | Captured in #15 | Used in #17, #18 |
| `guardTradeId` | Captured in #21 | Used in #23, #24, #26 |
| `guardBudgetId` | Captured in #22 | Used in #25, #27 |
| `_tokenExpiry` | Set in #2 | Internal cache marker; not used directly by requests |

---

## Known Gaps and Out-of-Scope Items

### Not covered in E2E (by design)

- **Validation / negative input tests (400s):** Intentionally in `FinTrackPro.dev.postman_collection.json` under `Validation & Error Cases`. The two-collection strategy keeps CI-gating tests strictly happy-path; see `api-e2e-plan.md` for rationale.

- **DELETE /api/transactions ownership guard (user2 → 403):** Adding this guard would require seeding a separate transaction inside the Authorization Guards folder, which in turn needs a category ID fetch and a budget — too heavy for a targeted guard test. The three existing 403 guards (PUT trade, DELETE trade, DELETE budget) adequately validate the ownership middleware.

- **GET /api/budgets/{month} as an explicit test step:** Currently exercised only inside the pre-request cleanup script of request #8 (GET + conditional DELETE). The dev collection covers it as a standalone test.

- **GET /api/trades by ID / GET /api/transactions by ID:** E2E uses the list endpoints only. Dev collection covers single-resource reads.

- **Transaction PUT (full update):** PATCH on budgets is covered (#12); full PUT on transactions is in the dev collection only.

- **Open trades endpoint** (`GET /api/trades?status=Open`): Dev collection only.

### Notes

- The `testMonth` variable is always reset at run start (collection pre-request). A stale value in a committed environment file or `--env-export` output will be overwritten every run.
- The guard seed budget (#22) uses `{{testMonth}}` with category `Entertainment`, which cannot conflict with the spending-flow budget (category `Food`) even when both run in the same month.
- Market tests (#28, #29) hit live CoinGecko-backed endpoints. Assertions are null-safe — a null fear-greed response or empty trending array passes. Flakiness in these tests indicates a third-party availability issue, not a regression in FinTrackPro.
