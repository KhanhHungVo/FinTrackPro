# FinTrackPro — UI Flows

> Last updated: 2026-03-24
> Format: screen-by-screen reference for UI design and Figma workflow generation.

---

## Navigation Flow

```
[Login (IAM provider)] ──────────────────────────► [Dashboard /]
                                                          │
                        [Persistent Navbar] ──────────────┤
                          ├── /dashboard                  │
                          ├── /transactions                │
                          ├── /budgets                     │
                          ├── /trades                      │
                          └── /settings                    │
                                                           ▼
                          Dashboard → Transactions → Budgets → Trades → Settings
```

---

## Screen: Navbar (Persistent)

**Present on:** all authenticated pages

### Layout
- Left: Logo ("FinTrackPro")
- Centre: Nav links — Dashboard, Transactions, Budgets, Trades, Settings
- Right: User avatar (first initial of name), hover reveals name + email + Logout button

### States
| State | Description |
|---|---|
| Active link | Blue background, white text |
| Inactive link | Gray text, hover darkens |
| User menu closed | Avatar circle only |
| User menu open | Dropdown with name, email, Logout button |

### User Actions
| Action | Result |
|---|---|
| Click nav link | Navigate to corresponding page |
| Click avatar | Open user menu |
| Click Logout | End session, redirect to IAM login |

---

## Screen: Login

**Route:** managed by IAM provider (Keycloak or Auth0), not an in-app page

### Flow
1. Unauthenticated user visits any route → redirected to IAM provider login page
2. User enters credentials
3. On success → redirected back to `/dashboard`
4. If first login → account is automatically created in the background

### States
| State | Description |
|---|---|
| Unauthenticated | Redirect to IAM |
| Auth error | IAM provider shows error (invalid credentials, etc.) |
| First login | Transparent auto-provisioning, lands on Dashboard |

---

## Screen: Dashboard

**Route:** `/dashboard` (default after login)
**Purpose:** High-level overview of the user's financial health and market context for the current month.

### Layout
```
┌─────────────────────────────────────────────────────┐
│  Income (month)   │  Expenses (month)  │  Trading P&L │  ← Summary cards
├───────────────────┴────────────────────┴─────────────┤
│  Fear & Greed Gauge (left)  │  Trending Coins (right) │  ← Market widgets
├─────────────────────────────────────────────────────┤
│  Recent Signals (full width)                         │  ← Signals list
└─────────────────────────────────────────────────────┘
```

### Regions

**Summary Cards (3-column grid)**
- Income this month — green, formatted as currency
- Expenses this month — red, formatted as currency
- Trading P&L — green if positive, red if negative

**Fear & Greed Widget**
- SVG semicircle gauge with animated needle
- 5 colour zones: Extreme Fear (0–20, red) / Fear (20–40, orange) / Neutral (40–60, yellow) / Greed (60–80, green) / Extreme Greed (80–100, dark green)
- Numeric value displayed below gauge
- Data refreshed every hour

**Trending Coins**
- List of top 7 trending coins: coin name, symbol, market cap rank
- Data refreshed every 15 minutes

**Recent Signals**
- List of latest market signals across all watched symbols
- Each row: signal type badge (colour-coded) + symbol + message preview + timestamp
- Signal type badge colours: RsiOversold=red, RsiOverbought=orange, VolumeSpike=blue

### States
| State | Description |
|---|---|
| Loading | Animated skeleton placeholders for each region |
| Loaded | All data displayed |
| No signals | "No signals yet — add symbols to your watchlist." |

### User Actions
None — Dashboard is read-only.

### Navigates To
- Any page via Navbar

---

## Screen: Transactions

**Route:** `/transactions`
**Purpose:** Log, browse, and delete income and expense transactions filtered by month.

### Layout
```
┌─────────────────────────────────────────────────────┐
│  Transactions          [Month selector ▼]            │  ← Header
├──────────────┬─────────────┬────────────────────────┤
│  Income      │  Expenses   │  Net                   │  ← Summary cards
├─────────────────────────────────────────────────────┤
│  Add Transaction Form                                │  ← Inline form
├─────────────────────────────────────────────────────┤
│  Transaction list                                    │  ← List
│   [badge] Category  Note       Amount   Date    [✕]  │
│   ...                                                │
└─────────────────────────────────────────────────────┘
```

### Regions

**Month Selector**
- Dropdown listing the last 6 months (e.g. "2026-03", "2026-02", …)
- Filters all data on the page to the selected month

**Summary Cards (3-column grid)**
- Income: sum of all Income transactions for selected month (green, +$X.XX)
- Expenses: sum of all Expense transactions (red, -$X.XX)
- Net: Income − Expenses (green if positive, red if negative)

