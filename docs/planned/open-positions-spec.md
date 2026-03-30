# Spec: Open Positions & Trade Status Model

## 1. Overview

### 1.1 Problem Statement
The current trade journal enforces exit price as a required field on every trade entry, making it impossible to log open positions, DCA entries, or long-term holdings (BTC, Gold, stocks). Users who invest gradually over time or hold assets indefinitely cannot use the journal without fabricating data.

### 1.2 Proposed Solution
Extend the existing Trades page with an Open/Closed status model. A trade is either an **Open Position** (no exit price, optional current price for unrealized P&L estimation) or a **Closed Trade** (exit price required, realized P&L calculated). A dedicated "Close Position" action on each open row allows the user to close it later with minimal friction.

### 1.3 Goals
- Allow logging of trades without an exit price
- Display both open and closed trades in one unified table
- Correctly separate realized P&L (closed) from unrealized P&L (open) in summary cards
- Allow closing an open position later via a focused modal

### 1.4 Non-Goals (Out of Scope — v1)
- DCA position grouping under a parent "Position" entity
- Auto-fetching current price from any market API
- Risk metrics, strategy tags, performance analytics
- Bulk close / bulk edit

---

## 2. Users & Personas

| Persona | Description | Key needs |
|---|---|---|
| Short-term trader | Logs completed trades for win rate and P&L tracking | Fast entry, clear realized P&L |
| Long-term investor | DCA-buys BTC, Gold, stocks; rarely sells | Log buys without exit price, see unrealized P&L estimate |

---

## 3. Functional Requirements

### 3.1 Trade Status Toggle on Add Form

**User Story**
> As a trader, I want to mark a new trade as Open or Closed when I log it, so that I am not forced to provide an exit price for positions I haven't sold yet.

**Acceptance Criteria**

| # | Given | When | Then |
|---|---|---|---|
| AC-1 | The Add Trade form is open | The user selects "Open Position" | Exit Price field is hidden; "Current Price (optional)" field is shown |
| AC-2 | The Add Trade form is open | The user selects "Closed Trade" | "Current Price" field is hidden; Exit Price field is shown and required |
| AC-3 | "Open Position" is selected | The user submits the form without a Current Price | The trade is saved successfully with `currentPrice = null` |
| AC-4 | "Closed Trade" is selected | The user submits the form without an Exit Price | Validation error is shown: "Exit price is required for a closed trade" |
| AC-5 | Either status is selected | The form is submitted | The trade is saved with the correct `status` value (`Open` or `Closed`) |

---

### 3.2 Unified Trade Table with Status Column

**User Story**
> As a trader, I want to see all my trades — open and closed — in one table, so that I have a complete picture of my activity without switching views.

**Acceptance Criteria**

| # | Given | When | Then |
|---|---|---|---|
| AC-1 | The trades list has both open and closed trades | The page loads | All trades appear in the same table sorted by date descending |
| AC-2 | A trade has `status = Open` | The row is rendered | STATUS column shows a green "Open" badge; Exit/Current Price column shows the current price (or "—" if null); P&L value is italic/muted to signal "estimated" |
| AC-3 | A trade has `status = Closed` | The row is rendered | STATUS column shows a grey "Closed" badge; Exit/Current Price column shows the exit price; P&L value is bold green or red |
| AC-4 | Any trade row is rendered | — | Column order is: Symbol, Direction, Status, Entry Price, Exit/Current Price, Position Size, Fees, P&L, Date, Actions |

---

### 3.3 Close Position Action

**User Story**
> As an investor, I want to close an open position directly from the trades table, so that I can record the final exit price without re-entering all the original trade data.

**Acceptance Criteria**

| # | Given | When | Then |
|---|---|---|---|
| AC-1 | A trade has `status = Open` | The row is rendered | A "Close" button/icon is visible in the Actions column |
| AC-2 | The user clicks "Close" on an open trade | — | A modal opens pre-filled with: Symbol (read-only), Entry Price (read-only), Position Size (read-only). Editable fields: Exit Price (required), Fees (optional), Date |
| AC-3 | The modal is submitted with a valid Exit Price | — | The trade is updated: `status = Closed`, `exitPrice` saved, `currentPrice` discarded (set to null), realized P&L recalculated |
| AC-4 | The modal is submitted without an Exit Price | — | Validation error shown: "Exit price is required to close a position" |
| AC-5 | The trade is successfully closed | — | The modal closes, the table row updates in place to reflect `status = Closed` without a full page reload |

