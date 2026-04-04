# UI E2E Test Cases

Playwright suite targeting critical user flows. Tests run sequentially against a live
stack (Vite dev server + .NET API + Keycloak + PostgreSQL).

## How to run

Run via `bash scripts/e2e-local.sh` from the repo root (see [docs/guides/dev-setup.md — Mode E](../../../docs/guides/dev-setup.md)).

## Auth strategy

All specs call `injectAuthToken(page)` in `beforeEach`. This bypasses the Keycloak
login flow by injecting a pre-issued JWT into `localStorage` and setting the
`e2e_bypass` flag so `AuthProvider` skips SDK initialisation. The token is minted
from Keycloak via the ROPC flow in `scripts/e2e-local.sh`.

---

## Test cases

### Budgets — `budgets.spec.ts`

| # | Test | Selectors | Asserts |
|---|------|-----------|---------|
| B1 | **create budget** | `select[1]` (category) → `🍜 Food & Beverage`; placeholder `500` (limit) | Slug `food_beverage` and `/ $300.00` visible in budget list |
| B2 | **delete budget** | `select[1]` (category) → `🏠 Rent`; limit `$999`; `aria-label="Delete"` button | Row count decreases by 1 after delete |

**Selector notes:**
- Selects on `/budgets`: `[0]` = month (page header), `[1]` = category (form), `[2]` = currency (form)
- `BudgetsPage` renders `budget.category` as the raw slug (e.g. `food_beverage`), not the display label
- Delete `×` button uses `aria-label="Delete"` → selectable via `getByRole('button', { name: 'Delete' })`

**Not tested here (unit / API E2E coverage):**
- Inline limit edit (requires blur/Enter interaction; covered by API E2E)
- Budget overrun progress bar colour change
- Month selector navigation

---

### Transactions — `transactions.spec.ts`

| # | Test | Selectors | Asserts |
|---|------|-----------|---------|
| T1 | **create expense transaction** | `input[type=number]` (amount); `select[2]` (category) → `🍜 Food & Beverage` | Row with `🍜 Food & Beverage` and `-$85.50` visible |
| T2 | **create income transaction** | Income toggle button; `input[type=number]`; `select[2]` → `💰 Salary` | Row with `💰 Salary` and `+$1,000.00` visible |
| T3 | **delete transaction** | `input[type=number]`; `select[2]` → `🛍️ Shopping`; `Note` textarea (`e2e-delete`); `title="Delete"` button | Row count for `e2e-delete` rows decreases by 1 |

**Selector notes:**
- Selects on `/transactions`: `[0]` = month (page header), `[1]` = currency (form), `[2]` = category (form)
- `TransactionsPage` renders `resolveCategoryLabel(slug)` → `${icon} ${labelEn}` (e.g. `🍜 Food & Beverage`)
- Income/Expense toggle resets `categoryId` and reloads the category select with the matching type's options
- Delete `✕` button uses `title="Delete"` (no `aria-label`; text content is `✕`) → use `getByTitle('Delete')`
- Note field used as a stable unique identifier across repeated test runs

**Not tested here:**
- Currency conversion display
- Monthly filter (summary cards update)
- Validation: empty amount / no category selected

---

### Trades — `trades.spec.ts`

| # | Test | Selectors | Asserts |
|---|------|-----------|---------|
| TR1 | **log closed trade and verify PnL** | `Add Trade` toggle button; `Closed Trade` type button; symbol/entry/exit/size/fees placeholders | Row with `BTCUSDT` and `+$495.00` visible; PnL = (65000−60000)×0.1−5 |

**Not tested here:**
- Open position logging (unrealised PnL depends on live price fetch)
- Trade delete / close-position flow

---

## System categories used

Expense categories (seeded by `TransactionCategoryDataSeeder`):

| Slug | Display label | Used in |
|------|--------------|---------|
| `food_beverage` | 🍜 Food & Beverage | B1, T1 |
| `rent` | 🏠 Rent | B2 |
| `shopping` | 🛍️ Shopping | T3 |

Income categories:

| Slug | Display label | Used in |
|------|--------------|---------|
| `salary` | 💰 Salary | T2 |
