# FinTrackPro — UI Flows

> Last updated: 2026-04-20
> Format: screen-by-screen reference for UI design and Figma workflow generation.

---

## Navigation Flow

```
[Unauthenticated] ─────────────────────────────► [Landing Page /]
                                                        │
                  ┌─────────────────────────────────────┘
                  │  CTA: "Log in" / "Start for Free"
                  ▼
[Login / Sign-up (IAM provider)] ──────────────► [Dashboard /dashboard]
                                                        │
                        [Persistent Navbar] ────────────┤
                          ├── /dashboard                 │
                          ├── /transactions               │
                          ├── /budgets                    │
                          ├── /trades                     │
                          ├── /market                     │
                          └── /settings                   │
                                                          ▼
                          Dashboard → Transactions → Budgets → Trades → Market → Settings

          [Avatar Dropdown] ── Settings → /settings
                            ── Plan & Billing → /pricing
                            ── About → /about
                            ── Sign out
```

Unauthenticated users who navigate directly to any protected route (e.g. `/dashboard`) are redirected to `/` by the `RequireAuth` guard. Authenticated users who visit `/` are immediately redirected to `/dashboard`.

---

## Screen: Landing Page

**Route:** `/`
**Auth:** public — no login redirect. Unauthenticated users see the full page; authenticated users are redirected to `/dashboard`.

### Layout
Full-viewport dark-themed marketing page composed of stacked sections:

| Section | Purpose |
|---------|---------|
| `LandingNav` | Logo + "Log in" + "Start for Free" buttons |
| `HeroSection` | Headline, sub-copy, primary CTA |
| `PainPointsSection` | Problem framing cards |
| `DashboardMockupSection` | Product UI preview |
| `OutcomeSpotlightsSection` | Outcome highlights |
| `FeaturesSection` | Feature grid |
| `PricingSection` | Static Free / Pro pricing cards (limits read from `env.*`) |
| `HowItWorksSection` | 3-step onboarding overview |
| `LandingFooter` | Links and copyright |

### User Actions
| Action | Result |
|---|---|
| Click "Log in" | `authAdapter.login({ screen: 'login' })` → IAM provider login screen |
| Click "Start for Free" / "Upgrade to Pro" | `authAdapter.login({ screen: 'signup' })` → IAM provider registration screen |
| Click "View full pricing →" | Navigate to `/pricing` |

---

## Screen: Navbar (Persistent)

**Present on:** all authenticated pages

### Layout
- Left: Logo ("FinTrackPro")
- Centre: Nav links — Dashboard, Transactions, Budgets, Trades, Market, Settings
- Right: User avatar (first initial of name); click opens dropdown

### Avatar dropdown contents
1. User display name + email + plan badge
2. **Settings** button → `/settings`
3. **Plan & Billing** button → `/pricing`
4. **About** button → `/about`
5. Divider
6. **Sign out** button

### States
| State | Description |
|---|---|
| Active link | Blue background, white text |
| Inactive link | Gray text, hover darkens |
| User menu closed | Avatar circle only |
| User menu open | Dropdown with name, email, plan badge, nav buttons, Sign out |
| Locale dropdown closed | Globe icon button |
| Locale dropdown open | Panel with language, currency, and theme sections |

### User Actions
| Action | Result |
|---|---|
| Click nav link | Navigate to corresponding page |
| Click avatar | Open user dropdown |
| Click Settings in dropdown | Navigate to `/settings`; close dropdown |
| Click Plan & Billing in dropdown | Navigate to `/pricing`; close dropdown |
| Click About in dropdown | Navigate to `/about`; close dropdown |
| Click Sign out | End session, redirect to IAM login |
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
**Purpose:** Personalized command center — surfaces the user's own financial data (allocation, budget health, trading performance, recent activity) plus contextual market signals for their watchlist. Generic market data (Fear & Greed, Trending Coins) moved to `/market`.

