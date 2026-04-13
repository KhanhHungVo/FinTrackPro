# Plan: Search, Filter, Sort & Pagination for FinTrackPro List Pages

## Context

All three list pages (Transactions, Trades, Budgets) currently load all records in one request
with no search, sorting, or pagination. As data grows this becomes a UX and performance problem.

**Goal:** Add server-side search/filter/sort/pagination to Transactions and Trades; add
client-side filter/sort to Budgets (data is small and bounded by month).

**Agreed UX decisions:**
- Classic numbered pagination `< 1 2 3 >`, default 20 items, configurable (10/20/50)
- Debounce search input 300 ms
- Single-column sort; click cycles: default → desc → asc → reset; arrow indicator on header
- Filter state in-memory (not in URL)
- Budgets: no pagination, no search — over-budget toggle + client-side sort only

---

## Architecture Summary

| Page | Search | Filters | Sort | Pagination | Side |
|---|---|---|---|---|---|
| Transactions | note + category | date range, type, category | date★, amount, category | 20/page | Server |
| Trades | symbol | status, direction, date range | date★, P&L, symbol, entry price, size, fees | 20/page | Server |
| Budgets | — | over-budget toggle | spent %, category name | — | Client |

★ = default sort (descending)

---

## Key Design Decisions

**No default date filter on Trades:** Trades are lifecycle records — a position opened in one
month and closed in another belongs to both. Applying a default month filter would hide open
positions and break continuity. Date range is an optional user-applied filter only. The summary
endpoint therefore aggregates over the full trade history by default (lifetime P&L, lifetime win
rate) unless the user explicitly sets a date range.

**P&L sort on Trades:** `Result` and `UnrealizedResult` are computed properties ignored by EF Core.
Sort must be expressed as an inline LINQ expression translated to SQL `CASE WHEN` — cannot sort
in-memory after `Skip/Take` or results will be wrong.

**BudgetsPage calls `useTransactions(month)`:** After the hook signature change, this call site
becomes `useTransactions({ month, pageSize: 100 })` and reads `data?.items` instead of `data`.
This is a pragmatic limit; a dedicated budget-summary endpoint is the long-term fix.

**Summary cards (Transactions, Trades):** After pagination, KPI cards must not sum only the
current page. A dedicated summary endpoint computes aggregates in a single SQL query server-side
and returns only scalar values — no rows transferred. KPIs update when filters change but are
unaffected by page or sort. This is the same pattern used by TraderVue, Myfxbook, and standard
expense trackers.

**Invalidation:** No changes needed. Prefix-based invalidation `['transactions']` / `['trades']`
already busts all parametrized cache entries.

---

## Phase 1 — Backend

### New files

| File | What |
|---|---|
| `FinTrackPro.Application/Common/Models/PagedResult.cs` | `record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)` with `TotalPages`, `HasPreviousPage`, `HasNextPage` computed properties |
| `FinTrackPro.Domain/Repositories/TransactionPageQuery.cs` | Plain record: `Page, PageSize, Search, Month, Type?, CategoryId?, SortBy, SortDir` |
| `FinTrackPro.Domain/Repositories/TradePageQuery.cs` | Plain record: `Page, PageSize, Search, Status?, Direction?, DateFrom?, DateTo?, SortBy, SortDir` |
| `FinTrackPro.Application/Finance/Queries/GetTransactions/GetTransactionsQueryValidator.cs` | Validates page ≥ 1, pageSize 1-100, sortBy in allowed set, month format yyyy-MM |
| `FinTrackPro.Application/Trading/Queries/GetTrades/GetTradesQueryValidator.cs` | Same pattern; validates dateFrom ≤ dateTo |
| `FinTrackPro.Application/Finance/Queries/GetTransactionSummary/GetTransactionSummaryQuery.cs` | `record GetTransactionSummaryQuery(string? Month, string? Type, Guid? CategoryId) : IRequest<TransactionSummaryDto>` |
| `FinTrackPro.Application/Finance/Queries/GetTransactionSummary/GetTransactionSummaryQueryHandler.cs` | Computes `TotalIncome`, `TotalExpense`, `NetBalance` via a single aggregation query — no rows loaded |
| `FinTrackPro.Application/Trading/Queries/GetTradeSummary/GetTradeSummaryQuery.cs` | `record GetTradeSummaryQuery(string? Status, string? Direction, DateOnly? DateFrom, DateOnly? DateTo) : IRequest<TradeSummaryDto>` |
| `FinTrackPro.Application/Trading/Queries/GetTradeSummary/GetTradeSummaryQueryHandler.cs` | Computes `TotalPnl`, `WinRate`, `TotalTrades`, `UnrealizedPnl` via a single aggregation query — no rows loaded |
| `FinTrackPro.Application/Finance/DTOs/TransactionSummaryDto.cs` | `record TransactionSummaryDto(decimal TotalIncome, decimal TotalExpense, decimal NetBalance)` |
| `FinTrackPro.Application/Trading/DTOs/TradeSummaryDto.cs` | `record TradeSummaryDto(decimal TotalPnl, int WinRate, int TotalTrades, decimal UnrealizedPnl)` |

