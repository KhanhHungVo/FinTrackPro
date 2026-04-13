# Dashboard Redesign & App-Wide Light/Dark Theme

## Overview

FinTrackPro's UI was originally built with flat white cards and a uniform `bg-gray-50` background — functional but visually monotonous. This redesign introduces:

- A **polished dark mode** with a dark navy background (`#0f1117`), glass-morphism cards, and coloured accent borders
- A **light/dark theme toggle** surfaced in the navbar locale dropdown, persisted across sessions
- A **dashboard greeting header** that adapts to the time of day and addresses the user by first name
- **Visual depth** added to KPI cards via left-border colour accents and larger typographic scale

Light mode remains the default. Dark mode is opt-in and applies consistently across every page and component.

---

## Design Tokens

### Background Layers

| Layer | Light | Dark |
|---|---|---|
| Page background | `bg-gray-50` | `bg-[#0f1117]` |
| Navbar | `bg-white` | `bg-[#0c0e14]` |
| Card / modal | `bg-white` | `bg-[#161a25]` or `rgba(255,255,255,0.04)` |
| Table header | `bg-gray-50` | `rgba(255,255,255,0.03)` |
| Input | `bg-white` | `bg-slate-800` |

### Card Styles

Two reusable card utilities are defined in `globals.css`:

**`glass-card`** — used for dashboard widgets (rounded-xl, soft glass look in dark mode)
- Light: white background, `border-gray-200`, `border-radius: 0.75rem`
- Dark: `rgba(255,255,255,0.04)` background, `rgba(255,255,255,0.12)` border, `backdrop-filter: blur(12px)`

**`page-card`** — used for list items and table wrappers across all non-dashboard pages (slightly tighter)
- Light: white background, `border-gray-200`, `border-radius: 0.5rem`
- Dark: `rgba(255,255,255,0.04)` background, `rgba(255,255,255,0.08)` border

### Typography Scale

| Role | Light | Dark |
|---|---|---|
| Primary text | `text-gray-900` | `dark:text-slate-200` |
| Secondary text | `text-gray-500` | `dark:text-slate-400` |
| Muted / labels | `text-gray-400` | `dark:text-slate-500` |
| Navbar inactive links | `text-gray-600` | `dark:text-slate-400` |

### Borders & Dividers

| Context | Light | Dark |
|---|---|---|
| Standard card border | `border-gray-200` | `dark:border-white/8` |
| Table row divider | `border-gray-50` | `dark:border-white/5` |
| Modal border | `border-gray-200` | `dark:border-white/10` |
| Navbar bottom border | `border-b` | `dark:border-white/6` |
| Dropdown divider | `border-t` | `dark:border-white/6` |

### Interactive States

| State | Light | Dark |
|---|---|---|
| Hover background | `hover:bg-gray-100` | `dark:hover:bg-white/5` |
| Active nav link | `bg-blue-600 text-white` | (unchanged — works on both) |
| Selected dropdown item | `bg-blue-50 text-blue-700` | `dark:bg-blue-500/15 dark:text-blue-400` |
| Input focus ring | `focus:ring-blue-500` | (unchanged) |

### Status & Semantic Colours

Badges and highlights use a consistent `bg-*/10–15` + `text-*/400` pattern in dark mode to maintain readability without overpowering the dark background:

| Semantic | Light | Dark |
|---|---|---|
| Success / income | `bg-green-100 text-green-700` | `dark:bg-green-500/15 dark:text-green-400` |
| Danger / expense | `bg-red-100 text-red-700` | `dark:bg-red-500/15 dark:text-red-400` |
| Warning | `bg-amber-50 border-amber-200 text-amber-900` | `dark:bg-amber-500/10 dark:border-amber-500/20 dark:text-amber-300` |
| Info / neutral | `bg-blue-50 text-blue-700` | `dark:bg-blue-500/15 dark:text-blue-400` |
| Muted / closed | `bg-gray-100 text-gray-600` | `dark:bg-white/5 dark:text-slate-400` |

---

## Theme System

### Storage

Theme preference is stored as `'light' | 'dark'` inside the existing `fintrackpro-locale` Zustand persist store. It survives page reloads and is available synchronously on mount, avoiding flash-of-wrong-theme.

### Application

