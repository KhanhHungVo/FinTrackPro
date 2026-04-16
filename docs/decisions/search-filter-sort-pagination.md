# Search, Filter, Sort & Pagination

## Context

All three list pages (Transactions, Trades, Budgets) previously loaded every record in a single
request with no search, sorting, or pagination. As data grows this degrades both UX and database
performance. This document describes the server-side search/filter/sort/pagination added to
Transactions and Trades, and the client-side filter/sort added to Budgets.

---

## 1. Key Decisions

### Server-side vs client-side split

| Page | Side | Reason |
|---|---|---|
| Transactions | Server | Unbounded history; growing dataset |
| Trades | Server | Unbounded history; P&L sort must be expressed as SQL |
| Budgets | Client | Always scoped to one calendar month; small and bounded |

### No default date filter on Trades

Trades are lifecycle records — a position opened in one month and closed in another belongs to
both. A default month filter would hide open positions and break continuity. Date range is an
optional user-applied filter only. The summary endpoint aggregates over full trade history by
default (lifetime P&L, lifetime win rate) unless the user explicitly sets a date range.

### Dedicated summary endpoints for KPI cards

After pagination, KPI cards must not sum only the current page. A dedicated summary endpoint
computes aggregates in a single SQL query server-side and returns only scalar values — no rows
transferred. KPIs update when filters change but are unaffected by page or sort.

Both `GET /api/transactions/summary` and `GET /api/trades/summary` accept the same filter params
as the list endpoints (minus `page`, `pageSize`, `sortBy`, `sortDir`).

### P&L sort expressed as inline LINQ

`Result` and `UnrealizedResult` are computed properties ignored by EF Core. Sorting on P&L must
be an inline LINQ expression translated to SQL `CASE WHEN` — sorting in-memory after `Skip/Take`
would produce wrong results.

```csharp
("pnl", _) => query.OrderByDescending(t =>
    t.Status == TradeStatus.Closed && t.ExitPrice != null
        ? (t.Direction == TradeDirection.Long
            ? (t.ExitPrice.Value - t.EntryPrice) * t.PositionSize - t.Fees
            : (t.EntryPrice - t.ExitPrice.Value) * t.PositionSize - t.Fees)
        : 0m)
```

### BudgetsPage transaction cap

`BudgetsPage` calls `useTransactions({ month, pageSize: 100 })` and reads `data.items` for the
`spentByCategory` computation. This is a pragmatic cap. A dedicated
`/api/budgets/{month}/summary` endpoint is the correct long-term fix (see Known Limitations).

### Cache invalidation — no changes needed

Prefix-based invalidation `['transactions']` / `['trades']` already busts all parametrised cache
entries when mutations fire.

---

## 2. Architecture Overview

```
Domain:         TransactionPageQuery · TradePageQuery (query param records)
                ITransactionRepository.GetPagedAsync · ITradeRepository.GetPagedAsync
Application:    PagedResult<T> (Common/Models)
                GetTransactionsQuery → PagedResult<TransactionDto>
                GetTradesQuery       → PagedResult<TradeDto>
                GetTransactionSummaryQuery → TransactionSummaryDto
                GetTradeSummaryQuery       → TradeSummaryDto
                FluentValidation: GetTransactionsQueryValidator · GetTradesQueryValidator
Infrastructure: TransactionRepository.GetPagedAsync  (filter → count → sort → skip/take)
                TradeRepository.GetPagedAsync         (same pattern + inline P&L sort arm)
API:            TransactionsController — GET /api/transactions  · GET /api/transactions/summary
                TradesController       — GET /api/trades        · GET /api/trades/summary
Frontend:       shared/api/types.ts          PagedResult<T> TypeScript interface
                shared/lib/cleanParams.ts    strips undefined/'' before Axios call
                shared/lib/useDebounce.ts    300 ms generic debounce hook
                shared/ui/Pagination.tsx     < 1 2 … N > with ellipsis + page-size dropdown
                shared/ui/SortableColumnHeader.tsx  click cycles ↕ → ↓ → ↑ → ↕
                features/filter-transactions/       TransactionFilterBar
                features/filter-trades/             TradeFilterBar
                pages/transactions/                 two queries: summary + paged table
                pages/trades/                       two queries: summary + paged table
                pages/budgets/                      useTransactions({ month, pageSize: 100 })
```