### Modified files

**`ITransactionRepository.cs`** — add:
```csharp
Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetPagedAsync(
    Guid userId, TransactionPageQuery query, CancellationToken ct = default);
```
Keep existing `GetByUserAsync` (still used by `BudgetOverrunJob`).

**`ITradeRepository.cs`** — add:
```csharp
Task<(IReadOnlyList<Trade> Items, int TotalCount)> GetPagedAsync(
    Guid userId, TradePageQuery query, CancellationToken ct = default);
```
Keep existing `GetByUserAsync`.

**`TransactionRepository.cs`** — implement `GetPagedAsync`:
- Build `IQueryable<Transaction>` starting from `Where(t => t.UserId == userId)`
- Apply optional filters: `BudgetMonth`, `Type`, `CategoryId`, `Search` (`.ToLower().Contains(term)` on `Note` and `Category`)
- Call `CountAsync` on filtered query (before pagination)
- Apply `OrderBy` switch expression on `(SortBy, SortDir)`
- Apply `Skip((page-1)*pageSize).Take(pageSize).ToListAsync`

**`TradeRepository.cs`** — implement `GetPagedAsync`:
- Same pattern for filters: `Symbol.ToLower().Contains`, `Status`, `Direction`, `CreatedAt` range
- P&L inline sort arm (EF Core translatable LINQ):
  ```csharp
  ("pnl", _) => query.OrderByDescending(t =>
      t.Status == TradeStatus.Closed && t.ExitPrice != null
          ? (t.Direction == TradeDirection.Long
              ? (t.ExitPrice.Value - t.EntryPrice) * t.PositionSize - t.Fees
              : (t.EntryPrice - t.ExitPrice.Value) * t.PositionSize - t.Fees)
          : 0m)
  ```
  Verify SQL translation with `LogTo(Console.WriteLine)` during implementation.

**`GetTransactionsQuery.cs`** — replace `(string? Month)` with:
```csharp
public record GetTransactionsQuery(
    int Page = 1, int PageSize = 20, string? Search = null,
    string? Month = null, string? Type = null, Guid? CategoryId = null,
    string SortBy = "date", string SortDir = "desc"
) : IRequest<PagedResult<TransactionDto>>;
```

**`GetTransactionsQueryHandler.cs`** — change return type to `PagedResult<TransactionDto>`:
- Preserve subscription enforcement block (unchanged — month parsing + `EnforceTransactionHistoryAccessAsync`)
- Replace `GetByUserAsync` call with `GetPagedAsync(user.Id, new TransactionPageQuery(...))`
- Clamp `PageSize` to max 100 in handler
- Return `new PagedResult<TransactionDto>(items.Select(...), page, pageSize, totalCount)`

**`GetTradesQuery.cs`** — replace empty record with full parameter set, return `PagedResult<TradeDto>`.

**`GetTradesQueryHandler.cs`** — same pattern as transactions handler.

