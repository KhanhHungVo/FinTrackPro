# Background Jobs

FinTrackPro runs four active Hangfire recurring jobs plus one hosted-service warm-up. All active jobs are registered in `Program.cs`. Hangfire storage uses the active database provider — PostgreSQL (`Hangfire.PostgreSql`) in production on Render, SQL Server (`Hangfire.SqlServer`) for local Docker dev.

| Job | Schedule | Status | Purpose |
|---|---|---|---|
| `MarketSignalJob` | Every 4 hours | Active | RSI + volume spike signals for watched symbols |
| `BudgetOverrunJob` | Daily | Active | Detect and alert on budget limit breaches (USD-normalised) |
| `ExchangeRateSyncJob` | Every 8 hours + startup | Active | Warm and refresh in-memory exchange rate cache |
| `SignalCleanupJob` | Daily | Active | Hard-delete dismissed signals older than 90 days |
| `IamUserSyncJob` | Daily | **Commented out** | Deactivate local users deleted from IAM provider |

The Hangfire dashboard is available at `/hangfire` (requires `Admin` role).

---

## MarketSignalJob

Runs every 4 hours. Iterates all `WatchedSymbol` rows across all users, computes RSI via Skender and checks for volume spikes via Binance, then sends Telegram notifications on first detection (24h dedup).

```mermaid
sequenceDiagram
    participant Hangfire
    participant Job as MarketSignalJob
    participant WR as WatchedSymbolRepo
    participant Binance as BinanceService
    participant Skender
    participant SR as SignalRepo
    participant NS as NotificationService
    participant Telegram
    participant DB as DbContext

    Hangfire->>Job: Trigger (every 4h)
    Job->>WR: GetAllAsync() — all users' watched symbols
    WR-->>Job: List of WatchedSymbol

    loop each WatchedSymbol
        Job->>Binance: GetKlinesAsync(symbol, "1w", 14)
        Binance-->>Job: 14 weekly candles
        Job->>Skender: GetRsi(14) on close prices
        Skender-->>Job: RSI value

        alt RSI < 30 (oversold) or RSI > 70 (overbought)
            Job->>SR: ExistsRecentAsync(userId, symbol, signalType, 24h)
            alt Not yet notified in last 24h
                Job->>SR: Add(Signal) — RSI signal
                Job->>NS: NotifyAsync(userId, title, message)
                NS->>Telegram: SendMessage(chatId, text)
            end
        end

        Job->>Binance: Get24HrTickerAsync(symbol)
        Binance-->>Job: 24h volume ticker
        Job->>Binance: GetKlinesAsync(symbol, "1d", 7)
        Binance-->>Job: 7 daily candles

        alt Current volume > 2× 7-day average
            Job->>SR: ExistsRecentAsync(userId, symbol, VolumeSpike, 24h)
            alt Not yet notified in last 24h
                Job->>SR: Add(Signal) — VolumeSpike signal
                Job->>NS: NotifyAsync(userId, title, message)
                NS->>Telegram: SendMessage(chatId, text)
            end
        end
    end

    Job->>DB: SaveChangesAsync
```

**Key details:**
- Signal dedup window: 24 hours per `(UserId, Symbol, SignalType)` triple
- RSI computed on 14 weekly candles (1W timeframe)
- Volume spike threshold: 2× the 7-day average daily volume (8 klines fetched; today's open candle excluded from baseline via `Take(7)`; today's live volume comes from the 24h ticker)
- Errors per symbol are caught and logged; job continues to next symbol

---

## BudgetOverrunJob

Runs daily. For each budget in the current month, loads the user's expenses in that category and normalises all amounts to USD using each record's stored `RateToUsd`. If USD-normalised spending exceeds the USD-normalised limit and no alert has been sent yet this month, it sends one Telegram notification (formatted in the budget's own currency) and inserts a `BudgetAlertLog` row to prevent duplicate alerts.

```mermaid
sequenceDiagram
    participant Hangfire
    participant Job as BudgetOverrunJob
    participant BR as BudgetRepo
    participant TR as TransactionRepo
    participant AL as BudgetAlertLogs (DbContext)
    participant NS as NotificationService
    participant Telegram
    participant DB as DbContext

    Hangfire->>Job: Trigger (daily)
    Job->>BR: GetByMonthAsync(currentMonth) — all users
    BR-->>Job: List of Budget

    loop each Budget
        Job->>TR: GetExpensesAsync(userId, category, month)
        TR-->>Job: List of Transaction (with Currency, RateToUsd)

        Note over Job: budgetInUsd = LimitAmount / RateToUsd
        Note over Job: spentInUsd = Sum(t.Amount / t.RateToUsd)

        alt spentInUsd > budgetInUsd
            Job->>AL: AnyAsync(userId, category, currentMonth)
            alt No log row exists (first breach this month)
                Job->>NS: NotifyAsync(userId, "Budget Alert", message in budget.Currency)
                NS->>Telegram: SendMessage(chatId, text)
                Job->>AL: Add(BudgetAlertLog) — dedup marker
                Job->>DB: SaveChangesAsync
            end
        end
    end
```

**Key details:**
- All comparisons are in USD using stored `RateToUsd` — avoids live rate calls and is consistent with historical data
- Alert fires only once per `(UserId, Category, Month)` — first breach only, enforced by unique index on `BudgetAlertLogs`
- Notification message formats amounts back in `budget.Currency` for human readability
- Dedup uses the dedicated `BudgetAlertLogs` table — the `Signals` table contains only market signals

---

## ExchangeRateSyncJob

Implements `IHostedService` (startup warm-up) and is also scheduled as a Hangfire recurring job (every 8 hours). Fetches rates for the currencies listed in `ExchangeRate:PreloadCurrencies` and stores them in `IMemoryCache` with an 8-hour TTL.

**Key details:**
- USD is short-circuited: returns `1.0m` without a network call
- Per-currency errors are caught and logged; other currencies continue
- Registered as both `AddScoped<ExchangeRateSyncJob>()` and `AddHostedService<ExchangeRateSyncJob>()` so it runs at startup and via Hangfire

---

## SignalCleanupJob

Runs daily. Hard-deletes all `Signals` rows where `DismissedAt IS NOT NULL AND DismissedAt < UtcNow - 90 days`, using a single bulk `ExecuteDeleteAsync` (no entity loading, no `SaveChangesAsync`). Logs the deleted row count at `Information` level. All exceptions are caught and logged — the job never throws.

**Key details:**
- 90-day retention window; active signals (never dismissed) are never deleted
- Uses `ISignalRepository.DeleteOldDismissedAsync(cutoff)` — bulk SQL DELETE via EF `ExecuteDeleteAsync`, bypasses the change tracker
- Served by the partial index `IX_Signals_DismissedAt WHERE DismissedAt IS NOT NULL` for an efficient cross-user sweep

---

## IamUserSyncJob

Runs daily. Calls the active IAM provider's admin API to get the current list of active users, then deactivates any local `AppUser` rows whose `ExternalUserId` is no longer present. Includes a safety guard: if the IAM returns zero users (possible API error), no deactivations occur.

See the [auth-setup.md](auth-setup.md#nightly-iam-user-sync) sequence diagram for this job's flow.
