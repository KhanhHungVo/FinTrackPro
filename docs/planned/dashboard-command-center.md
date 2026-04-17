# Dashboard Redesign — Decision-Making Command Center

## Problem

The current dashboard shows a greeting, 3 KPI cards (income/expenses/trading P&L), a Fear & Greed gauge, trending coins from CoinGecko, and a signals list. Most of this content is generic market data that isn't personalized. Users with few trades or simple DCA strategies see little value beyond the KPI cards.

## Goal

Transform the dashboard into a personalized command center that:
- Surfaces the user's own financial data (allocation, trends, budget health)
- Works for both active traders and passive investors
- Provides value even with minimal data
- Moves generic market data to a dedicated Market page

## Design Principles

1. **Non-empty sections first** — Expense allocation and budget health render above trading widgets because they have data as soon as the user records any transaction or budget
2. **Conditional sections hide cleanly** — Trading performance and signals only render when the user has trades/watchlist symbols; no empty placeholders
3. **Market data moves to its own page** — Fear & Greed and Trending Coins get a dedicated `/market` route
4. **All data from existing endpoints** — No new backend endpoints required; client-side grouping from paginated lists (same pattern BudgetsPage already uses)
5. **Trading is the primary priority** — KPI header is rebalanced: Income + Expenses (finance) + Trading P&L + Unrealized P&L (trading). Net Savings is dropped in favour of exposing Unrealized P&L, which is a primary trading signal currently buried in the trading section

---

## Wireframe

### Desktop (lg breakpoint)