**`TransactionsController.cs`** — change `GetAll` signature:
```csharp
[HttpGet]
public async Task<ActionResult<PagedResult<TransactionDto>>> GetAll(
    [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
    [FromQuery] string? search = null, [FromQuery] string? month = null,
    [FromQuery] string? type = null, [FromQuery] Guid? categoryId = null,
    [FromQuery] string sortBy = "date", [FromQuery] string sortDir = "desc")
```

**`TradesController.cs`** — same pattern with trades params.

**`TransactionsController.cs`** — add summary endpoint:
```csharp
[HttpGet("summary")]
public async Task<ActionResult<TransactionSummaryDto>> GetSummary(
    [FromQuery] string? month, [FromQuery] string? type, [FromQuery] Guid? categoryId)
```
Accepts the same filter params as `GetAll` (minus page/sort) so the frontend can pass the active
filter state and always get correct aggregates over the full filtered dataset.

**`TradesController.cs`** — add summary endpoint:
```csharp
[HttpGet("summary")]
public async Task<ActionResult<TradeSummaryDto>> GetSummary(
    [FromQuery] string? status, [FromQuery] string? direction,
    [FromQuery] DateOnly? dateFrom, [FromQuery] DateOnly? dateTo)
```

---

## Phase 2 — Frontend Infrastructure (`shared/`)

### New files

| File | What |
|---|---|
| `src/shared/api/types.ts` | `PagedResult<T>` TypeScript interface matching backend response |
| `src/shared/lib/cleanParams.ts` | Strips `undefined` and `''` values from params object before Axios call |
| `src/shared/lib/useDebounce.ts` | `function useDebounce<T>(value: T, delayMs: number): T` |
| `src/shared/ui/Pagination.tsx` | Props: `page, totalPages, pageSize, onPageChange, onPageSizeChange, pageSizeOptions?, disabled?`. Renders `< 1 2 … N >` with ellipsis at > 7 pages. Page size dropdown. |
| `src/shared/ui/SortableColumnHeader.tsx` | Props: `label, field, currentSortBy, currentSortDir, onSort, align?`. Click cycles: null → desc → asc → null. Shows ↑ ↓ ↕ indicator. |

Export both from `src/shared/ui/index.ts`.

---

## Phase 3 — Frontend Entity Layer

**`src/entities/transaction/api/transactionApi.ts`**
- Add `TransactionQueryParams` interface (page, pageSize, search, month, type, categoryId, sortBy, sortDir)
- Add `TransactionSummaryParams` interface (month, type, categoryId — filters only, no page/sort)
- Replace `useTransactions(month?: string)` with `useTransactions(params: TransactionQueryParams = {})`
- Add `useTransactionSummary(params: TransactionSummaryParams = {})` → `GET /api/transactions/summary`
- Query keys: `['transactions', params]` / `['transactions', 'summary', params]`
- Return types: `PagedResult<Transaction>` / `TransactionSummary`

**`src/entities/trade/api/tradeApi.ts`**
- Add `TradeQueryParams` interface (page, pageSize, search, status, direction, dateFrom, dateTo, sortBy, sortDir)
- Add `TradeSummaryParams` interface (status, direction, dateFrom, dateTo — filters only, no page/sort)
- Replace `useTrades()` with `useTrades(params: TradeQueryParams = {})`
- Add `useTradesSummary(params: TradeSummaryParams = {})` → `GET /api/trades/summary`
- Query keys: `['trades', params]` / `['trades', 'summary', params]`
- Return types: `PagedResult<Trade>` / `TradesSummary`

Export new param interfaces and summary hooks from `index.ts` files.

---

## Phase 4 — Feature Filter Bars

**`src/features/filter-transactions/ui/TransactionFilterBar.tsx`**
- Props: `value: TransactionFilters, onChange, categories`
- Contains: search input (uncontrolled debounce via `useDebounce`), month select, type toggle (All/Income/Expense), category dropdown
- Export from `src/features/filter-transactions/index.ts`