### Layout
```
┌─────────────────────────────────────────────────────┐
│  [FreePlanAdBanner — Free users only]                │
├─────────────────────────────────────────────────────┤
│  Good morning, Alex 👋   Sunday, April 17            │  ← Greeting header
├─────────────────────────────────────────────────────┤
│  Income · Month │ Expenses · Month │ P&L · Month │ Unrealized P&L │  ← KPI header (4-col)
├─────────────────────────────────────────────────────┤
│  Expense Allocation (donut)  │  Budget Health        │  ← Section 2 (side-by-side)
├─────────────────────────────────────────────────────┤
│  Trading Intelligence (hidden if 0 trades)           │  ← Section 3
│    Open Positions panel · Closed Trades panel        │
├─────────────────────────────────────────────────────┤
│  Recent Activity (merged transactions + trades)      │  ← Section 4
├─────────────────────────────────────────────────────┤
│  Signals for Your Watchlist (hidden if watchlist=∅)  │  ← Section 5
└─────────────────────────────────────────────────────┘
```

### Regions

**Greeting Header**
- Time-based greeting using the user's first name: "Good morning / afternoon / evening, [First name] 👋"
  - Morning: before 12:00 · Afternoon: 12:00–17:59 · Evening: 18:00+
- Subtitle shows today's date, formatted with the browser's locale (e.g. "Sunday, April 17")
- Greeting text is i18n-keyed and translated in both EN and VI

**KPI Header — 4 cards (`grid-cols-2 lg:grid-cols-4`)**
| Card | Color | Period | Subtitle |
|---|---|---|---|
| Income | green border | This month | Last month value; delta badge |
| Expenses | red border | This month | Last month value; delta badge |
| Trading P&L | blue border | This month (closed trades) | Last month value; delta badge |
| Unrealized P&L | purple border | All open positions | "N open position(s)" or "No open positions" |

**Expense Allocation** (`ExpenseAllocationWidget`)
- Recharts donut chart grouping current-month Expense transactions by category
- Center label: total expenses in preferred currency
- Legend: category emoji + name + amount + percentage (top categories; "+ N more" overflow)
- Empty state: "No expenses this month" + link to /transactions
- Data: `useTransactions({ month, type: 'Expense', pageSize: 200 })` grouped client-side

**Budget Health** (`BudgetHealthWidget`)
- Compact progress bar list sorted worst-first; max 5 shown
- Summary header: "X of Y on track"
- Bar colors: green (< 60%), yellow (60–90%), red (> 90%)
- Footer: "View all budgets" link to /budgets
- Empty state: "No budgets set" + link to /budgets

**Trading Intelligence** (`TradingIntelligenceWidget`) — hidden when `totalTrades === 0`
- *Open Positions panel* — unrealized P&L card + open count card + capital allocation donut (one slice per symbol) + winning/losing position lists + risk signals (concentration warnings)
- *Closed Trades panel* — realised P&L + win rate + avg P&L/trade cards + cumulative P&L line chart (weekly buckets) + by-symbol / by-direction breakdowns + avg win/loss/R:R metrics
- Empty state for closed panel: "No closed trades this month" (open panel still shown above)

**Recent Activity** (`RecentActivityWidget`)
- Merged feed: latest 5 transactions + latest 5 trades, sorted by `createdAt` desc, top 8 shown
- Left-border color coding: green = income, red = expense, blue = trade
- Empty state: "No recent activity yet"

**Contextual Signals** (`ContextualSignalsWidget`) — hidden when watchlist is empty
- Wraps `SignalsList` (5 items) with section heading + "Manage watchlist" link to `/settings?tab=watchlist`
- Full signals list available on the Market page

### States
| State | Description |
|---|---|
| Loading | Animated skeleton placeholders per region |
| Loaded | All sections rendered |
| No trades | Trading Intelligence section hidden entirely |
| No watchlist | Contextual Signals section hidden entirely |
| No expenses | Expense Allocation shows empty state |
| No budgets | Budget Health shows empty state |

### User Actions
None — Dashboard is read-only (all widgets are display-only).

### Navigates To
- Any page via Navbar
- `/transactions` via Expense Allocation empty state link
- `/budgets` via Budget Health links
- `/trades` via Trading Intelligence "View all" links
- `/settings?tab=watchlist` via Contextual Signals "Manage watchlist" link

---

## Screen: Market