---

### 3.4 Summary Cards

**User Story**
> As a trader, I want the summary cards to correctly reflect only my completed trades for performance metrics, with a separate card for estimated unrealized gains, so that I am not misled by mixed figures.

**Acceptance Criteria**

| # | Given | When | Then |
|---|---|---|---|
| AC-1 | The page loads | — | "Total P&L" and "Win Rate" cards count **closed trades only** |
| AC-2 | The page loads | — | "Total Trades" card counts **all trades** (open + closed) |
| AC-3 | At least one open position with a current price exists | The page loads | A 4th card "Unrealized P&L" appears showing the sum of `(currentPrice − entryPrice) × positionSize` for all open trades with a non-null current price |
| AC-4 | No open positions exist | The page loads | The "Unrealized P&L" card is not rendered |

---

## 4. Non-Functional Requirements

| Category | Requirement |
|---|---|
| Performance | Table renders within 2 seconds for up to 500 trade rows |
| Usability | "Close Position" modal requires no more than 2 required fields (Exit Price + confirm) |
| Data integrity | Closing a position is atomic — status, exitPrice, and currentPrice are updated in a single DB transaction |
| Backwards compatibility | All existing trade rows must be migrated to `status = Closed` without data loss |

---

## 5. Data & Domain Model

| Entity | Key attributes | Change |
|---|---|---|
| `Trade` | `status: enum(Open, Closed)` | **New field** |
| `Trade` | `currentPrice: decimal?` | **New field** — nullable; only meaningful when `status = Open` |
| `Trade` | `exitPrice: decimal?` | **Changed** — now nullable at DB level; required by validation only when `status = Closed` |

**Derived (not stored):**
- **Realized P&L** = `(exitPrice − entryPrice) × positionSize − fees` (when Closed, Long)
- **Unrealized P&L** = `(currentPrice − entryPrice) × positionSize` (when Open, currentPrice not null)

---

## 6. Error & Edge Case Handling

| Scenario | Expected behaviour |
|---|---|
| User submits Closed trade with no exit price | Validation error before API call: "Exit price is required for a closed trade" |
| User closes a position with no exit price in modal | Modal-level validation error: "Exit price is required to close a position" |
| Open trade has no current price | Unrealized P&L shown as "—" in the row; trade excluded from Unrealized P&L summary card |
| Existing trades in DB have no `status` field | DB migration backfills `status = Closed` for all existing rows |
| Short direction unrealized P&L | Calculated as `(entryPrice − currentPrice) × positionSize` |

---

## 7. Open Questions / TBDs

| # | Question | Owner | Target date |
|---|---|---|---|
| 1 | Should open trades be editable (change current price, notes) via the existing Edit flow? | Product | TBD |
| 2 | Should the "Close Position" date default to today or the original trade date? | UX | TBD |

---

## 8. Success Metrics

| Metric | Target |
|---|---|
| Zero validation errors on legitimate open trade submissions | 100% after release |
| Existing closed trades unaffected after migration | 100% data integrity verified by integration test |

---

## Implementation Tasks

### TASK-1: Add `status` and `currentPrice` fields to the Trade entity and run DB migration
**Phase:** Setup
**Spec ref:** [Spec §5]
**Depends on:** none
**Effort estimate:** S

**Description:**
Add `status` (enum: `Open`/`Closed`, default `Closed`) and `currentPrice` (nullable decimal) to the `Trade` domain entity, EF Core configuration, and generate a migration. The migration must backfill `status = Closed` for all existing rows and make `exitPrice` nullable at the DB level.

**Done when:**
- [ ] `Trade` entity has `Status` and `CurrentPrice` properties
- [ ] EF Core migration generated and applies cleanly on a fresh DB
- [ ] All existing rows have `status = 'Closed'` after migration
- [ ] `exitPrice` column is nullable in the DB schema

---