```
+============================================================================+
|  [FreePlanAdBanner — full width, dismissible]                              |
+============================================================================+

  Good morning, Vo                                                   
  Friday, April 17                                                   

+-- SECTION 1: KPI HEADER -----------------------------------------------+
|                                                                         |
|  +------------------+  +------------------+  +--------------------+  +------------------+
|  | INCOME · MONTH   |  | EXPENSES · MONTH |  | TRADING P&L · MONTH|  | UNREALIZED P&L   |
|  | |  d0            |  | |  d30,272,976   |  | |  +d4,987,035    |  | |  +d1,234,567   |
|  | |  green border  |  | |  red border    |  | |  blue border    |  | |  purple border |
|  | | +12% vs last   |  | | +8% vs last    |  | | -3% vs last     |  | | 3 open pos.   |
|  +------------------+  +------------------+  +--------------------+  +------------------+
|       ← finance →                                    ← trading →                       |
+-------------------------------------------------------------------------+

+-- SECTION 2: ALLOCATION & BUDGET HEALTH (side by side) -----------------+
|                                                                         |
|  +--- Expense Allocation ----------+  +--- Budget Health -------------+ |
|  |                                 |  |                               | |
|  |        +-----------+            |  |  3 of 5 on track             | |
|  |       /   DONUT     \           |  |                               | |
|  |      |    CHART      |          |  |  Food       [========--] 82% | |
|  |      |  d30.2M       |          |  |  Transport  [=====-----] 55% | |
|  |       \  total      /           |  |  Shopping   [==========] 105%| |
|  |        +-----------+            |  |  Utilities  [===-------] 30% | |
|  |                                 |  |  Health     [==--------] 20% | |
|  |  * Food         d12.5M  41%    |  |                               | |
|  |  * Transport    d6.2M   20%    |  |  View all budgets ->          | |
|  |  * Shopping     d4.8M   16%    |  |                               | |
|  |  * Utilities    d3.1M   10%    |  +-------------------------------+ |
|  |  * Other        d3.6M   13%    |                                    |
|  |                                 |                                    |
|  +---------------------------------+                                    |
|                                                                         |
+-------------------------------------------------------------------------+

+-- SECTION 3: TRADING INTELLIGENCE (hidden if 0 trades) -----------------+
|                                                                         |
|  Trading Intelligence                                                   |
|                                                                         |
|  ┌─ OPEN POSITIONS · Live snapshot ─────────────────────────────────┐  |
|  │                                                                   │  |
|  │  +--------------------+  +--------------------+                  │  |
|  │  | UNREALIZED P&L     |  | OPEN POSITIONS     |                  │  |
|  │  | +d1,234,567        |  | 3                  |                  │  |
|  │  +--------------------+  +--------------------+                  │  |
|  │                                                                   │  |
|  │  Capital Allocation                                               │  |
|  │                                                                   │  |
|  │     +---------+    Winning (2)              Losing (1)           │  |
|  │    / DONUT    \    ● BTC  +d820K  +6.2%  ● ETH  -d310K  -12.4% │  |
|  │   |  d97.6M   |        42% of portfolio       25% of portfolio  │  |
|  │   |  deployed |                                                   │  |
|  │    \_________/    ● SOL  +d210K  +4.8%                          │  |
|  │                        11% of portfolio                           │  |
|  │                                                                   │  |
|  │  ⚠  ETH is -12.4% and holds 25% of your portfolio               │  |
|  │  ⚠  BTC accounts for 42% of total exposure                       │  |
|  │                                                                   │  |
|  └───────────────────────────────────────────────────────────────────┘  |
|                                                                         |
|  ┌─ CLOSED TRADES · This month ─────────────────────────────────────┐  |
|  │                                                                   │  |
|  │  +------------------+  +------------------+  +-----------------+ │  |
|  │  | REALISED P&L     |  | WIN RATE         |  | AVG P&L / TRADE | │  |
|  │  | +d4,987,035      |  | 67%  (8/12)      |  | +d415,586       | │  |
|  │  +------------------+  +------------------+  +-----------------+ │  |
|  │                                                                   │  |
|  │  Cumulative P&L · This month                                      │  |
|  │  +d5M ╮                                                          │  |
|  │  +d4M  ╰─╮                                                       │  |
|  │  +d3M    ╰──╮   ╭─────╮                                          │  |
|  │  +d2M       ╰───╯     ╰──╮                                       │  |
|  │  +d1M                    ╰──────                                  │  |
|  │       W1     W2     W3     W4                                     │  |
|  │                                                                   │  |
|  │  By Symbol    BTC +d3.1M ██████  ETH +d1.2M ████  SOL -d0.3M ▓  │  |
|  │  By Direction Long  +d5.4M ███████   Short  -d0.4M ▓             │  |
|  │                                                                   │  |
|  │  Avg win +d680K   Avg loss -d290K   R:R  2.3                     │  |
|  │                                                                   │  |
|  └───────────────────────────────────────────────────────────────────┘  |
|                                                                         |
+-------------------------------------------------------------------------+

+-- SECTION 4: RECENT ACTIVITY -------------------------------------------+
|                                                                         |
|  Recent Activity                                                        |
|                                                                         |
|  | Food         -d120,000   Expense      2 hours ago                   |
|  | BTCUSDT      +d450,000   Long/Closed  5 hours ago                   |
|  | Salary       +d25,000,000 Income      1 day ago                     |
|  | Transport    -d85,000    Expense      1 day ago                     |
|  | ETHUSDT      Open        Long         2 days ago                    |
|  | Shopping     -d350,000   Expense      2 days ago                    |
|                                                                         |
+-------------------------------------------------------------------------+

+-- SECTION 5: CONTEXTUAL SIGNALS (hidden if watchlist empty) ------------+
|                                                                         |
|  Signals for Your Watchlist              Manage watchlist ->            |
|                                                                         |
|  [RSI Oversold]  BTCUSDT  RSI dropped below 30 on 4h     3h ago       |
|  [Volume Spike]  ETHUSDT  Volume 3.2x average on 1h      6h ago       |
|  [EMA Cross]     SOLUSDT  Bullish EMA cross on 4h        12h ago      |
|                                                                         |
+-------------------------------------------------------------------------+
```

### Mobile (sm breakpoint)

