# FinTrackPro — UI Flows

> Last updated: 2026-04-13
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
| User menu open | Dropdown with name, email, plan badge, Logout button |
| Locale dropdown closed | Globe icon button |
| Locale dropdown open | Panel with language, currency, and theme sections |

### User Actions
| Action | Result |
|---|---|
| Click nav link | Navigate to corresponding page |
| Click avatar | Open user menu |
| Click Logout | End session, redirect to IAM login |
| Click globe icon | Open locale/currency/theme dropdown |
| Click Light / Dark in theme section | Switch app theme; persisted across reloads |

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
│  Good morning, Alex 👋                               │  ← Greeting header
│  Sunday, April 13                                    │  ← Date subtitle
├─────────────────────────────────────────────────────┤
│  Income (month)   │  Expenses (month)  │  Trading P&L │  ← Summary cards
├───────────────────┴────────────────────┴─────────────┤
│  Fear & Greed Gauge (left)  │  Trending Coins (right) │  ← Market widgets
├─────────────────────────────────────────────────────┤
│  Recent Signals (full width)                         │  ← Signals list
└─────────────────────────────────────────────────────┘
```

### Regions

**Greeting Header**
- Time-based greeting using the user's first name: "Good morning / afternoon / evening, [First name] 👋"
  - Morning: before 12:00 · Afternoon: 12:00–17:59 · Evening: 18:00+
- Subtitle shows today's date, formatted with the browser's locale (e.g. "Sunday, April 13")
- Greeting text is i18n-keyed and translated in both EN and VI

**Summary Cards (3-column grid)**
- Each card has a coloured left-border accent for at-a-glance category recognition:
  - Income: green left border — formatted as currency
  - Expenses: red left border — formatted as currency
  - Trading P&L: blue left border — green if positive, red if negative
- Month-over-month delta badge shown below each value when previous month data is available

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
None — Dashboard is read-only (all widgets are display-only).

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
- Category field (dropdown selector, required) — groups system categories and user-created custom categories; shows emoji icon + localized label (EN/VI); includes "Manage my categories" link to /settings
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
- Category field (dropdown selector, required) — same grouped selector with emoji icons and localized labels (EN/VI)
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
- Columns (left to right): Symbol, Direction, Status, P&L, Entry Price, Exit/Current Price, Position Size, Fees, Date, Actions
- Direction badge: Long=green / Short=red
- Status badge: Open=emerald / Closed=gray
- P&L column:
  - Closed trades: realised P&L in green/red (formula: `(exitPrice − entryPrice) × positionSize − fees`)
  - Open trades: unrealised P&L in muted gray with "(unrlzd)" label; sourced from live price
  - No exit price yet: "—"
- Exit/Current Price: shows exit price for closed trades, live current price for open trades
- Actions column: visible on row hover only
  - ✓ (Close) — shown only for Open trades; opens Close Position modal
  - ✎ (Edit) — opens Edit Trade modal
  - ✕ (Delete) — deletes trade with guard against double-click

**Unrealised P&L Card**
- A 4th summary card appears in the stats bar when at least one open trade has a live price
- Emerald tinted card displaying the aggregate unrealised P&L across all open positions

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
| Edit modal open | Pre-filled modal over the page |
| Close modal open | Confirmation modal to record exit price |

### User Actions
| Action | Result |
|---|---|
| Click "+ Log Trade" | Toggle form visibility |
| Toggle Long/Short | Sets trade direction |
| Fill and submit form | Creates trade, refreshes table and stats |
| Hover a row | Reveals action buttons (✓ ✎ ✕) |
| Click ✓ on an open trade | Opens Close Position modal |
| Click ✎ on a row | Opens Edit Trade modal |
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

**My Categories Section**
- Header row: "My Categories" label + description + "+ New category" button (blue, top-right)
- Category list: each row shows emoji icon, EN name, VI name (gray subtext), type chip (red for Expense, green for Income), Edit and Delete buttons
- Empty state: 🗂️ illustration + "No custom categories yet" + "Create first category" button
- Edit button → opens Create/Edit Modal pre-filled with category data
- Delete button → soft-deletes the category immediately (no confirmation)
- System categories are never shown or manageable here

**Create/Edit Category Modal**
- Opened from the My Categories section (create or edit mode)
- Type chips: "Expense" (red tint) / "Income" (green tint) — disabled in edit mode (type is immutable)
- Emoji icon picker: scrollable 8-column grid of ~60 curated emojis; selected emoji highlighted with blue ring; live preview badge shows selected icon + current EN name
- Side-by-side name fields: EN name (required) and VI name (required)
- Slug preview (read-only monospace, auto-derived from EN name) — shown in create mode only
- Footer: Cancel + "Create category" / "Save changes" (blue primary)

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

---

## Subscription UI Flows

### PlanBadge (Navbar — user dropdown)

- Free users: gray pill `Free ▸` — clicking navigates to `/pricing`
- Pro users: blue pill `Pro` (display-only)
- Rendered inside the user avatar dropdown in the Navbar

### FreePlanAdBanner (Dashboard — Free users only)

- Full-width gradient banner shown at the top of the Dashboard page body
- Hidden for Pro users (returns `null` when `plan === 'Pro'`)
- "Upgrade to Pro →" button navigates to `/pricing`

### Upgrade Flow (triggered by any 402 response)

```
User action hits plan limit
  → API returns 402 with { extensions.feature, title }
  → 402 interceptor in shared/api/client.ts calls planLimitStore.setLimit(feature, title)
  → PlanLimitModal opens (mounted globally in App.tsx)
  → User clicks "Upgrade to Pro"
  → POST /api/subscription/checkout → { sessionUrl }
  → window.location.href = sessionUrl  (Stripe Checkout)
  → Stripe redirects back to /settings?subscribed=1
  → SubscriptionSection detects query param → toast "You're now on Pro!"
