# FinTrackPro — Features

> Last updated: 2026-03-24

A personal finance and crypto trading companion. Track your spending, manage monthly budgets, journal your trades, and receive automated market alerts — all in one place.

---

## 1. Transaction Tracking

Record every inflow and outflow of money:

- **Income & expense entries** — log any transaction with an amount, category (e.g. "Food", "Salary"), an optional note, and the target budget month.
- **Monthly view** — filter your transaction history by any month to see exactly what you spent or earned in that period.
- **Search and filter** — search transactions by note or category name; filter by type (Income/Expense) and category; all server-side.
- **Sort** — sort the list by date (default), amount, or category with a single click; direction cycles desc → asc → reset.
- **Paginated list** — transactions load 20 per page by default (configurable: 10/20/50); pagination controls below the list.
- **Income vs. expense summary** — the Transactions page KPI cards show a running total of income, expenses, and net balance for the active filter state, sourced from a dedicated summary endpoint. Cards update when filters change and are unaffected by the current page or sort.
- **Delete transactions** — remove any entry you own.

---

## 2. Budget Management

Set spending limits per category per month and stay on track:

- **Create a budget** — define a spending cap for a category (e.g. "Food: $500") for a specific month.
- **Edit a budget** — adjust the limit amount at any time without losing your transaction history.
- **Progress bars** — each category shows how much of the limit has been consumed, colour-coded green (under 80%), yellow (80–100%), and red (over limit).
- **Overrun highlight** — budgets that have been exceeded are visually flagged so problem areas are immediately obvious.
- **Automated overrun alerts** — once a day the app totals your expenses per category for the current month and sends a Telegram notification the first time a budget is breached. The alert includes the category name, total spent, the original limit, and the exact overage (e.g. *"Budget overrun: 'Food' spent $612 of $500 limit ($112 over)."*). You will receive at most one alert per category per month.
- **Delete a budget** — remove a budget you no longer need.

---

## 3. Trading Journal

A structured log for crypto trades with automatic profit/loss calculation:

- **Log a trade** — record a trade (Open or Closed) with: symbol (e.g. `BTCUSDT`), direction (Long or Short), status (Open / Closed), entry price, exit price (required when Closed), optional current price (shown for Open positions), position size, trading fees, and optional notes.
- **Symbol validation** — the symbol is verified against Binance's live trading pairs before saving. Invalid or misspelled pairs are rejected immediately.
- **Automatic P&L** — profit or loss is calculated on every read as `(exitPrice − entryPrice) × positionSize − fees`. It is always computed from your raw inputs, so it is always accurate.
- **Search and filter** — search by symbol; filter by status (Open/Closed), direction (Long/Short), and date range; all server-side.
- **Sort** — sort the table by date (default), P&L, symbol, entry price, position size, or fees; direction cycles desc → asc → reset.
- **Paginated list** — trades load 20 per page by default (configurable: 10/20/50); pagination controls below the table.
- **Journal statistics** — the Trades page KPI cards show aggregate stats for the active filter state: total realised P&L, win rate (percentage of trades that were profitable), total trade count, and unrealised P&L across open positions. Stats are sourced from a dedicated summary endpoint and reflect the full filtered dataset — not just the current page.
- **Delete a trade** — remove any trade you own.

---

## 4. Crypto Watchlist *(Pro plan only)*

Maintain a personal list of symbols to monitor for market signals. Reading and managing the watchlist requires an active Pro subscription; Free users see an upsell overlay with a preview of what they are missing.

- **Add a symbol** — enter any Binance trading pair (e.g. `ETHUSDT`); it is validated against Binance and checked for duplicates before being added.
- **Remove a symbol** — remove a symbol you no longer want to track.
- **Drives signal generation** — the watchlist is the source of truth for which symbols the signal engine analyses. Only symbols on your watchlist will generate alerts for your account.
- **Pro gate** — Free and expired-Pro users receive HTTP 402 (`feature: "watchlist"`) on all watchlist read endpoints. The frontend renders a `ProFeatureLock` blurred overlay with an upgrade call-to-action in place of the real content.

---

## 5. Market Signals *(Pro plan only)*

An automated engine analyses your watchlist every 4 hours and notifies you of notable market conditions. Viewing signals requires an active Pro subscription; Free users see a compact teaser in the dashboard and a full upsell overlay on the Market page.

### RSI (Relative Strength Index) — Weekly timeframe

The engine fetches the last 14 weekly candles from Binance and computes a 14-period RSI:

- **RSI Oversold** — fires when RSI drops below 30. Example: *"RSI 28.5 — BTCUSDT is oversold. Consider DCA."* Suggests the asset may be undervalued relative to recent history and could be a buying opportunity.
- **RSI Overbought** — fires when RSI exceeds 70. Example: *"RSI 74.2 — BTCUSDT is overbought. Consider taking profit."* Suggests the asset may be overextended and a correction could follow.