```
+======================================+
| [FreePlanAdBanner]                   |
+======================================+

  Good morning, Vo
  Friday, April 17

+-- KPI HEADER (2x2 grid) -----------+
|  +----------------+  +------------+|
|  | INCOME · MONTH |  | EXPENSES   ||
|  | d0             |  | d30.2M     ||
|  +----------------+  +------------+|
|  +----------------+  +------------+|
|  | TRADING P&L   |  | UNREALIZED ||
|  | +d4.9M · month|  | +d1.2M     ||
|  +----------------+  +------------+|
+------------------------------------+

+-- Expense Allocation ---------------+
|        +-------+                    |
|       / DONUT  \                    |
|      |  d30.2M  |                   |
|       \ total  /                    |
|        +-------+                    |
|  * Food       d12.5M   41%         |
|  * Transport  d6.2M    20%         |
|  * Shopping   d4.8M    16%         |
|  * Other      d6.7M    23%         |
+------------------------------------+

+-- Budget Health --------------------+
|  3 of 5 on track                   |
|  Food      [========--] 82%        |
|  Transport [=====-----] 55%        |
|  Shopping  [==========] 105%       |
|  View all budgets ->               |
+------------------------------------+

+-- Trading Intelligence -------------+
|  ── Open Positions · Live ──        |
|  +---------------+  +------------+ |
|  | UNREALIZED    |  | OPEN POS.  | |
|  | +d1.2M        |  | 3          | |
|  +---------------+  +------------+ |
|                                     |
|       +-------+                     |
|      / DONUT  \  Winning (2)       |
|     | d97.6M  |  ● BTC +6.2%  42% |
|      \_______/   ● SOL +4.8%  11% |
|                  Losing (1)        |
|                  ● ETH -12.4% 25%  |
|                                     |
|  ⚠ ETH -12.4% · 25% of portfolio  |
|                                     |
|  ── Closed Trades · This month ──   |
|  +--------+ +--------+ +---------+ |
|  |+d4.9M  | | 67% WR | | +d416K  | |
|  +--------+ +--------+ +---------+ |
|                                     |
|  BTC +d3.1M ██████                 |
|  ETH +d1.2M ████                   |
|  SOL -d0.3M ▓                      |
|                                     |
|  Avg win +d680K  Avg loss -d290K   |
|  R:R 2.3                           |
+------------------------------------+

+-- Recent Activity ------------------+
|  Food       -d120K    2h ago       |
|  BTCUSDT    +d450K    5h ago       |
|  Salary     +d25M     1d ago       |
|  Transport  -d85K     1d ago       |
+------------------------------------+

+-- Signals --------------------------+
|  [RSI] BTCUSDT  RSI < 30    3h    |
|  [VOL] ETHUSDT  3.2x vol    6h    |
+------------------------------------+
```

### Market Page (`/market`)

```
+============================================================================+

  Market                                                               

+-- Two-column grid --+---------------------------------------------------+
|                      |                                                    |
|  Fear & Greed Index  |  Trending Coins                          [LIVE]   |
|                      |                                                    |
|     +---------+      |  RANK  NAME              SYMBOL                   |
|    / GAUGE    \      |  #24   RaveDAO           RAVE                     |
|   |    21     |      |  #186  ORDI              ORDI                     |
|    \_________/       |  #107  Pudgy Penguins    PENGU                    |
|   EXTREME FEAR       |  #63   Siren             SIREN                    |
|                      |  #164  Genius            GENIUS                   |
|                      |  #528  Based             BASED                    |
|                      |  #7    Solana            SOL                      |
+----------------------+                        Powered by CoinGecko      |
                       +---------------------------------------------------+

+-- Recent Signals (full list) -------------------------------------------+
|                                                                         |
|  [RSI Oversold]   BTCUSDT  RSI dropped below 30 on 4h        3h ago   |
|  [Volume Spike]   ETHUSDT  Volume 3.2x average on 1h         6h ago   |
|  [EMA Cross]      SOLUSDT  Bullish EMA cross on 4h          12h ago   |
|  [BB Squeeze]     BTCUSDT  Bollinger Band squeeze on 1d       1d ago  |
|  ...up to 20 signals...                                                |
|                                                                         |
|  No signals yet -- add symbols to your watchlist.                      |
+-------------------------------------------------------------------------+
```