**Key invariants:**
- `GetByUserAsync` is kept on both repository interfaces — still used by `BudgetOverrunJob`.
- `PageSize` is clamped to max 100 in the query handler, not only in the validator.
- Summary endpoints share the same filter params as list endpoints; page/sort params are absent.
- FluentValidation returns HTTP 400 for unknown `sortBy` values and out-of-range `page`/`pageSize`.

---

## 3. API Reference

### `GET /api/transactions`

| Param | Type | Default | Validation |
|---|---|---|---|
| `page` | int | 1 | ≥ 1 |
| `pageSize` | int | 20 | 1–100 |
| `search` | string? | — | Matches `Note` or category name (case-insensitive contains) |
| `month` | string? | — | Format `yyyy-MM` |
| `type` | string? | — | `Income` or `Expense` |
| `categoryId` | Guid? | — | |
| `sortBy` | string | `date` | `date`, `amount`, `category` |
| `sortDir` | string | `desc` | `asc` or `desc` |

**Response 200:**
```json
{
  "items": [...],
  "page": 1,
  "pageSize": 20,
  "totalCount": 143,
  "totalPages": 8,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

**Response 400** — unknown `sortBy`, `page < 1`, or `pageSize > 100` (FluentValidation problem details).

---

### `GET /api/transactions/summary`

Accepts `month`, `type`, `categoryId` — same filters as list, no page/sort.

**Response 200:**
```json
{ "totalIncome": 5000.00, "totalExpense": 3200.00, "netBalance": 1800.00 }
```

---

### `GET /api/trades`

| Param | Type | Default | Validation |
|---|---|---|---|
| `page` | int | 1 | ≥ 1 |
| `pageSize` | int | 20 | 1–100 |
| `search` | string? | — | Matches `Symbol` (case-insensitive contains) |
| `status` | string? | — | `Open` or `Closed` |
| `direction` | string? | — | `Long` or `Short` |
| `dateFrom` | DateOnly? | — | Must be ≤ `dateTo` if both set |
| `dateTo` | DateOnly? | — | |
| `sortBy` | string | `date` | `date`, `pnl`, `symbol`, `entryPrice`, `size`, `fees` |
| `sortDir` | string | `desc` | `asc` or `desc` |

**Response 200:** same `PagedResult<T>` shape as transactions.

**Response 400** — unknown `sortBy`, `dateFrom > dateTo`, `page < 1`, `pageSize > 100`.

---

### `GET /api/trades/summary`

Accepts `status`, `direction`, `dateFrom`, `dateTo` — same filters as list, no page/sort.

**Response 200:**
```json
{ "totalPnl": 1240.50, "winRate": 62, "totalTrades": 34, "unrealizedPnl": 320.00 }
```

---

## 4. UX Conventions

| Convention | Detail |
|---|---|
| Pagination style | Classic numbered `< 1 2 … N >`; ellipsis appears when total pages > 7 |
| Default page size | 20; configurable via dropdown (10 / 20 / 50) |
| Sort cycle | Click column header: default → desc → asc → reset (null); indicators ↕ ↓ ↑ |
| Search debounce | 300 ms — query fires only after user stops typing |
| Filter state | In-memory only; not persisted to URL (explicitly deferred to a future iteration) |
| Budgets | No pagination, no search — over-budget toggle + client-side sort by spent % or category name |

---

## 5. Backend Components

### New files

| File | Purpose |
|---|---|
| `Application/Common/Models/PagedResult.cs` | `record PagedResult<T>` with `TotalPages`, `HasPreviousPage`, `HasNextPage` |
| `Domain/Repositories/TransactionPageQuery.cs` | Plain record: `Page`, `PageSize`, `Search`, `Month`, `Type?`, `CategoryId?`, `SortBy`, `SortDir` |
| `Domain/Repositories/TradePageQuery.cs` | Plain record: `Page`, `PageSize`, `Search`, `Status?`, `Direction?`, `DateFrom?`, `DateTo?`, `SortBy`, `SortDir` |
| `Application/Finance/Queries/GetTransactions/GetTransactionsQueryValidator.cs` | Validates page ≥ 1, pageSize 1–100, sortBy in allowed set, month format `yyyy-MM` |
| `Application/Trading/Queries/GetTrades/GetTradesQueryValidator.cs` | Same pattern; validates `dateFrom ≤ dateTo` |
| `Application/Finance/Queries/GetTransactionSummary/GetTransactionSummaryQuery.cs` | `IRequest<TransactionSummaryDto>` |
| `Application/Finance/Queries/GetTransactionSummary/GetTransactionSummaryQueryHandler.cs` | Aggregates `TotalIncome`, `TotalExpense`, `NetBalance` — no rows loaded |
| `Application/Trading/Queries/GetTradeSummary/GetTradeSummaryQuery.cs` | `IRequest<TradeSummaryDto>` |
| `Application/Trading/Queries/GetTradeSummary/GetTradeSummaryQueryHandler.cs` | Aggregates `TotalPnl`, `WinRate`, `TotalTrades`, `UnrealizedPnl` — no rows loaded |
| `Application/Finance/DTOs/TransactionSummaryDto.cs` | `record TransactionSummaryDto(decimal TotalIncome, decimal TotalExpense, decimal NetBalance)` |
| `Application/Trading/DTOs/TradeSummaryDto.cs` | `record TradeSummaryDto(decimal TotalPnl, int WinRate, int TotalTrades, decimal UnrealizedPnl)` |

### Modified files

| File | Change |
|---|---|
| `ITransactionRepository.cs` | Added `GetPagedAsync(Guid userId, TransactionPageQuery, CancellationToken)` — `GetByUserAsync` kept |
| `ITradeRepository.cs` | Added `GetPagedAsync` — `GetByUserAsync` kept |
| `TransactionRepository.cs` | Implemented `GetPagedAsync`: filter → `CountAsync` → sort switch → `Skip/Take` |
| `TradeRepository.cs` | Same pattern; P&L sort arm uses inline LINQ `CASE WHEN` (see §1) |
| `GetTransactionsQuery.cs` | Extended with `Page`, `PageSize`, `Search`, `Type`, `CategoryId`, `SortBy`, `SortDir`; return type `PagedResult<TransactionDto>` |
| `GetTradesQuery.cs` | Same; return type `PagedResult<TradeDto>` |
| `GetTransactionsQueryHandler.cs` | Calls `GetPagedAsync`; preserves subscription enforcement block; clamps `PageSize` to 100 |
| `GetTradesQueryHandler.cs` | Same pattern |
| `TransactionsController.cs` | `GetAll` accepts query params; added `GET /api/transactions/summary` |
| `TradesController.cs` | Same; added `GET /api/trades/summary` |

---

## 6. Frontend Components

### New files

| File | Purpose |
|---|---|
| `shared/api/types.ts` | `PagedResult<T>` TypeScript interface matching backend response |
| `shared/lib/cleanParams.ts` | Strips `undefined` and `''` values from params object before Axios call |
| `shared/lib/useDebounce.ts` | `function useDebounce<T>(value: T, delayMs: number): T` |
| `shared/ui/Pagination.tsx` | Props: `page`, `totalPages`, `pageSize`, `onPageChange`, `onPageSizeChange`, `pageSizeOptions?`, `disabled?` |
| `shared/ui/SortableColumnHeader.tsx` | Props: `label`, `field`, `currentSortBy`, `currentSortDir`, `onSort`, `align?` |
| `features/filter-transactions/ui/TransactionFilterBar.tsx` | Search input, month select, type toggle (All/Income/Expense), category dropdown |
| `features/filter-transactions/index.ts` | Barrel export |
| `features/filter-trades/ui/TradeFilterBar.tsx` | Search input, status toggle, direction toggle, date range (from/to) |
| `features/filter-trades/index.ts` | Barrel export |

### Modified files

| File | Change |
|---|---|
| `entities/transaction/api/transactionApi.ts` | `useTransactions(params)` replaces `useTransactions(month?)`; added `useTransactionSummary(params)`; query keys `['transactions', params]` / `['transactions', 'summary', params]` |
| `entities/trade/api/tradeApi.ts` | `useTrades(params)` replaces `useTrades()`; added `useTradesSummary(params)`; query keys `['trades', params]` / `['trades', 'summary', params]` |
| `pages/transactions/ui/TransactionsPage.tsx` | Two independent queries (summary + paged table); `TransactionFilterBar`, `SortableColumnHeader`, `Pagination` wired in; page resets to 1 on filter/sort change |
| `pages/trades/ui/TradesPage.tsx` | Two independent queries; client-side status filter removed (now server-side); `TradeFilterBar`, sortable headers (Date, P&L, Symbol, Entry Price, Position Size, Fees), `Pagination` |
| `pages/budgets/ui/BudgetsPage.tsx` | `useTransactions({ month, pageSize: 100 })`; reads `data.items`; over-budget toggle; client-side sort via `useMemo` |
| `shared/ui/index.ts` | Exports `Pagination` and `SortableColumnHeader` |

---

## 7. Testing

### Backend

| File | Type |
|---|---|
| `Application.UnitTests/Finance/GetTransactionsHandlerTests.cs` | Updated: return type `PagedResult<TransactionDto>`, access items via `.Items`; added paginated shape, search, type filter, sort param, summary aggregates cases |
| `Application.UnitTests/Trading/GetTradesHandlerTests.cs` | Updated: same pattern; added status/direction filter, summary aggregates cases |
| `Infrastructure.UnitTests/Repositories/TransactionRepositoryTests.cs` | New: `GetPagedAsync` with EF Core in-memory — search, filter, pagination boundaries, sort |
| `Infrastructure.UnitTests/Repositories/TradeRepositoryTests.cs` | New: same pattern + P&L sort case (confirms SQL expression does not throw) |
| `Api.IntegrationTests/Features/Finance/TransactionsTests.cs` | Updated: response shape is `PagedResult<T>`, not flat array; added paged response, search, invalid sortBy → 400, summary endpoint cases |
| `Api.IntegrationTests/Features/Trading/TradesTests.cs` | Updated: same; added status filter, P&L sort, summary endpoint cases |

### Frontend

| File | Type |
|---|---|
| `shared/ui/Pagination.test.tsx` | New: renders correct pages, ellipsis logic, calls `onPageChange` |
| `shared/ui/SortableColumnHeader.test.tsx` | New: click cycles desc → asc → null |
| `entities/transaction/api/transactionApi.test.ts` | Updated: mock returns `PagedResult<Transaction>`; assertions access `.items` |
| `entities/trade/api/tradeApi.test.ts` | Updated: same |
| `pages/transactions/TransactionsPage.test.tsx` | Updated: mocks return paged shape; mock added for `useTransactionSummary` |
| `pages/trades/TradesPage.test.tsx` | Updated: same; mock added for `useTradesSummary` |
| `tests/e2e/transactions.spec.ts` | Updated: assertions updated for page-1-only load; added search, sort, pagination, KPI accuracy cases |
| `tests/e2e/trades.spec.ts` | Updated: same |

---

## 8. Verification

```bash
# Backend smoke tests
GET http://localhost:5018/api/transactions?page=1&pageSize=5&sortBy=amount&sortDir=desc
GET http://localhost:5018/api/transactions?search=rent&month=2026-04
GET http://localhost:5018/api/transactions?page=1&sortBy=invalid          # → 400
GET http://localhost:5018/api/trades?status=Open&sortBy=pnl&sortDir=desc
GET http://localhost:5018/api/transactions/summary?month=2026-04
GET http://localhost:5018/api/transactions/summary?month=2026-04&type=Expense
GET http://localhost:5018/api/trades/summary
GET http://localhost:5018/api/trades/summary?status=Closed&dateFrom=2026-01-01&dateTo=2026-03-31

