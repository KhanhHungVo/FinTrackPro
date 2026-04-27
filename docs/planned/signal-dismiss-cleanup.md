# Technical Design: Signal Dismiss Flow + 90-Day Auto-Cleanup

**Status:** Draft
**Active surfaces:** FE / BE / DB
**Stack:**
  - FE: React 19 + Vite, Feature-Sliced Design, React Query v5, Axios, i18next, lucide-react
  - BE: .NET 10, Clean Architecture, MediatR, FluentValidation, EF Core 9, Hangfire
  - DB: PostgreSQL (Render), EF Core Code-First migrations
**Related spec:** N/A — derived from conversation design decisions (2026-04-27)

---

## 1. Overview

Market signals (generated every 4 h by `MarketSignalJob`) currently accumulate indefinitely
and cannot be removed by users. This design adds a **dismiss flow**: users click an X button
on any signal card to soft-delete it (`DismissedAt` timestamp), immediately hiding it from
their feed via an optimistic UI update. A new daily Hangfire job
(`SignalCleanupJob`) hard-deletes dismissed signals older than 90 days, keeping the table
lean while preserving short-term audit history. Active signals (never dismissed) are never
automatically deleted.

---

## 2. Architecture Decisions

### 2.1 — Soft delete (`DismissedAt`) instead of hard delete on dismiss

**Context:** Signals are auto-generated from reputable market data. They have audit value —
a user may want to correlate a past signal against a trade they made.

**Decision:** Add a nullable `DismissedAt` column. The dismiss API sets it; the feed query
filters it out. A scheduled cleanup job hard-deletes rows older than 90 days.

**Alternatives considered:**
- *Hard delete on dismiss* — irreversible; prevents future "Signal History" page; conflicts
  with job deduplication logic in `MarketSignalJob` (24-hour `ExistsRecentAsync` window).
- *localStorage flag* — no server state, lost across devices/browsers, cannot enforce
  cleanup, inconsistent with the rest of the app's persistence model.

**Consequences:** Extra nullable column; feed query gains one `WHERE` predicate (cheap —
filtered against an already-narrow per-user result set).

### 2.2 — `PATCH /api/signals/{id}/dismiss` (not DELETE)

**Context:** HTTP verb should reflect the operation semantics.

**Decision:** `PATCH` — this is a **state transition** (setting `DismissedAt`), not a
resource removal. The signal continues to exist in the DB. `DELETE` would mislead callers
into expecting the record is gone.

**Alternatives considered:**
- *DELETE verb* — semantically wrong; signal is not hard-deleted from the DB at this point.
- *POST /api/signals/{id}/dismiss* — acceptable but `PATCH` is idiomatic for partial updates.

**Consequences:** Callers receive `204 No Content` on success. Second call is idempotent
(domain method guards against re-setting `DismissedAt`).

### 2.3 — Optimistic removal with rollback (no confirmation dialog)

**Context:** The transaction card uses a confirmation dialog before deletion. Signals are
auto-generated, not user-created data — dismissing one is low-stakes and reversible via
re-generation.

**Decision:** Remove the signal from the React Query cache immediately on click (`onMutate`).
Rollback via snapshot on API error. No `ConfirmDeleteDialog` — the UX overhead is not
justified.

**Alternatives considered:**
- *Invalidate-on-success (no optimistic)* — visible re-fetch flicker since `staleTime` is
  4 minutes; the card would linger until the server confirms.
- *Confirmation dialog* — unnecessary friction for a non-destructive action.

**Consequences:** Must handle rollback cleanly; `useGuardedMutation` prevents duplicate
in-flight calls on rapid clicks.

### 2.4 — Filtered partial index on `DismissedAt`

**Context:** The cleanup job issues a cross-user `DELETE WHERE DismissedAt < cutoff`. A
full-column index on `DismissedAt` would include all NULL rows (active signals), making it
large and mostly useless for the feed query.

**Decision:** Partial index `WHERE DismissedAt IS NOT NULL`. Only dismissed rows are indexed,
keeping the index tiny and directly useful for the cleanup query.

**Alternatives considered:**
- *Composite index (UserId, DismissedAt)* — useful if queries filter by user AND dismissed
  state, but the cleanup job is cross-user; unnecessary overhead.
- *No index* — sequential scan across potentially millions of rows in the cleanup job.

**Consequences:** The existing `IX_Signals_UserId_CreatedAt` index continues to serve the
feed query. PostgreSQL applies `DismissedAt == null` as a cheap residual predicate on the
already-narrow per-user result set.

---

## 3. Frontend Design

### 3.1 UI/UX Wireframes

#### Signal card — current state
```
┌────────────────────────────────────────────────────────┐
│ [Volume Spike]  MASKUSDT                    4/27/2026  │
│                 Volume spike detected…                  │
└────────────────────────────────────────────────────────┘
```

#### Signal card — after this change
```
┌──────────────────────────────────────────────────────────────┐
│ [Volume Spike]  MASKUSDT               4/27/2026  [✕]       │
│                 Volume spike detected…                        │
└──────────────────────────────────────────────────────────────┘
                                                     ↑
                                         X button (size=12, gray→red on hover)
                                         No confirmation dialog
```