**`src/features/filter-trades/ui/TradeFilterBar.tsx`**
- Props: `value: TradeFilters, onChange`
- Contains: search input, status toggle, direction toggle, date range (from/to)
- Export from `src/features/filter-trades/index.ts`

---

## Phase 5 — Page Wiring

### `TransactionsPage.tsx`
- Replace `[month, setMonth]` with `filters` object state + `page/pageSize/sortBy/sortDir` state
- `useEffect` resets page to 1 when any filter/sort changes
- **Two independent queries:**
  - Summary: `useTransactionSummary({ month: filters.month, type: filters.type, categoryId: filters.categoryId })` → feeds KPI cards; updates when filters change, unaffected by page/sort
  - Table: `useTransactions({ ...filters, search: debouncedSearch, page, pageSize, sortBy, sortDir })` → feeds list
- Replace raw `transactions?.map(...)` with `data?.items.map(...)`
- Add `<TransactionFilterBar>` above table
- Add `<SortableColumnHeader>` to Date, Amount, Category columns
- Add `<Pagination>` below table

### `TradesPage.tsx`
- Same pattern as Transactions
- **Two independent queries:**
  - Summary: `useTradesSummary({ status: filters.status, direction: filters.direction, dateFrom: filters.dateFrom, dateTo: filters.dateTo })` → feeds KPI cards; updates when filters change, unaffected by page/sort
  - Table: `useTrades({ ...filters, search: debouncedSearch, page, pageSize, sortBy, sortDir })` → feeds list
- KPI cards (`totalPnl`, `winRate`, `totalTrades`, `unrealizedPnl`) sourced from summary response, not derived from the table rows
- Remove existing client-side `filter()` for Open/Closed status (now server-side)
- Add `<TradeFilterBar>` above table
- Add `<SortableColumnHeader>` to: Date, P&L, Symbol, Entry Price, Position Size, Fees
- Add `<Pagination>` below table

### `BudgetsPage.tsx`
- Change `useTransactions(month)` → `useTransactions({ month, pageSize: 100 })`
- Read `data?.items` instead of `data` for `spentByCategory` computation
- Add `[showOverBudgetOnly, setShowOverBudgetOnly]` state → toggle button
- Add `[budgetSort, budgetSortDir]` state
- Derive `displayBudgets` with `useMemo`: filter over-budget, then sort by `spentPct` or `category`
- Add `<SortableColumnHeader>`-style clickable labels above the list (client-side, inline)

---

## Phase 6 — Tests

### General rule
For every file touched in Phases 1–5, check existing tests first. Update or fix any test that
breaks due to signature changes (return type, query params, hook interface) before adding new
cases. No existing passing test should be left failing after this phase.

### Backend unit tests — fix existing
- `GetTransactionsHandlerTests.cs` — update all existing cases: return type is now `PagedResult<TransactionDto>`, access items via `.Items`; query constructor now requires full param set
- `GetTradesHandlerTests.cs` — same: update return type assertions and query construction
- Any handler test that calls `GetByUserAsync` directly — verify it still compiles after the repository interface gains `GetPagedAsync`

### Backend unit tests — new cases
- `GetTransactionsHandlerTests.cs` — add: paginated response shape, search filter, type filter, sort param passed through, summary query returns correct aggregates
- `GetTradesHandlerTests.cs` — add: same pattern + status/direction filter, summary query aggregates
- New `TransactionRepositoryTests.cs` in Infrastructure.UnitTests — test `GetPagedAsync` with EF Core in-memory: search, filter, pagination boundaries, sort
- New `TradeRepositoryTests.cs` — include P&L sort case (confirm SQL expression does not throw)

### Backend integration tests — fix existing
- `TransactionsTests.cs` — existing assertions that read response as `Transaction[]` must be updated to read `PagedResult<TransactionDto>` and access `.items`
- `TradesTests.cs` — same: response body is now `{ items, page, pageSize, totalCount }` not a flat array
- Any test that checks response array length directly (e.g. `body.GetArrayLength()`) — update to check `items` length inside the paged wrapper