**Route:** `/market`
**Purpose:** Dedicated page for generic market data — Fear & Greed Index, trending coins, and the full signals list. Previously embedded in Dashboard; moved to reduce dashboard noise for users without active trading.

### Layout
```
┌─────────────────────────────────────────────────────┐
│  Market                                              │  ← Page title
├──────────────────────────┬──────────────────────────┤
│  Fear & Greed Index      │  Trending Coins           │  ← Market widgets (2-col)
├─────────────────────────────────────────────────────┤
│  Recent Signals (up to 20, full width)               │  ← Signals list
└─────────────────────────────────────────────────────┘
```

### Regions

**Fear & Greed Widget**
- SVG semicircle gauge with animated needle
- 5 colour zones: Extreme Fear (0–20, red) / Fear (20–40, orange) / Neutral (40–60, yellow) / Greed (60–80, green) / Extreme Greed (80–100, dark green)
- Numeric value and label displayed below gauge
- Data refreshed every hour

**Trending Coins**
- List of top 7 trending coins: coin name, symbol, market cap rank
- Powered by CoinGecko; data refreshed every 15 minutes

**Recent Signals (full list)**
- Up to 20 latest market signals across all watched symbols
- Each row: signal type badge (colour-coded) + symbol + message preview + timestamp
- Empty state: "No signals yet — add symbols to your watchlist."

### States
| State | Description |
|---|---|
| Loading | Animated skeleton placeholders |
| Loaded | All three regions displayed |
| No signals | "No signals yet — add symbols to your watchlist." |

### User Actions
None — Market page is read-only.

---

## Screen: Transactions

**Route:** `/transactions`
**Purpose:** Log, browse, and delete income and expense transactions with search, filter, sort, and pagination.

### Layout
```
┌─────────────────────────────────────────────────────┐
│  Transactions                        [+ Add]         │  ← Header
├──────────────┬─────────────┬────────────────────────┤
│  Income      │  Expenses   │  Net                   │  ← Summary cards (from /summary endpoint)
├─────────────────────────────────────────────────────┤
│  [Search___] [Month▼] [Type▼] [Category▼]           │  ← Filter bar
├─────────────────────────────────────────────────────┤
│  [Add Transaction Form — visible only when toggled]  │  ← Collapsible form
├─────────────────────────────────────────────────────┤
│  ↕ Date  ↕ Category  Note    ↕ Amount  [✕]          │  ← Sortable column headers
│  [badge] Category   Note     Amount   Date    [✕]   │  ← Transaction rows
│  ...                                                 │
├─────────────────────────────────────────────────────┤
│  < 1 2 3 >   10 ▼ per page                          │  ← Pagination
└─────────────────────────────────────────────────────┘
```

### Regions

**Summary Cards (3-column grid)**
- Sourced from `GET /api/transactions/summary` with active filter state (month, type, categoryId)
- Updates when any filter changes; **unaffected by page or sort**
- Income: sum of all Income transactions matching filters (green, +$X.XX)
- Expenses: sum of all Expense transactions matching filters (red, -$X.XX)
- Net: Income − Expenses (green if positive, red if negative)

**Filter Bar**
- Search input (debounced 300 ms) — searches note and category name
- Month selector dropdown — last 6 months; filters all data to selected month
- Type toggle: All / Income / Expense
- Category dropdown — filter by a specific category
- Any filter change resets page to 1

**Add Transaction Form (collapsible)**
- Hidden by default; revealed by the "+ Add" button in the header
- Type toggle: "Income" (green) / "Expense" (red) — one active at a time
- Amount field (number, required, > 0)
- Category field (dropdown selector, required) — groups system and custom categories; shows emoji icon + localized label (EN/VI); includes "Manage my categories" link to `/settings?tab=categories`
- Note field (text, optional)
- Submit button: "Add" → "Saving..." while pending → clears form and collapses on success
- Month taken from the current month filter selection

**Transaction Table**
- Sortable column headers: Date (default desc), Amount, Category — click cycles: default → desc → asc → reset; arrow indicator (↑ ↓ ↕)
- One row per transaction: type badge, category name, note (gray subtext), amount (green/red), date, delete button (✕)