The `dark` class is applied to the outermost `<div>` in `App.tsx`. Tailwind's `@variant dark (&:where(.dark, .dark *))` selector (defined in `globals.css`) drives all `dark:` utility classes throughout the tree. No `prefers-color-scheme` media query is used — the choice is always explicit and user-controlled.

### Toggle Surface

The theme toggle lives inside the **locale/currency dropdown** in the navbar (`LocaleSettingsDropdown`). It renders as a two-button inline group (sun icon · Light / moon icon · Dark) below the currency section. This keeps the toggle discoverable without cluttering the navbar with a standalone icon.

---

## Dashboard Greeting Header

The plain `<h1>Dashboard</h1>` is replaced with a contextual greeting:

- **Greeting** — time-based: "Good morning", "Good afternoon", or "Good evening" (morning: before 12:00, afternoon: 12:00–17:59, evening: 18:00+)
- **Name** — first token of the user's display name from the auth store
- **Date** — formatted with the browser's locale (e.g. "Sunday, April 13")

The greeting copies are i18n-keyed (`dashboard.goodMorning`, `dashboard.goodAfternoon`, `dashboard.goodEvening`) and translated in both `en` and `vi` locale files.

---

## KPI Cards — Visual Refresh

Each KPI card (Income, Expenses, Trading P&L) gains a **coloured left border accent** (`border-l-4`) to instantly communicate its category at a glance:

| Card | Accent colour |
|---|---|
| Income | `border-l-green-500` |
| Expenses | `border-l-red-500` |
| Trading P&L | `border-l-blue-500` |

The value typographic scale is increased from `text-2xl font-semibold` to `text-3xl font-bold tracking-tight` to create a stronger visual hierarchy. Cards use `bg-white dark:bg-white/4 dark:backdrop-blur-sm` instead of the `glass-card` utility to maintain the left-border rendering correctly (no rounded-xl conflict).

---

## Widget Card Patterns

Dashboard widgets use `glass-card` for visual consistency:

| Widget | Card type | Notes |
|---|---|---|
| KPI Summary | Inline (`bg-white + border-l-4`) | Left-border accent, no glass-card utility |
| Fear & Greed Index | `glass-card` | SVG fills/strokes use Tailwind classes for dark adaptation |
| Trending Coins | `glass-card overflow-hidden` | Shimmer skeleton uses dark variants |
| Signals List | `glass-card` | Per-signal-type badge colours each have dark variants |

Non-dashboard pages (Transactions, Budgets, Trades) use `page-card` for list items and table wrappers to maintain a slightly tighter visual weight appropriate for data-dense views.

---

## SVG Theming

Widgets with inline SVGs (Fear & Greed gauge, Trending Coins info icon) replace hardcoded `fill`/`stroke` hex values with Tailwind utility classes or `stroke="currentColor"`, so they inherit the correct colour in both themes:

- `fill="#9ca3af"` → `className="fill-gray-400 dark:fill-slate-600"`
- `stroke="#1f2937"` → `className="stroke-gray-800 dark:stroke-slate-200"`
- Generic icon strokes → `stroke="currentColor"` with a `text-gray-400` class on the element

---

## Skeleton Loaders

All skeleton/shimmer placeholders use:
- Light: `bg-gray-100` / gradient `from-gray-100 via-gray-200 to-gray-100`
- Dark: `dark:bg-white/5` / gradient `dark:from-white/5 dark:via-white/8 dark:to-white/5`

This ensures loading states remain visually distinguishable against the dark background without appearing too bright.

---

## Scope

The redesign covers every user-facing surface:

| Area | Components |
|---|---|
| Layout shell | `App.tsx`, `Navbar`, `DonationFooter` |
| Dashboard | `DashboardPage`, `KpiSummaryWidget`, `DeltaBadge`, `FearGreedWidget`, `TrendingCoinsWidget`, `SignalsList` |
| Pages | `TransactionsPage`, `BudgetsPage`, `TradesPage`, `SettingsPage`, `PricingPage` |
| Modals & forms | All add/edit/close modals, upgrade modals, category modals, notification & watchlist forms |
| Shared UI | `AuthErrorScreen`, `ErrorPage`, `NotFoundPage`, `DonationModal`, `BankTransferDetails` |