### Backend integration tests — new cases
- `TransactionsTests.cs` — add: `GET /api/transactions?page=1&pageSize=5` returns wrapped response, search returns filtered results, invalid sortBy returns 400, `GET /api/transactions/summary` returns correct aggregates
- `TradesTests.cs` — same pattern; add status filter, P&L sort, and `GET /api/trades/summary` tests

### Frontend — fix existing
- `transactionApi.test.ts` — update mock return value from `Transaction[]` to `PagedResult<Transaction>`; update any assertion on `result.current.data` to access `.items`
- `tradeApi.test.ts` — same
- `TransactionsPage.test.tsx` / `TradesPage.test.tsx` — update mocks to return paged shape; add mock for summary hook (`useTransactionSummary`, `useTradesSummary`)
- E2E `transactions.spec.ts` / `trades.spec.ts` — update any assertion that relied on all records being present on first load (now only page 1 is returned)

### Frontend — new tests
- `Pagination.test.tsx` — renders correct pages, ellipsis logic, calls `onPageChange`
- `SortableColumnHeader.test.tsx` — click cycles through desc → asc → null
- E2E: add assertions for search, sort, pagination, and KPI accuracy across page changes to `transactions.spec.ts` and `trades.spec.ts`

---

## Critical Files (quick reference)

**Backend — modify:**
- `backend/src/FinTrackPro.Domain/Repositories/ITransactionRepository.cs`
- `backend/src/FinTrackPro.Domain/Repositories/ITradeRepository.cs`
- `backend/src/FinTrackPro.Infrastructure/Persistence/Repositories/TransactionRepository.cs`
- `backend/src/FinTrackPro.Infrastructure/Persistence/Repositories/TradeRepository.cs`
- `backend/src/FinTrackPro.Application/Finance/Queries/GetTransactions/GetTransactionsQuery.cs`
- `backend/src/FinTrackPro.Application/Finance/Queries/GetTransactions/GetTransactionsQueryHandler.cs`
- `backend/src/FinTrackPro.Application/Trading/Queries/GetTrades/GetTradesQuery.cs`
- `backend/src/FinTrackPro.Application/Trading/Queries/GetTrades/GetTradesQueryHandler.cs`
- `backend/src/FinTrackPro.API/Controllers/TransactionsController.cs`
- `backend/src/FinTrackPro.API/Controllers/TradesController.cs`

**Backend — new:**
- `backend/src/FinTrackPro.Application/Common/Models/PagedResult.cs`
- `backend/src/FinTrackPro.Domain/Repositories/TransactionPageQuery.cs`
- `backend/src/FinTrackPro.Domain/Repositories/TradePageQuery.cs`
- `backend/src/FinTrackPro.Application/Finance/Queries/GetTransactions/GetTransactionsQueryValidator.cs`
- `backend/src/FinTrackPro.Application/Trading/Queries/GetTrades/GetTradesQueryValidator.cs`
- `backend/src/FinTrackPro.Application/Finance/Queries/GetTransactionSummary/GetTransactionSummaryQuery.cs`
- `backend/src/FinTrackPro.Application/Finance/Queries/GetTransactionSummary/GetTransactionSummaryQueryHandler.cs`
- `backend/src/FinTrackPro.Application/Finance/DTOs/TransactionSummaryDto.cs`
- `backend/src/FinTrackPro.Application/Trading/Queries/GetTradeSummary/GetTradeSummaryQuery.cs`
- `backend/src/FinTrackPro.Application/Trading/Queries/GetTradeSummary/GetTradeSummaryQueryHandler.cs`
- `backend/src/FinTrackPro.Application/Trading/DTOs/TradeSummaryDto.cs`

**Frontend — modify:**
- `frontend/fintrackpro-ui/src/entities/transaction/api/transactionApi.ts`
- `frontend/fintrackpro-ui/src/entities/trade/api/tradeApi.ts`
- `frontend/fintrackpro-ui/src/pages/transactions/ui/TransactionsPage.tsx`
- `frontend/fintrackpro-ui/src/pages/trades/ui/TradesPage.tsx`
- `frontend/fintrackpro-ui/src/pages/budgets/ui/BudgetsPage.tsx`
- `frontend/fintrackpro-ui/src/shared/ui/index.ts`