### TASK-2: Update backend validation — exit price required only for Closed trades
**Phase:** Core Features
**Spec ref:** [Spec §3.1 AC-4, §6]
**Depends on:** TASK-1
**Effort estimate:** S

**Description:**
Update the `CreateTradeCommand` and `UpdateTradeCommand` FluentValidation validators so that `ExitPrice` is required only when `Status = Closed`. Remove the unconditional exit price required rule.

**Done when:**
- [ ] `POST /trades` with `status = Open` and no exit price returns HTTP 201
- [ ] `POST /trades` with `status = Closed` and no exit price returns HTTP 400 with message "Exit price is required for a closed trade"
- [ ] Existing passing validation tests are not broken

---

### TASK-3: Add `ClosePositionCommand` handler and PATCH endpoint
**Phase:** Core Features
**Spec ref:** [Spec §3.3]
**Depends on:** TASK-1
**Effort estimate:** M

**Description:**
Implement a `ClosePositionCommand` (MediatR) that sets `status = Closed`, stores `exitPrice`, sets `currentPrice = null`, and recalculates realized P&L atomically. Expose it as `PATCH /trades/{id}/close`.

**Done when:**
- [ ] `PATCH /trades/{id}/close` with valid exit price returns HTTP 200 with updated trade DTO
- [ ] Trade row has `status = Closed`, `exitPrice` set, `currentPrice = null` after the call
- [ ] `PATCH /trades/{id}/close` without exit price returns HTTP 400
- [ ] Calling the endpoint on an already-closed trade returns HTTP 409
- [ ] Operation is executed in a single DB transaction

---

### TASK-4: Update trade list and detail DTOs to include `status` and `currentPrice`
**Phase:** Core Features
**Spec ref:** [Spec §3.2, §5]
**Depends on:** TASK-1
**Effort estimate:** XS

**Description:**
Add `Status` and `CurrentPrice` to `TradeDto` (and any list/summary projections). Update the explicit `operator` conversion from the `Trade` entity.

**Done when:**
- [ ] `GET /trades` response includes `status` and `currentPrice` on every trade object
- [ ] `status` value is `"Open"` or `"Closed"` (string enum serialization)

---

### TASK-5: Update Add Trade form — Open/Closed toggle and conditional fields
**Phase:** Core Features
**Spec ref:** [Spec §3.1]
**Depends on:** TASK-2, TASK-4
**Effort estimate:** M

**Description:**
Replace the current always-visible Exit Price field with an Open/Closed toggle on the Add Trade form. When "Open Position" is selected, hide Exit Price and show an optional "Current Price" field. When "Closed Trade" is selected, hide Current Price and show Exit Price as required. Submit the correct `status` value to the API.

**Done when:**
- [ ] Toggle renders with "Open Position" as the default selected state
- [ ] Selecting "Open" hides Exit Price, shows Current Price (optional)
- [ ] Selecting "Closed" shows Exit Price (required), hides Current Price
- [ ] Submitting an open trade without current price succeeds
- [ ] Submitting a closed trade without exit price shows inline validation error
- [ ] `status` field is included in the POST request body

---

### TASK-6: Update trade table — STATUS column and adaptive Exit/Current Price column
**Phase:** Core Features
**Spec ref:** [Spec §3.2]
**Depends on:** TASK-4
**Effort estimate:** M

**Description:**
Add a STATUS column to the trades table. Rename "Exit Price" column to "Exit / Current Price". For open rows, render current price (or "—" if null) with italic/muted P&L. For closed rows, render exit price with bold colored P&L. Add STATUS badges: green "Open", grey "Closed".

**Done when:**
- [ ] STATUS column appears between Direction and Entry Price
- [ ] Open rows show green "Open" badge, muted italic P&L
- [ ] Closed rows show grey "Closed" badge, bold green/red P&L
- [ ] Open rows with no current price show "—" in the Exit/Current Price column
- [ ] Column order matches spec §3.2 AC-4

---

### TASK-7: Implement Close Position modal
**Phase:** Core Features
**Spec ref:** [Spec §3.3]
**Depends on:** TASK-3, TASK-6
**Effort estimate:** M