**Pagination**
- Classic `< 1 2 … N >` with ellipsis beyond 7 pages
- Page size dropdown: 10 / 20 / 50
- Page resets to 1 on any filter or sort change

### States
| State | Description |
|---|---|
| Loading | 4 animated skeleton rows |
| Empty | "No transactions for [month]." (or "No results match your search.") |
| Loaded | Filter bar + table + pagination |
| Form hidden | Default — only filters, summary cards, and table visible |
| Form visible | Add form shown below filter bar |
| Form pending | Submit button disabled, shows "Saving..." |

### User Actions
| Action | Result |
|---|---|
| Type in search box | Debounced filter; page resets to 1 |
| Change month / type / category filter | Reload list and refresh summary cards; page resets to 1 |
| Click sortable column header | Cycle sort direction; page resets to 1 |
| Click "+ Add" | Toggle form visibility |
| Toggle Income/Expense | Sets transaction type on form |
| Fill and submit form | Creates transaction, refreshes list and summary |
| Click ✕ on a row | Deletes that transaction, refreshes list and summary |
| Navigate pagination | Load selected page |
| Change page size | Reload with new page size; page resets to 1 |

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
│  [Show over budget only ☐]  ↕ Category  ↕ Spent %   │  ← Filter / sort controls
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

**Filter / Sort Controls (client-side)**
- "Show over budget only" toggle — hides budgets where spent < limit
- Clickable "Category" label — sorts alphabetically (asc → desc → reset)
- Clickable "Spent %" label — sorts by spent percentage (desc → asc → reset)
- Filter and sort are applied in-memory; no additional API calls

**Budget Cards**
- Category name
- Spent / Limit amounts (e.g. "$150.00 / $500.00")
- Progress bar: green (< 80%), yellow (80–100%), red (> 100%)
- Overrun state: red bar, red "Over budget by $X.XX" label
- Edit button (✎) → switches amount to inline input; saves on blur or Enter, cancels on Escape
- Delete button (×)
- Spending is auto-calculated from Expense transactions in the same month and category (loaded with `pageSize: 100`)

### States
| State | Description |
|---|---|
| Loading | 3 animated skeleton boxes |
| Empty | "No budgets set for [month]." |
| Loaded | Filter controls + list of budget cards |
| Filtered empty | "No over-budget categories for [month]." (when toggle is on and all budgets are under limit) |
| Edit mode (per card) | Limit amount becomes an editable input |
| Overrun | Red progress bar + red overage text |
| Form pending | Add button disabled, shows "..." |

### User Actions
| Action | Result |
|---|---|
| Change month | Reload budgets and recalculate spending |
| Fill and submit Add form | Creates budget, refreshes list |
| Toggle "Show over budget only" | Filter cards client-side; no API call |
| Click Category / Spent % sort label | Client-side sort; cycles direction |
| Click ✎ on a card | Enter inline edit mode for that card's limit |
| Blur / press Enter in edit | Save new limit, exit edit mode |
| Press Escape in edit | Cancel edit, restore original value |
| Click × on a card | Delete budget, refreshes list |

---

## Screen: Trades

**Route:** `/trades`
**Purpose:** Log and review crypto trades with search, filter, sort, pagination, and aggregate P&L statistics.

### Layout
```
┌─────────────────────────────────────────────────────┐
│  Trades                          [+ Log Trade]       │  ← Header
├──────────────┬─────────────┬────────────────────────┤
│  Total P&L   │  Win Rate   │  Total Trades           │  ← KPI cards (from /summary endpoint)
├─────────────────────────────────────────────────────┤
│  [Search___] [Status▼] [Direction▼] [From] [To]     │  ← Filter bar
├─────────────────────────────────────────────────────┤
│  [AddTradeForm — visible only when toggled]          │  ← Collapsible form
├─────────────────────────────────────────────────────┤
│  ↕ Date │ Symbol │ Dir │ ↕ Entry │ ↕ Size │ ↕ Fees │ ↕ P&L │ … │  ← Sortable headers
│  Trades table (scrollable)                           │
├─────────────────────────────────────────────────────┤
│  < 1 2 3 >   10 ▼ per page                          │  ← Pagination
└─────────────────────────────────────────────────────┘
```

