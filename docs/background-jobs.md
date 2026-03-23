# Background Jobs

FinTrackPro runs three Hangfire recurring jobs. All jobs are registered in `Program.cs` and use SQL Server as the Hangfire storage backend.

| Job | Schedule | Purpose |
|---|---|---|
| `MarketSignalJob` | Every 4 hours | RSI + volume spike signals for watched symbols |
| `BudgetOverrunJob` | Daily | Detect and alert on budget limit breaches |
| `IamUserSyncJob` | Daily | Deactivate local users deleted from IAM provider |

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
- Volume spike threshold: 2× the 6-day average daily volume
- Errors per symbol are caught and logged; job continues to next symbol

---

## BudgetOverrunJob

Runs daily. For each budget in the current month, sums the user's expenses in that category. If spending exceeds the limit and no alert has been sent yet this month, it sends one Telegram notification and records a marker `Signal` to prevent duplicate alerts.

```mermaid
sequenceDiagram
    participant Hangfire
    participant Job as BudgetOverrunJob
    participant BR as BudgetRepo
    participant TR as TransactionRepo
    participant SR as SignalRepo
    participant NS as NotificationService
    participant Telegram
    participant DB as DbContext

    Hangfire->>Job: Trigger (daily)
    Job->>BR: GetByMonthAsync(currentMonth) — all users
    BR-->>Job: List of Budget

    loop each Budget
        Job->>TR: SumExpensesAsync(userId, category, month)
        TR-->>Job: Total spent (decimal)

        alt spent > budget.LimitAmount
            Job->>SR: ExistsAsync(userId, "BUDGET:<category>", FundingRate, currentMonth)
            alt No marker exists (first breach this month)
                Job->>NS: NotifyAsync(userId, "Budget Overrun", overage message)
                NS->>Telegram: SendMessage(chatId, text)
                Job->>SR: Add(Signal) — marker with Symbol="BUDGET:<category>", Type=FundingRate
            end
        end
    end

    Job->>DB: SaveChangesAsync
```

**Key details:**
- Alert fires only once per `(UserId, Category, Month)` — first breach only
- Marker signal uses `SignalType.FundingRate` as a workaround (no dedicated `BudgetOverrun` type yet)
- `Symbol` field stores `"BUDGET:<category>"` to namespace budget markers from market signals

---

## IamUserSyncJob

Runs daily. Calls the active IAM provider's admin API to get the current list of active users, then deactivates any local `AppUser` rows whose `ExternalUserId` is no longer present. Includes a safety guard: if the IAM returns zero users (possible API error), no deactivations occur.

See the [auth-setup.md](auth-setup.md#nightly-iam-user-sync) sequence diagram for this job's flow.