**Add Transaction Form**
- Type toggle: "Income" (green) / "Expense" (red) — one active at a time
- Amount field (number, required, > 0)
- Category field (text, required)
- Note field (text, optional)
- Submit button: "Add" → "Saving..." while pending → clears form on success
- Month is taken from the current month selector selection

**Transaction List**
- One row per transaction, sorted newest first
- Row contains: type badge, category name, note (gray subtext), amount (green/red), date, delete button (✕)

### States
| State | Description |
|---|---|
| Loading | 4 animated skeleton rows |
| Empty | "No transactions for [month]." |
| Loaded | List of transaction rows |
| Form pending | Submit button disabled, shows "Saving..." |

### User Actions
| Action | Result |
|---|---|
| Change month | Reload transactions and summary for selected month |
| Toggle Income/Expense | Sets transaction type on form |
| Fill and submit form | Creates transaction, refreshes list and summary |
| Click ✕ on a row | Deletes that transaction, refreshes list and summary |

---

## Screen: Budgets

**Route:** `/budgets`
**Purpose:** Set and manage per-category spending limits for a month; track progress and overruns.

### Layout
```
┌─────────────────────────────────────────────────────┐
│  Budgets                   [Month selector ▼]        │  ← Header
├─────────────────────────────────────────────────────┤
│  Category [___________]  Limit [$] [_____]  [Add]   │  ← Add Budget form
├─────────────────────────────────────────────────────┤
│  Budget cards (vertical stack)                       │
│  ┌──────────────────────────────────────────────┐   │
│  │  Food                           [✎]  [×]     │   │
│  │  $150.00 / $500.00                            │   │
│  │  ████████░░░░░░░░░░░░  30%                   │   │
│  └──────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────┐   │
│  │  Transport  ← OVERRUN               [✎]  [×] │   │
│  │  $320.00 / $300.00                            │   │
│  │  ████████████████████  107% (red)             │   │
│  │  Over budget by $20.00  (red text)            │   │
│  └──────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
```

### Regions

**Month Selector** — same pattern as Transactions page

**Add Budget Form (horizontal)**
- Category field (text, placeholder "e.g. Food")
- Limit field (number, placeholder "500")
- Add button → "..." while pending

**Budget Cards**
- Category name
- Spent / Limit amounts (e.g. "$150.00 / $500.00")
- Progress bar: green (< 80%), yellow (80–100%), red (> 100%)
- Overrun state: red bar, red "Over budget by $X.XX" label
- Edit button (✎) → switches amount to inline input; saves on blur or Enter, cancels on Escape
- Delete button (×)
- Spending is auto-calculated from Expense transactions in the same month and category

### States
| State | Description |
|---|---|
| Loading | 3 animated skeleton boxes |
| Empty | "No budgets set for [month]." |
| Loaded | List of budget cards |
| Edit mode (per card) | Limit amount becomes an editable input |
| Overrun | Red progress bar + red overage text |
| Form pending | Add button disabled, shows "..." |

### User Actions
| Action | Result |
|---|---|
| Change month | Reload budgets and recalculate spending |
| Fill and submit Add form | Creates budget, refreshes list |
| Click ✎ on a card | Enter inline edit mode for that card's limit |
| Blur / press Enter in edit | Save new limit, exit edit mode |
| Press Escape in edit | Cancel edit, restore original value |
| Click × on a card | Delete budget, refreshes list |

---

## Screen: Trades

**Route:** `/trades`
**Purpose:** Log and review crypto trades with automatic P&L calculation and aggregate statistics.

### Layout
```
┌─────────────────────────────────────────────────────┐
│  Trades                          [+ Log Trade]       │  ← Header
├──────────────┬─────────────┬────────────────────────┤
│  Total P&L   │  Win Rate   │  Total Trades           │  ← Stats bar
├─────────────────────────────────────────────────────┤
│  [AddTradeForm — visible only when toggled]          │  ← Collapsible form
├─────────────────────────────────────────────────────┤
│  Trades table (scrollable)                           │
│  Symbol │ Dir │ Entry │ Exit │ Size │ Fees │ P&L │ … │
└─────────────────────────────────────────────────────┘
```

### Regions

**Stats Bar (3-column grid)**
- Total P&L: sum of all trade results (green/red)
- Win Rate: % of trades where P&L > 0
- Total Trades: count

**Add Trade Form (collapsible)**
- Visible when "+ Log Trade" is clicked; hidden by default
- Direction toggle: Long (green) / Short (red)
- Symbol field (monospace, auto-uppercase, required — validated against Binance)
- Entry price (number, required)
- Exit price (number, required)
- Position size (number, required)
- Fees (number, optional, default 0)
- Notes (text, optional)
- Error message area (red, shown if symbol is invalid or validation fails)
- Submit: "Log Trade" → "Saving..." while pending → form clears and collapses on success