```

### Pricing Page (`/pricing`)

- Two cards: Free (highlighted when current plan) and Pro (highlighted when current plan)
- Free card has a disabled "Current plan" button
- Pro card: "Upgrade to Pro" button for Free users; "Manage subscription" (→ Stripe portal) for Pro users
- Accessed via `FreePlanAdBanner`, `PlanBadge` click, or direct URL

### Manage Subscription Flow (Pro users)

```
Settings → Subscription section → "Manage subscription"
  → POST /api/subscription/portal → { portalUrl }
  → window.location.href = portalUrl  (Stripe Customer Portal)
  → User manages billing, cancels, or updates payment method
  → Stripe redirects back to /settings
```

### Telegram Notifications Paywall (Free users)

- `NotificationSettingsForm` renders with `opacity-50 pointer-events-none` overlay when plan is Free
- A centered overlay shows "Telegram notifications are a Pro feature." + `UpgradeButton`
- Pro users see the normal form with no overlay

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

### Semantic Colours

| Colour | Meaning |
|---|---|
| Green | Income, positive P&L, Long direction, budget under 80% |
| Yellow/Orange | Budget 80–100% of limit |
| Red | Expense, negative P&L, Short direction, budget overrun |
| Blue | Primary action buttons, active nav link |
| Emerald | Open trade status, unrealised P&L card, close action |
| Gray | Secondary text, dates, inactive states, closed trade status |

### Signal Badge Colours

| Signal Type | Light | Dark |
|---|---|---|
| RsiOversold | Red | `dark:bg-red-500/15 dark:text-red-400` |
| RsiOverbought | Orange | `dark:bg-orange-500/15 dark:text-orange-400` |
| VolumeSpike | Blue | `dark:bg-blue-500/15 dark:text-blue-400` |
| FundingRate | Purple | `dark:bg-purple-500/15 dark:text-purple-400` |
| EmaCross | Green | `dark:bg-green-500/15 dark:text-green-400` |
| BbSqueeze | Yellow | `dark:bg-yellow-500/15 dark:text-yellow-400` |

### Theme

The app supports light (default) and dark modes, toggled via the locale dropdown in the navbar. The preference is persisted in `localStorage` (inside the `fintrackpro-locale` Zustand store). See `docs/planned/dashboard-redesign-light-dark-theme.md` for the full design token reference.