#### Optimistic dismiss (in-flight)
```
┌──────────────────────────────────────────────────────────────┐
│ [Volume Spike]  MASKUSDT               4/27/2026  [✕]       │  ← card gone immediately
│                 (card removed from list instantly)            │
└──────────────────────────────────────────────────────────────┘
```

#### Error rollback
```
┌──────────────────────────────────────────────────────────────┐
│ [Volume Spike]  MASKUSDT               4/27/2026  [✕]       │  ← card reappears
│                 Volume spike detected…                        │
├──────────────────────────────────────────────────────────────┤
│ 🔴 toast: "Failed to dismiss signal"                         │
└──────────────────────────────────────────────────────────────┘
```

### 3.2 Component Structure

```
DismissSignalButton                        [features/dismiss-signal]
  Responsibility: Renders the X button and fires the dismiss mutation
  Props:          signalId: string — the signal to dismiss
  State managed:  in-flight guard via useGuardedMutation (ref-based, not useState)
  Emits:          none — side-effects via React Query cache manipulation

SignalsList                                [widgets/signals-list] — MODIFIED
  Responsibility: Lists active signals; now includes DismissSignalButton per card
  Props:          count?: number (default 20)
  No new state — dismiss is handled entirely inside DismissSignalButton
```

### 3.3 State & Data Flow

```
useSignals(count)                    → React Query cache: ['signals', count]
  ↓ optimistic onMutate
useDismissSignal.mutate(id)
  ├─ cancelQueries(['signals'])
  ├─ snapshot all ['signals', *] entries
  ├─ setQueriesData(['signals', *]) — filter out dismissed id
  ├─ PATCH /api/signals/{id}/dismiss
  │    ├─ 204 → onSettled: invalidateQueries(['signals'])
  │    └─ error → onError: restore snapshots + toast
  └─ useGuardedMutation prevents second call while first is in-flight
```

---

## 4. Database Design

### 4.1 Schema Diagram

```
┌─────────────────────────────────────────┐
│ Signals                                 │
├─────────────────────────────────────────┤
│ PK  Id            uuid                  │
│ FK  UserId        uuid  ──────▶ Users   │
│     Symbol        varchar(20)           │
│     SignalType    integer               │
│     Message       varchar(500)          │
│     Value         numeric(18,8)         │
│     Timeframe     varchar(10)           │
│     IsNotified    boolean               │
│     CreatedAt     timestamptz           │
│     DismissedAt   timestamptz  NULL     │  ← NEW
├─────────────────────────────────────────┤
│ IX  (UserId, CreatedAt)                 │  existing
│ IX  (DismissedAt) WHERE NOT NULL        │  NEW — partial, for cleanup job
└─────────────────────────────────────────┘
```

### 4.2 Migrations

- `AddSignalDismissedAt` — `ALTER TABLE Signals ADD COLUMN DismissedAt timestamptz NULL` +
  `CREATE INDEX IX_Signals_DismissedAt ON Signals (DismissedAt) WHERE DismissedAt IS NOT NULL`

### 4.3 Access Patterns

| Operation | Filter | Index used |
|---|---|---|
| Feed query (`GetLatestByUserAsync`) | `UserId = X AND DismissedAt IS NULL ORDER BY CreatedAt DESC TAKE N` | `IX_Signals_UserId_CreatedAt` + cheap residual predicate |
| Cleanup job (`DeleteOldDismissedAsync`) | `DismissedAt IS NOT NULL AND DismissedAt < cutoff` | `IX_Signals_DismissedAt` (partial) |
| Dedup check (`ExistsRecentAsync`) | `UserId = X AND Symbol = Y AND SignalType = Z AND CreatedAt >= cutoff` | `IX_Signals_UserId_CreatedAt` — unchanged |
| Dismiss command (`GetByIdAsync`) | `Id = X` (PK lookup) | PK index |

---

## 5. Backend Design

### 5.1 Layer / Component Breakdown

