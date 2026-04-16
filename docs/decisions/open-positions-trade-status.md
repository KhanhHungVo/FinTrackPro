# Open Positions & Trade Status Model

## Context

The trade journal previously required an exit price on every entry, making it impossible to log
open positions, DCA entries, or long-term holdings. This document describes the Open/Closed status
model added to the Trades domain, including schema changes, the new close-position endpoint, and
the split between realized and unrealized P&L in summary cards.

---

## 1. Key Decisions

### `exitPrice` made nullable; `status` drives validation

Rather than adding a parallel "open trade" entity, `exitPrice` is made nullable at the DB level
and a `status` enum (`Open` / `Closed`) is added. Validation moves the "exit price required" rule
from unconditional to conditional on `status = Closed`. Existing rows are backfilled to
`status = Closed` by the migration — no data loss, no behavioral change for existing users.

### `currentPrice` is optional and display-only

`currentPrice` is stored only to let users record an estimated unrealized P&L at entry time;
it is never auto-fetched. When `status = Open` and `currentPrice` is null, unrealized P&L is
shown as "—". When a position is closed via the close endpoint, `currentPrice` is set to null —
it is meaningless once a realized exit price is recorded.

### Dedicated `PATCH /trades/{id}/close` endpoint

Closing a position is a domain event distinct from a general edit. A dedicated endpoint makes the
intent explicit, enforces the atomic update (`status`, `exitPrice`, `currentPrice = null`) in a
single transaction, and returns HTTP 409 when called on an already-closed trade — preventing
accidental double-close.

### Summary cards split realized from unrealized P&L

After adding open positions, mixing open and closed trades in the same P&L aggregate would
produce misleading numbers. `TotalPnl` and `WinRate` are restricted to closed trades only;
`UnrealizedPnl` is a separate aggregate over open trades with a non-null `currentPrice`. The
`GET /api/trades/summary` endpoint already computes these server-side; only the aggregation
logic and the DTO field need updating.

---

## 2. Architecture Overview

```
Domain:         Trade.Status (TradeStatus enum: Open / Closed)
                Trade.CurrentPrice (decimal?)
                Trade.ExitPrice (decimal? — was non-nullable)
Application:    CreateTradeCommand / UpdateTradeCommand — conditional ExitPrice validation
                ClosePositionCommand — sets status=Closed, exitPrice, currentPrice=null atomically
                TradeDto — adds Status, CurrentPrice fields
                TradeSummaryDto — TotalPnl/WinRate from closed trades; UnrealizedPnl from open
API:            TradesController — PATCH /api/trades/{id}/close (new)
                TradesController — GET /api/trades, GET /api/trades/summary (updated projections)
Frontend:       features/add-trade/     — Open/Closed toggle; conditional Exit/Current Price fields
                features/close-trade/   — ClosePositionModal (Exit Price + Fees + Date)
                pages/trades/           — STATUS column, adaptive Exit/Current Price column,
                                          conditional Unrealized P&L card
```

**Key invariants:**
- `exitPrice` is nullable in the DB; required only when `status = Closed` (FluentValidation).
- `currentPrice` is always null for closed trades — enforced by `ClosePositionCommand`.
- Short-direction unrealized P&L = `(entryPrice − currentPrice) × positionSize`.
- `UnrealizedPnl` card is rendered only when ≥ 1 open trade has a non-null `currentPrice`.

---

## 3. API Reference

### `PATCH /api/trades/{id}/close`

```json
// Request
{ "exitPrice": 65000.00, "fees": 12.50, "exitDate": "2026-04-16" }
```

| Field | Type | Required |
|---|---|---|
| `exitPrice` | decimal | Yes |
| `fees` | decimal? | No |
| `exitDate` | DateOnly? | No — defaults to today |

**Response 200:** updated `TradeDto` with `status = "Closed"`, `currentPrice = null`.

**Response 400:** missing `exitPrice`.

**Response 404:** trade not found or does not belong to the caller.

**Response 409:** trade is already closed.

---

### Updated `TradeDto`

```json
{
  "id": "...",
  "symbol": "BTC",
  "direction": "Long",
  "status": "Open",
  "entryPrice": 60000.00,
  "exitPrice": null,
  "currentPrice": 65000.00,
  "positionSize": 0.5,
  "fees": 10.00,
  "date": "2026-01-15",
  "result": null,
  "unrealizedResult": 2490.00
}
```

`result` is null for open trades. `unrealizedResult` is null when `currentPrice` is null or
`status = Closed`.

---

### `GET /api/trades/summary` — updated aggregation

| Field | Scope |
|---|---|
| `totalPnl` | Closed trades only |
| `winRate` | Closed trades only |
| `totalTrades` | All trades (open + closed) |
| `unrealizedPnl` | Open trades with non-null `currentPrice`; 0 when none |