### Volume Spike — Daily timeframe

The engine compares the current 24-hour trading volume against the average volume of the previous 6 daily candles:

- **Volume Spike** — fires when today's volume is 2× or more above that 6-day average. Example: *"Volume spike detected on ETHUSDT: 3.4x above 7-day average."* An unusual surge in volume often precedes or accompanies a significant price move.

### Deduplication

Each signal type (RSI Oversold, RSI Overbought, Volume Spike) is suppressed for 24 hours after it last fired for a given symbol. If BTCUSDT is oversold today, you will not receive the same alert for BTCUSDT again until tomorrow — preventing repeated notifications for the same condition.

### Viewing signals

All generated signals are stored in your account. Each entry shows the symbol, signal type, the computed value (e.g. the RSI reading or the volume ratio), the timeframe analysed, and the timestamp. You can retrieve up to 1,000 of your most recent signals.

### Dismissing signals

Each signal card has an ✕ button. Clicking it immediately removes the card from your feed (optimistic UI — no confirmation dialog needed for auto-generated data). The signal is soft-deleted (`DismissedAt` timestamp set) and excluded from subsequent fetches. If the server call fails the card reappears with an error toast. Dismissed signals are permanently deleted after 90 days by a daily cleanup job; active signals are never automatically deleted.

---

## 6. Market Data

Live market context available to all users:

- **Fear & Greed Index** — a sentiment indicator (0–100) reflecting the current emotional state of the crypto market. Values near 0 indicate extreme fear; values near 100 indicate extreme greed. Refreshed every hour. Displayed on the Market page (`/market`) with an animated gauge.
- **Trending coins** — the top 10 trending cryptocurrencies right now, including name, symbol, market cap rank, live price, and 1h/24h/7d % change. Refreshed every 2 minutes. Displayed on the Market page.
- **Top Market Cap** — top 10 cryptocurrencies ranked by global market cap (CoinGecko), with price and 1h/24h/7d % change. Market cap formatted as `$1.2T` / `$1.2B`. Refreshed every 2 minutes. Displayed on the Market page.
- **Watchlist RSI Analysis** — for each symbol in the user's watchlist: live price, 24h % change, RSI-14 on daily and weekly timeframes. RSI computed using Welles Wilder smoothing (matches TradingView). RSI < 30 shows a blue `OS` badge (oversold); RSI > 70 shows a red `OB` badge (overbought). Refreshed every 3 minutes. Empty state with Settings link when watchlist is empty. Each row includes a **Trade badge** — an amber pill button (Binance logo + "Trade {BASE}" on desktop, icon-only on mobile) linking to `https://www.binance.com/en/trade/{BASE}_{QUOTE}`; URL derived client-side, badge hidden for non-Binance symbols. Widget footer shows "via Binance" attribution.

---

## 7. Telegram Notifications

Receive alerts directly in Telegram without opening the app:

- **Setup** — enter your Telegram Chat ID in Settings to link your account.
- **Enable / disable** — toggle notifications on or off at any time without losing your Chat ID.
- **What triggers a notification** — budget overrun alerts (daily check) and market signals (RSI + volume, every 4 hours).

---

## 8. Authentication & User Management

- **Secure login** — all access requires authentication via an industry-standard JWT-based login flow.
- **Automatic account creation** — the first time you log in, your account is created automatically. No separate registration step required.
- **Role-based access** — two roles exist: standard user access and admin access.

---

## 9. Admin Subscription Management *(Admin role only)*

A dedicated tab inside the Settings page (`/settings?tab=admin`) that lets admins manage user subscriptions without direct database access. The tab is only visible to users with the Admin role.

- **User list** — paginated list of all registered users showing their email, display name, current plan (`Free` / `Pro`), and subscription expiry date. Filterable by partial email.
- **Activate Pro** — two buttons per user: `+1 month` and `+1 year`. Extending an already-active subscription stacks onto the existing expiry rather than starting from today, so a user is never penalised for a late-activation run.
- **Revoke Pro** — an inline confirmation chip must be clicked to confirm before the subscription is revoked. Revocation sets the user back to Free immediately.
- **Audit trail** — manually activated subscriptions are stamped with a `bank_` prefix on `PaymentSubscriptionId`, making them distinguishable from Stripe-issued activations in the database.
- **Use case** — bank-transfer payments: when a user pays via bank transfer, the admin activates Pro here instead of running a raw SQL update.

---

## Planned / Not Yet Built

| Feature | Notes |
|---|---|
| EMA Golden/Death Cross signals | Exponential moving average crossover detection |
| Bollinger Band Squeeze signals | Volatility compression detection |
| Funding Rate Sentiment signals | Perpetual contract funding rate alerts |
| Email notifications | Infrastructure is in place; implementation pending |