**Trades Table**
- Columns: Symbol, Direction (Long=green badge / Short=red badge), Entry Price, Exit Price, Position Size, Fees, P&L (green/red, bold), Date, Delete (✕)
- P&L formula: `(exitPrice − entryPrice) × positionSize − fees`

### States
| State | Description |
|---|---|
| Loading | 4 animated skeleton rows |
| Empty | "No trades logged yet. Click 'Log Trade' to get started." |
| Loaded | Stats bar + table |
| Form hidden | Default — only stats + table visible |
| Form visible | Form shown below stats bar |
| Form pending | Submit button disabled, shows "Saving..." |
| Form error | Red error message below form fields |

### User Actions
| Action | Result |
|---|---|
| Click "+ Log Trade" | Toggle form visibility |
| Toggle Long/Short | Sets trade direction |
| Fill and submit form | Creates trade, refreshes table and stats |
| Click ✕ on a row | Deletes that trade, refreshes table and stats |

---

## Screen: Settings

**Route:** `/settings`
**Purpose:** Configure Telegram notifications and manage the crypto symbol watchlist.

### Layout
```
┌─────────────────────────────────────────────────────┐
│  Settings                                            │
├─────────────────────────────────────────────────────┤
│  Notifications                                       │  ← Section heading
│  ┌──────────────────────────────────────────────┐   │
│  │  Telegram Notifications                       │   │
│  │  [info box: how to get Chat ID]               │   │
│  │  Telegram Chat ID  [________________]         │   │
│  │  [✓] Enable notifications                     │   │
│  │  [Save Preferences]                           │   │
│  │  ✓ Preferences saved.  (on success)           │   │
│  └──────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────┤
│  Signal Watchlist                                    │  ← Section heading
│  ┌──────────────────────────────────────────────┐   │
│  │  [SYMBOL______]  [Add]                        │   │
│  │  Error message (if invalid symbol)            │   │
│  │  BTCUSDT                         [Remove]     │   │
│  │  ETHUSDT                         [Remove]     │   │
│  └──────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
```

### Regions

**Notification Settings Form**
- Info box: step-by-step instructions to find your Telegram Chat ID
- Telegram Chat ID input (monospace, required)
- Enable notifications checkbox
- Save button: "Save Preferences" → "Saving..." while pending
- Success message: green "✓ Preferences saved." shown after successful save
- Pre-populated with current saved values on load

**Watchlist Manager**
- Symbol input (monospace, auto-uppercase) + Add button
- Error message if symbol is invalid or already in watchlist
- List of watched symbols, each with a Remove button
- Description text explains that watched symbols drive the signal engine

### States

**Notification Form**
| State | Description |
|---|---|
| Loading | Animated skeleton (single block) |
| Default | Form pre-filled with saved values (or blank if first time) |
| Pending | Save button disabled, shows "Saving..." |
| Success | Green success message appears |

**Watchlist Manager**
| State | Description |
|---|---|
| Loading | Animated skeleton |
| Empty | "No symbols watched yet." |
| Loaded | List of symbol rows |
| Add error | Red error message below input |

### User Actions
| Action | Result |
|---|---|
| Enter Chat ID + toggle enable + save | Save notification preferences |
| Enter symbol + click Add | Validate and add symbol to watchlist |
| Click Remove on a symbol | Remove symbol from watchlist |

---

## Component States — Summary Reference

| Component | Loading | Empty | Error |
|---|---|---|---|
| Transaction list | Skeleton rows | "No transactions for [month]." | — |
| Budget list | Skeleton boxes | "No budgets set for [month]." | — |
| Trade table | Skeleton rows | "No trades logged yet." | — |
| Signals list | Skeleton block | "No signals yet — add symbols to your watchlist." | — |
| Fear & Greed gauge | Skeleton block | — | — |
| Notification form | Skeleton block | — | — |
| Watchlist | Skeleton block | "No symbols watched yet." | Red message below input |
| Trade form | — | — | Red message below fields |

---

## Colour System Reference

| Colour | Meaning |
|---|---|
| Green | Income, positive P&L, Long direction, budget under 80% |
| Yellow/Orange | Budget 80–100% of limit, RSI Overbought signal badge |
| Red | Expense, negative P&L, Short direction, budget overrun, RSI Oversold signal badge |
| Blue | Primary action buttons, active nav link, Volume Spike signal badge |
| Gray | Secondary text, dates, inactive states |