---

## 4. Domain Model Changes

| Field | Before | After |
|---|---|---|
| `Trade.ExitPrice` | `decimal` (non-nullable) | `decimal?` (nullable) |
| `Trade.Status` | — | `TradeStatus` enum: `Open` / `Closed` |
| `Trade.CurrentPrice` | — | `decimal?` — meaningful only when `Open` |

**Migration:** `AddTradeStatusAndCurrentPrice`
- Adds `Status` column (`varchar(10)`, not null, default `'Closed'`).
- Adds `CurrentPrice` column (`decimal(18,8)`, nullable).
- Makes `ExitPrice` nullable.
- Backfills `Status = 'Closed'` for all existing rows.

---

## 5. Backend Components

### New files

| File | Purpose |
|---|---|
| `Application/Trading/Commands/ClosePosition/ClosePositionCommand.cs` | `IRequest<TradeDto>`; fields: `TradeId`, `ExitPrice`, `Fees?`, `ExitDate?` |
| `Application/Trading/Commands/ClosePosition/ClosePositionCommandHandler.cs` | Loads trade, asserts `Open`, sets Closed state atomically, returns updated DTO |
| `Application/Trading/Commands/ClosePosition/ClosePositionCommandValidator.cs` | `ExitPrice > 0` required |

### Modified files

| File | Change |
|---|---|
| `Domain/Trading/Trade.cs` | Add `Status`, `CurrentPrice`; make `ExitPrice` nullable; add `Close(exitPrice, fees, date)` domain method |
| `Application/Trading/Commands/CreateTrade/CreateTradeCommandValidator.cs` | `ExitPrice` required only when `Status = Closed` |
| `Application/Trading/Commands/UpdateTrade/UpdateTradeCommandValidator.cs` | Same conditional rule |
| `Application/Trading/DTOs/TradeDto.cs` | Add `Status`, `CurrentPrice`, `UnrealizedResult` |
| `Application/Trading/Queries/GetTradeSummary/GetTradeSummaryQueryHandler.cs` | Scope `TotalPnl`/`WinRate` to closed; aggregate `UnrealizedPnl` from open |
| `TradesController.cs` | Add `PATCH /{id}/close` action |

---

## 6. Frontend Components

### New files

| File | Purpose |
|---|---|
| `features/close-trade/ui/ClosePositionModal.tsx` | Modal with read-only Symbol/EntryPrice/PositionSize; editable ExitPrice (required), Fees, Date |
| `features/close-trade/index.ts` | Barrel export |

### Modified files

| File | Change |
|---|---|
| `entities/trade/model/types.ts` | Add `status`, `currentPrice`, `unrealizedResult` to `Trade` type |
| `entities/trade/api/tradeApi.ts` | Add `useCloseTrade` mutation (`PATCH /{id}/close`) |
| `features/add-trade/ui/AddTradeForm.tsx` | Open/Closed toggle; conditionally show `exitPrice` or `currentPrice` field |
| `pages/trades/ui/TradesPage.tsx` | STATUS column; adaptive Exit/Current Price column; P&L style (bold vs. muted italic); conditional Unrealized P&L card; "Close" action on open rows |

---

## 7. UX Conventions

| Convention | Detail |
|---|---|
| Default status on Add form | "Open Position" — the more common new-entry case for this feature |
| Status badge | Green "Open" / grey "Closed" pill in the STATUS column |
| P&L rendering | Open: italic, muted — signals "estimated". Closed: bold, green or red |
| Exit/Current Price column | Single adaptive column — shows `exitPrice` for closed, `currentPrice` (or "—") for open |
| Close modal required fields | One required field only: Exit Price |

---

## 8. Testing

### Backend

| File | Type |
|---|---|
| `Application.UnitTests/Trading/ClosePositionHandlerTests.cs` | Happy path, already-closed 409, missing exit price 400 |
| `Application.UnitTests/Validators/CreateTradeCommandValidatorTests.cs` | Updated: open trade passes without exit price; closed trade fails without exit price |
| `Api.IntegrationTests/Features/Trading/TradesTests.cs` | Updated: `POST /trades` open → 201; `PATCH /{id}/close` → 200 / 409; migration backfill |

### Frontend

| File | Type |
|---|---|
| `features/add-trade/ui/AddTradeForm.test.tsx` | Toggle hides/shows fields; submission behavior per status |
| `features/close-trade/ui/ClosePositionModal.test.tsx` | Validation error on empty exit price; success closes modal |
| `pages/trades/TradesPage.test.tsx` | STATUS badges, P&L style, Unrealized P&L card conditional render |

---

## Known Limitations

- `currentPrice` is entered manually — no auto-fetch from market APIs (deferred to a future iteration).
- DCA entries are separate rows with no parent-position grouping (deferred to a future iteration).