### Regions

**KPI Cards (3–4 column grid)**
- Sourced from `GET /api/trades/summary` with active filter state (status, direction, dateFrom, dateTo)
- Updates when any filter changes; **unaffected by page or sort** — represent totals over the full filtered dataset
- Total P&L: sum of all realized trade results matching filters (green/red)
- Win Rate: percentage of matching trades where P&L > 0
- Total Trades: count of matching trades
- Unrealised P&L card (4th): appears when filter includes Open trades that have a live price; emerald tinted

**Filter Bar**
- Search input (debounced 300 ms) — searches symbol
- Status toggle: All / Open / Closed
- Direction toggle: All / Long / Short
- Date range: From / To date inputs (optional)
- Any filter change resets page to 1

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
- Sortable column headers (click cycles: default → desc → asc → reset; arrow indicator ↑ ↓ ↕): Date (default desc), P&L, Symbol, Entry Price, Position Size, Fees
- Columns: Symbol, Direction, Status, P&L, Entry Price, Exit/Current Price, Position Size, Fees, Date, Actions
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

**Pagination**
- Classic `< 1 2 … N >` with ellipsis beyond 7 pages
- Page size dropdown: 10 / 20 / 50
- Page resets to 1 on any filter or sort change

### States
| State | Description |
|---|---|
| Loading | 4 animated skeleton rows |
| Empty | "No trades logged yet. Click 'Log Trade' to get started." |
| Loaded | Filter bar + KPI cards + table + pagination |
| Form hidden | Default — only filters, KPI cards, and table visible |
| Form visible | Form shown below filter bar |
| Form pending | Submit button disabled, shows "Saving..." |
| Form error | Red error message below form fields |
| Edit modal open | Pre-filled modal over the page |
| Close modal open | Confirmation modal to record exit price |

### User Actions
| Action | Result |
|---|---|
| Type in search box | Debounced filter; page resets to 1 |
| Change status / direction / date range filter | Reload list and refresh KPI cards; page resets to 1 |
| Click sortable column header | Cycle sort direction; page resets to 1 |
| Click "+ Log Trade" | Toggle form visibility |
| Toggle Long/Short | Sets trade direction |
| Fill and submit form | Creates trade, refreshes table and KPI cards |
| Hover a row | Reveals action buttons (✓ ✎ ✕) |
| Click ✓ on an open trade | Opens Close Position modal |
| Click ✎ on a row | Opens Edit Trade modal |
| Click ✕ on a row | Deletes that trade, refreshes table and KPI cards |
| Navigate pagination | Load selected page |
| Change page size | Reload with new page size; page resets to 1 |

---

## Screen: Settings

**Route:** `/settings` (default tab: `account`)
**Purpose:** Account management, billing, notification preferences, transaction categories, and watchlist — each in its own tab.

### Layout

```
┌─────────────────────────────────────────────────────┐
│  Settings                                            │
├──────────────┬──────────────────────────────────────┤
│  Account     │                                       │  ← desktop: w-44 sidebar
│  Plan &      │   [Active tab content]                │
│  Billing     │                                       │
│  Notifications│                                      │
│  Categories  │                                       │
│  Watchlist   │                                       │
└──────────────┴──────────────────────────────────────┘
```

Mobile: horizontal scrollable tab strip above the content panel (sidebar hidden).

Active tab is stored in the URL query string (`?tab=<slug>`). Invalid slugs fall back to `account`. Deep links work (e.g. `/settings?tab=billing` loads the billing tab directly).

### Tabs

| Slug | Label | Content |
|------|-------|---------|
| `account` | Account | Placeholder card — profile editing coming soon |
| `billing` | Plan & Billing | `SubscriptionSection` |
| `notifications` | Notifications | `NotificationSettingsForm` |
| `categories` | Categories | `ManageCategoriesSection` |
| `watchlist` | Watchlist | `WatchlistManager` |

### Regions

**Account tab**
- Placeholder card: "Profile settings — Profile editing is coming soon."

**Plan & Billing tab (`SubscriptionSection`)**
- See Subscription UI Flows section below.