---

## Section Details

### Section 1: KPI Header (enhanced)

| Card | Label | Period | Data Source | Color | Subtitle |
|------|-------|--------|-------------|-------|---------|
| Income | `INCOME · THIS MONTH` | Current month | `useTransactionSummary({ month })` | green | delta vs last month |
| Expenses | `EXPENSES · THIS MONTH` | Current month | `useTransactionSummary({ month })` | red | delta vs last month |
| Trading P&L | `TRADING P&L · THIS MONTH` | Current month (closed trades) | `useTradesSummary({ status: 'Closed', dateFrom, dateTo })` | blue | delta vs last month |
| Unrealized P&L | `UNREALIZED P&L` | All open positions (no period filter) | `useTradesSummary({ status: 'Open' })` | purple | "N open positions" |

Income, Expenses, and Trading P&L show month-over-month delta badges. Unrealized P&L shows open position count as subtitle — it's a live snapshot, not a trend, so no delta. Grid: `sm:grid-cols-2 lg:grid-cols-4`.

### Section 2a: Expense Allocation (donut chart)

- **Data:** `useTransactions({ month, type: 'Expense', pageSize: 200 })` grouped by category
- **Chart:** Recharts `<PieChart>` with `<Pie innerRadius outerRadius>` (donut style)
- **Center label:** Total expenses formatted in preferred currency
- **Legend:** Category icon + name + amount + percentage
- **Colors:** Deterministic palette (10-12 colors, assigned by index)
- **Empty state:** "No expenses this month" + link to /transactions

### Section 2b: Budget Health (progress bars)

- **Data:** `useBudgets(currentMonth)` + expense transactions (React Query deduplicates with 2a)
- **UI:** Compact progress bars, sorted worst-first, max 5 shown
- **Colors:** Green (< 60%), Yellow (60-90%), Red (> 90%)
- **Summary:** "X of Y on track" header line
- **Empty state:** "No budgets set" + link to /budgets

### Section 3: Trading Intelligence (conditional)

- **Visibility:** Hidden entirely when `totalTrades === 0`

#### 3a — Open Positions (live snapshot, no period filter)

- **Data:** `useTrades({ status: 'Open', pageSize: 100 })` — current open positions only
- **Metrics (2 cards):** Unrealized P&L (sum across open positions), Open position count
- **Capital Allocation donut:** Recharts `<PieChart>` — each slice = one symbol, sized by capital deployed (entry price × quantity). Center label = total capital deployed.
- **P&L distribution list:** Positions split into Winning / Losing groups, sorted by absolute P&L desc. Each row: symbol, unrealized P&L (absolute + %), portfolio weight (position size ÷ total deployed). Max 3 rows per group; overflow hidden behind "View all → /trades".
- **Risk signals (max 2):** Derived client-side from the open positions list:
  - Largest loser with weight > 20% → "X is −Y% and holds Z% of your portfolio"
  - Any single symbol with weight > 50% → "X accounts for Z% of total exposure"
  - If neither threshold is met, signals row is hidden (no placeholder text)

#### 3b — Closed Trades (current month)

- **Data:** `useTradesSummary({ status: 'Closed', dateFrom: startOfMonth, dateTo: today })` + `useTrades({ status: 'Closed', dateFrom, dateTo, pageSize: 100 })`
- **Period:** Current calendar month (fixed — no toggle; extended history deferred to a future Pro-tier feature)
- **Metrics (3 cards):** Realised P&L, Win Rate (wins ÷ total closed this month, shown as % and fraction), Avg P&L per trade
- **Cumulative P&L line chart:** Recharts `<LineChart>` — X axis = week buckets within the month (W1–W4), Y axis = running cumulative realised P&L. Gives trend shape without per-day granularity.
- **Edge breakdown (2 rows, text + mini bar):** By Symbol (top 3 by absolute P&L); By Direction (Long vs Short). Values are realised P&L totals for the month.
- **Behavior metrics (1 row):** Avg win, Avg loss, Risk/Reward ratio — all derived client-side from the closed trades list.
- **Empty state:** "No closed trades this month" — section still renders so the Open Positions panel above remains visible.