# Backend tests
cd backend
dotnet test --filter "Category!=Integration"   # unit tests
dotnet test --filter "Category=Integration"    # integration tests

# Frontend compile check (catches all broken call sites)
cd frontend/fintrackpro-ui
npm run build

# Frontend E2E
bash scripts/e2e-local.sh tests/e2e/transactions.spec.ts
bash scripts/e2e-local.sh tests/e2e/trades.spec.ts
```

**Checklist:**
- [ ] `GET /api/transactions?page=1&pageSize=5` returns `{ items, page, pageSize, totalCount, totalPages, hasPreviousPage, hasNextPage }`
- [ ] `GET /api/transactions?sortBy=invalid` returns 400
- [ ] `GET /api/transactions?pageSize=200` returns 400
- [ ] `GET /api/transactions/summary` returns `{ totalIncome, totalExpense, netBalance }` — values are numbers
- [ ] KPI cards on TransactionsPage reflect full filtered dataset, not only the current page
- [ ] Sorting a column does not reset KPI values
- [ ] `GET /api/trades?status=Open&sortBy=pnl` returns P&L sort without throwing
- [ ] `GET /api/trades/summary` returns `{ totalPnl, winRate, totalTrades, unrealizedPnl }`; `winRate` in [0, 100]
- [ ] BudgetsPage `spentByCategory` is computed from `data.items`, not `data`
- [ ] `npm run build` passes with zero TypeScript errors
- [ ] `dotnet test --filter "Category!=Integration"` passes
- [ ] `dotnet test --filter "Category=Integration"` passes

---

## Known Limitations

- BudgetsPage spending totals are capped at 100 transactions/month — needs a dedicated
  `/api/budgets/{month}/summary` endpoint.
- Filter state is not persisted to the URL (explicitly deferred to a future iteration).