**Frontend — new:**
- `frontend/fintrackpro-ui/src/shared/api/types.ts`
- `frontend/fintrackpro-ui/src/shared/lib/cleanParams.ts`
- `frontend/fintrackpro-ui/src/shared/lib/useDebounce.ts`
- `frontend/fintrackpro-ui/src/shared/ui/Pagination.tsx`
- `frontend/fintrackpro-ui/src/shared/ui/SortableColumnHeader.tsx`
- `frontend/fintrackpro-ui/src/features/filter-transactions/ui/TransactionFilterBar.tsx`
- `frontend/fintrackpro-ui/src/features/filter-transactions/index.ts`
- `frontend/fintrackpro-ui/src/features/filter-trades/ui/TradeFilterBar.tsx`
- `frontend/fintrackpro-ui/src/features/filter-trades/index.ts`

---

## Verification

```bash
# Backend — smoke test after Phase 1
GET http://localhost:5018/api/transactions?page=1&pageSize=5&sortBy=amount&sortDir=desc
GET http://localhost:5018/api/transactions?search=rent&month=2026-04
GET http://localhost:5018/api/trades?status=Open&sortBy=pnl&sortDir=desc
GET http://localhost:5018/api/transactions?page=1&sortBy=invalid  # → 400

# Summary endpoints — KPIs always over full filtered dataset, no rows returned
GET http://localhost:5018/api/transactions/summary?month=2026-04
GET http://localhost:5018/api/transactions/summary?month=2026-04&type=Expense
GET http://localhost:5018/api/trades/summary
GET http://localhost:5018/api/trades/summary?status=Closed&dateFrom=2026-01-01&dateTo=2026-03-31

# Backend tests
cd backend
dotnet test --filter "Category!=Integration"   # unit tests
dotnet test --filter "Category=Integration"    # integration tests

# Frontend — compile check catches all broken call sites
cd frontend/fintrackpro-ui
npm run build

# Frontend E2E
bash scripts/e2e-local.sh tests/e2e/transactions.spec.ts
bash scripts/e2e-local.sh tests/e2e/trades.spec.ts
```

---

## Postman Collection Updates (apply after Phase 1 backend is done)

### E2E collection (`FinTrackPro.e2e.postman_collection.json`) — fix existing

The following requests assert on the response as a flat array and will break once the response
shape changes to `PagedResult<T>`:

| Request | Current assertion | Fix |
|---|---|---|
| #4 List trades — trade present | `trades` is array; `tradeId` in list | Read `body.items`; check `body.items.some(t => t.id === tradeId)` |
| #7 List trades — trade absent | `tradeId` not in list | Read `body.items`; check `body.items.every(t => t.id !== tradeId)` |
| #11 List transactions for month | `txs` is array; `transactionId` in list | Read `body.items`; check `body.items.some(t => t.id === transactionId)` |

Also update `api-e2e-test-cases.md` totals and key-assertions column for requests #4, #7, #11
to reflect the paged response shape.

### E2E collection — new requests (add to existing folders)

**`Trades — Full lifecycle` folder** — add after existing list requests:
- `GET /api/trades/summary` → 200; assert `totalPnl`, `winRate`, `totalTrades`, `unrealizedPnl` present; `winRate` in [0, 100]

**`Budgets + Transactions — Spending flow` folder** — add after transaction list request:
- `GET /api/transactions/summary?month={{testMonth}}` → 200; assert `totalIncome`, `totalExpense`, `netBalance` present; values are numbers

### Dev collection (`FinTrackPro.dev.postman_collection.json`) — fix existing

| Request | Fix |
|---|---|
| `GET /api/trades → 200` | Assert `body.items` array (not `body` array); assert `page`, `pageSize`, `totalCount` present |
| `GET /api/transactions → 200` | Same — assert paged shape |
| `GET /api/transactions?month={{testMonth}} → 200` | Same — assert paged shape, check `body.items[0].budgetMonth` |