**Categories tab (`ManageCategoriesSection`)**
- Header row: "My Categories" label + description + "+ New category" button (blue, top-right)
- Category list: each row shows emoji icon, EN name, VI name (gray subtext), type chip (red for Expense, green for Income), Edit and Delete buttons
- Empty state: 🗂️ illustration + "No custom categories yet" + "Create first category" button
- Edit button → opens Create/Edit Modal pre-filled with category data
- Delete button → soft-deletes immediately (no confirmation)
- System categories are never shown or manageable here

**Create/Edit Category Modal**
- Type chips: "Expense" (red tint) / "Income" (green tint) — disabled in edit mode (type is immutable)
- Emoji icon picker: scrollable 8-column grid of ~60 curated emojis; selected emoji highlighted with blue ring; live preview badge shows selected icon + current EN name
- Side-by-side name fields: EN name (required) and VI name (required)
- Slug preview (read-only monospace, auto-derived from EN name) — shown in create mode only
- Footer: Cancel + "Create category" / "Save changes" (blue primary)

**Notifications tab (`NotificationSettingsForm`)**
- Info box: step-by-step instructions to find your Telegram Chat ID
- Telegram Chat ID input (monospace, required)
- Enable notifications checkbox
- Save button: "Save Preferences" → "Saving..." while pending
- Success message: green "✓ Preferences saved." shown after successful save
- Pre-populated with current saved values on load

**Watchlist tab (`WatchlistManager`)**
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
| Click tab | Switch active tab; URL updates to `?tab=<slug>` |
| Enter Chat ID + toggle enable + save | Save notification preferences |
| Enter symbol + click Add | Validate and add symbol to watchlist |
| Click Remove on a symbol | Remove symbol from watchlist |

---

## Screen: About

**Route:** `/about`
**Purpose:** App identity, version, links, and author info.

### Layout

```
← Back

┌────────────────────────────────────────┐
│  About FinTrackPro             (h1)    │
│  Your personal finance & trading…      │
│                                        │
│  Description paragraph                 │
│  ──────────────────────────            │
│  Version v1.0.0                        │
│  View pricing plans →                  │
│  Buy me a coffee ☕                    │
└────────────────────────────────────────┘

┌────────────────────────────────────────┐
│  Built by                 (label)      │
│  Khanh Hung Vo                         │
│  [GitHub icon]  [Email icon]           │
└────────────────────────────────────────┘
```

### User Actions
| Action | Result |
|---|---|
| Click "← Back" | `navigate(-1)` |
| Click "View pricing plans →" | Navigate to `/pricing` |
| Click "Buy me a coffee ☕" | Open donation modal |
| Click GitHub / Email icons | Open external link (placeholders until filled) |

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
  → Stripe redirects back to /settings?tab=billing&subscribed=1
  → SubscriptionSection detects query param → toast "You're now on Pro!"
```

### Pricing Page (`/pricing`)

- Two cards: Free (highlighted when current plan) and Pro (highlighted when current plan)
- Free card has a disabled "Current plan" button
- Pro card: "Upgrade to Pro" button for Free users; "Manage subscription" (→ Stripe portal) for Pro users
- Accessed via `FreePlanAdBanner`, `PlanBadge` click, avatar dropdown "Plan & Billing", or direct URL

### Manage Subscription Flow (Pro users)

```
Settings (billing tab) → "Manage subscription"
  → POST /api/subscription/portal → { portalUrl }
  → window.location.href = portalUrl  (Stripe Customer Portal)
  → User manages billing, cancels, or updates payment method
  → Stripe redirects back to /settings?tab=billing
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
| Expense Allocation | Skeleton block | "No expenses this month" | — |
| Budget Health | Skeleton block | "No budgets set" | — |
| Trading Intelligence | Hidden | Hidden (when 0 trades) | — |
| Recent Activity | Skeleton block | "No recent activity yet" | — |
| Contextual Signals | Hidden | Hidden (when watchlist empty) | — |
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
| Purple | Unrealized P&L KPI card (Dashboard); capital allocation donut accents |
| Emerald | Open trade status, close action button |
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
