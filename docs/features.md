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

- **Log a trade** — record a completed trade with: symbol (e.g. `BTCUSDT`), direction (Long or Short), entry price, exit price, position size, trading fees, and optional notes.
- **Symbol validation** — the symbol is verified against Binance's live trading pairs before saving. Invalid or misspelled pairs are rejected immediately.
- **Automatic P&L** — profit or loss is calculated on every read as `(exitPrice − entryPrice) × positionSize − fees`. It is always computed from your raw inputs, so it is always accurate.
- **Search and filter** — search by symbol; filter by status (Open/Closed), direction (Long/Short), and date range; all server-side.
- **Sort** — sort the table by date (default), P&L, symbol, entry price, position size, or fees; direction cycles desc → asc → reset.
- **Paginated list** — trades load 20 per page by default (configurable: 10/20/50); pagination controls below the table.
- **Journal statistics** — the Trades page KPI cards show aggregate stats for the active filter state: total realised P&L, win rate (percentage of trades that were profitable), total trade count, and unrealised P&L across open positions. Stats are sourced from a dedicated summary endpoint and reflect the full filtered dataset — not just the current page.
- **Delete a trade** — remove any trade you own.

---

## 4. Crypto Watchlist

Maintain a personal list of symbols to monitor for market signals:

- **Add a symbol** — enter any Binance trading pair (e.g. `ETHUSDT`); it is validated against Binance and checked for duplicates before being added.
- **Remove a symbol** — remove a symbol you no longer want to track.
- **Drives signal generation** — the watchlist is the source of truth for which symbols the signal engine analyses. Only symbols on your watchlist will generate alerts for your account.

---

## 5. Market Signals

An automated engine analyses your watchlist every 4 hours and notifies you of notable market conditions:

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

---

## 6. Market Data

Live market context available to all users:

- **Fear & Greed Index** — a sentiment indicator (0–100) reflecting the current emotional state of the crypto market. Values near 0 indicate extreme fear; values near 100 indicate extreme greed. Refreshed every hour. Displayed on the Dashboard with an animated gauge.
- **Trending coins** — the top 7 trending cryptocurrencies right now, including name, symbol, and market cap rank. Refreshed every 15 minutes. Displayed on the Dashboard.

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

## Planned / Not Yet Built

| Feature | Notes |
|---|---|
| EMA Golden/Death Cross signals | Exponential moving average crossover detection |
| Bollinger Band Squeeze signals | Volatility compression detection |
| Funding Rate Sentiment signals | Perpetual contract funding rate alerts |
| Email notifications | Infrastructure is in place; implementation pending |