### Dev collection — new requests to add

**Trades folder:**
- `GET /api/trades?page=1&pageSize=5&sortBy=pnl&sortDir=desc → 200` — assert `page=1`, `pageSize=5`, `items.length ≤ 5`
- `GET /api/trades?status=Open → 200` — assert all `items[*].status === 'Open'`
- `GET /api/trades?search=BTC → 200` — assert paged shape returned
- `GET /api/trades/summary → 200` — assert `totalPnl`, `winRate`, `totalTrades`, `unrealizedPnl`; `winRate` in [0,100]
- `GET /api/trades/summary?status=Closed → 200` — assert same fields; filtered to closed trades

**Transactions folder:**
- `GET /api/transactions?page=1&pageSize=5&sortBy=amount&sortDir=desc → 200` — assert `page=1`, `pageSize=5`, `items.length ≤ 5`
- `GET /api/transactions?search=rent → 200` — assert paged shape returned
- `GET /api/transactions/summary?month={{testMonth}} → 200` — assert `totalIncome`, `totalExpense`, `netBalance`; all are numbers
- `GET /api/transactions/summary → 200` — assert same fields; no month filter (lifetime totals)

**Validation & Error Cases folder:**
- `GET /api/transactions?sortBy=invalid → 400` — assert status 400 (FluentValidation rejects unknown sort column)
- `GET /api/trades?sortBy=invalid → 400` — same
- `GET /api/trades?page=0 → 400` — assert status 400 (page must be ≥ 1)
- `GET /api/transactions?pageSize=200 → 400` — assert status 400 (pageSize max 100)

---

## Documentation Sync (apply after all phases are done)

### `docs/architecture/api-spec.md`
- `GET /api/transactions` — update query params table (add `page`, `pageSize`, `search`, `type`, `categoryId`, `sortBy`, `sortDir`); replace flat array response with `PagedResult<T>` shape; add 400 validation errors row
- `GET /api/trades` — same: update query params, replace flat array response with `PagedResult<T>` shape
- Add `GET /api/transactions/summary` section — query params (`month`, `type`, `categoryId`), response shape (`totalIncome`, `totalExpense`, `netBalance`)
- Add `GET /api/trades/summary` section — query params (`status`, `direction`, `dateFrom`, `dateTo`), response shape (`totalPnl`, `winRate`, `totalTrades`, `unrealizedPnl`)

### `docs/architecture/ui-flows.md`
- **Transactions screen** — add filter bar row to layout diagram (search input, month selector, type toggle, category dropdown); add sortable column headers row; add pagination row below list; update summary cards note (sourced from `/summary` endpoint, not the page)
- **Trades screen** — add filter bar row (search, status toggle, direction toggle, date range); add sortable column headers to table (Date, P&L, Symbol, Entry Price, Position Size, Fees); add pagination row; update KPI cards note (sourced from `/summary` endpoint — lifetime totals, unaffected by page/sort)
- **Budgets screen** — add over-budget toggle and client-side sort to User Actions table

### `docs/features.md`
- **Transaction Tracking** — add bullet: search by note/category, filter by type, sort by date/amount/category, paginated list (20/page)
- **Trading Journal** — add bullet: search by symbol, filter by status/direction/date range, sort by date/P&L/symbol/entry price/size/fees, paginated list (20/page); update "journal statistics" bullet to clarify KPI cards reflect lifetime totals (or filtered totals when date range is set), not just the current page

### `docs/postman/api-e2e-test-cases.md`
- Update totals count (new requests added)
- Update key-assertions column for requests #4, #7, #11 to reflect paged shape (`body.items` array)

---

## Known Limitations (post-v1 follow-up)

- BudgetsPage spending totals capped at 100 transactions/month → needs dedicated `/api/budgets/{month}/summary` endpoint
- No URL persistence of filter state (explicitly out of scope for v1)