```
Signal.Dismiss()                           [Domain — Entity]
  Responsibility: Set DismissedAt = UtcNow; idempotent (no-op if already set)
  Depended on by: DismissSignalCommandHandler

ISignalRepository (additions)             [Domain — Interface]
  GetByIdAsync(Guid id) → Signal?
  DeleteOldDismissedAsync(DateTime cutoff) → int (deleted count)
  Depended on by: DismissSignalCommandHandler, SignalCleanupJob

DismissSignalCommand                      [Application — Command]
  Record: (Guid Id)
  Depended on by: SignalsController.Dismiss

DismissSignalCommandHandler               [Application — Handler]
  Responsibility: Load signal, verify ownership, call Dismiss(), save
  Depends on: ISignalRepository, ICurrentUser, IApplicationDbContext
  Key behaviour:
    - 404 if signal not found
    - 403 if signal.UserId != currentUser.UserId
    - Calls signal.Dismiss() (idempotent domain method)
    - SaveChangesAsync — EF change tracker picks up DismissedAt

SignalRepository (additions)              [Infrastructure — Repository]
  GetByIdAsync — FindAsync (EF identity-map cache, then DB)
  DeleteOldDismissedAsync — ExecuteDeleteAsync (bulk SQL DELETE, no entity load)
  GetLatestByUserAsync — add .Where(s => s.DismissedAt == null) predicate

SignalsController.Dismiss                 [API — Controller action]
  Responsibility: Route PATCH /{id}/dismiss to DismissSignalCommand via MediatR
  Returns 204 No Content

SignalCleanupJob                          [BackgroundJobs — Hangfire job]
  Responsibility: Daily hard-delete of dismissed signals older than 90 days
  Depends on: ISignalRepository
  Key behaviour:
    - cutoff = UtcNow - 90 days
    - Calls DeleteOldDismissedAsync(cutoff) — returns deleted count
    - Logs count at Information level; catches and logs unexpected exceptions
    - Does NOT call SaveChangesAsync (ExecuteDeleteAsync bypasses change tracker)
```

### 5.2 Interface Contracts

```
Signal.Dismiss() → void
  Sets DismissedAt = DateTime.UtcNow
  No-op if DismissedAt already has a value

ISignalRepository.GetByIdAsync(
  id: Guid,
  cancellationToken: CancellationToken
) → Task<Signal?>
  Returns null if not found

ISignalRepository.DeleteOldDismissedAsync(
  cutoff: DateTime,
  cancellationToken: CancellationToken
) → Task<int>
  Returns count of hard-deleted rows
  Does not throw on zero rows deleted

DismissSignalCommandHandler.Handle(
  request: DismissSignalCommand { Id: Guid },
  cancellationToken: CancellationToken
) → Task
  Errors:
    NotFoundException        (→ 404) — signal not found
    AuthorizationException   (→ 403) — signal belongs to different user

SignalCleanupJob.ExecuteAsync(
  cancellationToken: CancellationToken
) → Task
  Never throws — catches all exceptions and logs them
```

### 5.3 API Surface

```
PATCH /api/signals/{id}/dismiss
  Auth:     Bearer JWT, Role = User
  Request:  no body
  Response: 204 No Content
  Errors:
    401 — missing/invalid JWT
    403 — signal belongs to a different user
    404 — signal not found

GET /api/signals?count=N       (MODIFIED — now excludes dismissed)
  Auth:     Bearer JWT, Role = User
  Response: Signal[] with dismissedAt field (null for active)
```

### 5.4 Cross-Cutting Concerns

| Concern | Approach |
|---|---|
| Authentication / Authorization | `[Authorize(Roles = UserRole.User)]` class-level; ownership check in handler via `signal.UserId != currentUser.UserId` |
| Input validation | `id` is a route `Guid` — framework rejects malformed values with 400 before MediatR; no FluentValidation validator needed (single-field command with no business rules to validate) |
| Error handling & mapping | `NotFoundException` → 404, `AuthorizationException` → 403 via `ExceptionHandlingMiddleware` (existing, unchanged) |
| Logging | `LoggingBehavior` (MediatR pipeline) logs request name + elapsed time automatically; `SignalCleanupJob` logs deleted count + errors explicitly |
| Transactions / consistency | Single `SaveChangesAsync` in handler; `ExecuteDeleteAsync` in cleanup job issues its own transaction internally |
| Caching | N/A — no server-side cache for signals |
| Idempotency | `Signal.Dismiss()` is idempotent — second PATCH on same signal returns 204 with no state change |
| Rate limiting | N/A — existing rate limiting on the API applies; no special treatment needed |

---

## 6. Infrastructure Design

No new services, environment variables, or infrastructure changes required.
The cleanup job runs inside the existing Hangfire instance registered in `Program.cs`.

---

## 7. Security Considerations

- **Ownership check** — handler compares `signal.UserId` against `ICurrentUser.UserId`
  (resolved from JWT by `UserContextMiddleware`). Cross-user dismissal returns 403.
- **No mass-dismiss endpoint** — only single-signal dismiss is exposed. Bulk operations
  would require explicit design review.
- **No sensitive data in signal** — `Message`, `Symbol`, `Value` are market data, not PII.
  No special logging constraints.

---

## 8. Open Questions / Risks

> ℹ️ RESOLVED: `ExistsRecentAsync` in `MarketSignalJob` checks for signals within 24 hours
> regardless of `DismissedAt`. A dismissed signal still counts as "recent" for deduplication.
> Effect: if a user dismisses a Volume Spike signal, the job will NOT re-generate it for
> another 24 hours. **Decision: accept as intentional.** The signal was genuinely generated;
> the user chose to dismiss it. No code change needed.

---

## 9. Out of Scope

- Signal History page (view all dismissed signals)
- Undo dismiss
- Bulk dismiss ("clear all")
- Per-signal-type retention rules (all dismissed signals use the same 90-day window)
- Push notification on new signal (already handled by `IsNotified` / `MarkNotified()`)