### Section 4: Recent Activity (unified feed)

- **Data:** Latest 5 transactions + 5 trades, merged and sorted by `createdAt` desc, top 8
- **UI:** Compact list with left-border color coding (green=income, red=expense, blue=trade)
- **Each row:** Icon/symbol, label, amount, relative timestamp
- **Empty state:** "No recent activity" + links to /transactions and /trades

### Section 5: Contextual Signals (conditional)

- **Visibility:** Hidden when `useWatchedSymbols()` returns empty array
- **UI:** Existing `SignalsList` widget wrapped with section title + "Manage watchlist" link
- **Max items:** 5 signals (reduced from 20; full list on Market page)

---

## New Files (18)

```
src/pages/market/index.ts
src/pages/market/ui/MarketPage.tsx

src/widgets/expense-allocation/index.ts
src/widgets/expense-allocation/ui/ExpenseAllocationWidget.tsx
src/widgets/expense-allocation/lib/useExpensesByCategory.ts

src/widgets/budget-health/index.ts
src/widgets/budget-health/ui/BudgetHealthWidget.tsx
src/widgets/budget-health/lib/useBudgetHealth.ts

src/widgets/trading-intelligence/index.ts
src/widgets/trading-intelligence/ui/TradingIntelligenceWidget.tsx
src/widgets/trading-intelligence/ui/OpenPositionsPanel.tsx      -- 3a: donut + P&L list + risk signals
src/widgets/trading-intelligence/ui/ClosedTradesPanel.tsx       -- 3b: metrics + chart + edge breakdown
src/widgets/trading-intelligence/ui/AllocationDonut.tsx
src/widgets/trading-intelligence/ui/RiskSignals.tsx
src/widgets/trading-intelligence/lib/useOpenPositions.ts        -- derives allocation, P&L groups, risk signals
src/widgets/trading-intelligence/lib/useClosedTradesSummary.ts  -- current-month closed trades + behavior metrics

src/widgets/recent-activity/index.ts
src/widgets/recent-activity/ui/RecentActivityWidget.tsx
src/widgets/recent-activity/ui/ActivityItem.tsx
src/widgets/recent-activity/lib/useMergedActivity.ts

src/widgets/contextual-signals/index.ts
src/widgets/contextual-signals/ui/ContextualSignalsWidget.tsx

src/shared/lib/resolveCategoryLabel.ts
```

## Modified Files (6)

```
src/pages/dashboard/ui/DashboardPage.tsx      -- new layout composition
src/widgets/kpi-summary/ui/KpiSummaryWidget.tsx -- add Net Savings card, 4-col grid
src/app/App.tsx                                -- add /market route
src/widgets/navbar/ui/Navbar.tsx               -- add Market nav link
src/shared/i18n/en.ts                          -- new translation keys
src/shared/i18n/vi.ts                          -- new translation keys
```

## Implementation Phases

| Phase | Scope | Dependencies |
|-------|-------|-------------|
| 1 | Market page + strip old widgets from dashboard | None |
| 2 | Expense Allocation + Budget Health + KPI 4th card | Phase 1 |
| 3 | Trading Performance + Recent Activity + Contextual Signals | Phase 1 |
| 4 | i18n, dark mode polish, empty state testing | Phase 2 + 3 |

## Future Backend Enhancement (separate task)

`GET /api/transactions/summary/by-category?month=X&type=Expense` — server-side category grouping to avoid fetching 200+ transactions for the donut chart. Not blocking; the client-side approach works for MVP.