**Description:**
Add a "Close" icon/button in the Actions column for open trade rows only. Clicking it opens a modal pre-filled with Symbol, Entry Price, and Position Size (read-only). The modal has two editable fields: Exit Price (required) and Fees (optional), plus a Date field. On submit, call `PATCH /trades/{id}/close` and update the row in place on success.

**Done when:**
- [ ] "Close" button is visible only on open trade rows
- [ ] Modal opens with correct pre-filled read-only values
- [ ] Submitting without exit price shows validation error inside the modal
- [ ] Successful submission closes the modal and the row updates to "Closed" without page reload
- [ ] Closed trade rows do not show the "Close" button

---

### TASK-8: Update summary cards — split realized vs. unrealized P&L
**Phase:** Core Features
**Spec ref:** [Spec §3.4]
**Depends on:** TASK-4
**Effort estimate:** S

**Description:**
Update the three existing summary cards so that Total P&L and Win Rate count closed trades only. Update Total Trades to count all trades. Add a conditional 4th card "Unrealized P&L" that sums `(currentPrice − entryPrice) × positionSize` for open trades with a non-null current price; render it only when at least one such trade exists.

**Done when:**
- [ ] Total P&L reflects closed trades only
- [ ] Win Rate reflects closed trades only
- [ ] Total Trades counts open + closed
- [ ] Unrealized P&L card appears when ≥1 open trade has a current price
- [ ] Unrealized P&L card is absent when no open trades have a current price
- [ ] Short direction unrealized P&L uses `(entryPrice − currentPrice) × positionSize`

---

### TASK-9: Write backend unit and integration tests
**Phase:** Testing
**Spec ref:** [Spec §3.1, §3.3, §6]
**Depends on:** TASK-2, TASK-3
**Effort estimate:** M

**Description:**
Write unit tests for the updated validators and `ClosePositionCommand` handler. Write integration tests for `POST /trades` (open and closed), `PATCH /trades/{id}/close` (happy path, missing exit price, already-closed conflict), and the DB migration backfill.

**Done when:**
- [ ] Unit test: open trade with no exit price passes validation
- [ ] Unit test: closed trade with no exit price fails validation with correct message
- [ ] Integration test: `POST /trades` open → HTTP 201, no exit price stored
- [ ] Integration test: `PATCH /trades/{id}/close` → HTTP 200, `currentPrice = null`, `status = Closed`
- [ ] Integration test: `PATCH /trades/{id}/close` on already-closed trade → HTTP 409
- [ ] Integration test: existing rows have `status = Closed` after migration
- [ ] All tests pass in CI

---

### TASK-10: Write frontend unit tests
**Phase:** Testing
**Spec ref:** [Spec §3.1, §3.2, §3.3, §3.4]
**Depends on:** TASK-5, TASK-6, TASK-7, TASK-8
**Effort estimate:** S

**Description:**
Write Vitest component tests for the Add Trade form toggle behavior, the trade table row rendering (open vs. closed), the Close Position modal validation, and the summary cards conditional rendering.

**Done when:**
- [ ] Test: selecting "Open" hides Exit Price, shows Current Price
- [ ] Test: selecting "Closed" shows Exit Price as required
- [ ] Test: open row renders green badge and muted P&L
- [ ] Test: closed row renders grey badge and bold P&L
- [ ] Test: Close modal shows error when exit price is empty on submit
- [ ] Test: Unrealized P&L card renders only when open trades with current price exist

---

### TASK-11: Update API and architecture documentation
**Phase:** Documentation
**Spec ref:** [Spec §1, §3, §5]
**Depends on:** TASK-3, TASK-4
**Effort estimate:** XS

**Description:**
Update `docs/architecture/api-spec.md` with the new `PATCH /trades/{id}/close` endpoint, updated `TradeDto` shape, and the new `status`/`currentPrice` fields. Update `docs/architecture/database.md` with the schema changes.

**Done when:**
- [ ] `api-spec.md` documents `PATCH /trades/{id}/close` request/response
- [ ] `api-spec.md` shows updated `TradeDto` with `status` and `currentPrice`
- [ ] `database.md` reflects nullable `exitPrice`, new `status` and `currentPrice` columns
