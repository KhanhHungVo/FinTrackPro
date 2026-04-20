# Navigation — Avatar Dropdown Links & Tabbed Settings

## Context

The app had no discoverable path to `/pricing` for Pro users (the upgrade banner is hidden for them), and the Settings page was a single long-scroll layout that would become unmanageable as more sections were added. The avatar dropdown only showed the user's name and a Sign out button.

This document describes two related changes shipped together: expanding the avatar dropdown to surface key account pages, and converting Settings to a URL-persisted tabbed layout.

---

## Key Decisions

### Avatar dropdown surfaces Settings, Plan & Billing, and About

Following the pattern used by Robinhood, YNAB, Coinbase, and Revolut, secondary account pages are accessed through the avatar dropdown rather than the top nav. The top nav stays at six items (Dashboard, Transactions, Budgets, Trades, Market, Settings) and is unchanged.

Three buttons are added above the Sign out row: **Settings**, **Plan & Billing** (navigates to `/pricing`), and **About** (navigates to `/about`). A `border-t` divider separates them from Sign out. Each button calls `navigate()` and closes the dropdown (`setOpen(false)`).

### Settings tab state lives in the URL (`?tab=<slug>`)

Tab state is stored in the query string rather than component state so that:
- Deep links work (`/settings?tab=billing` loads the correct tab directly).
- The Stripe portal return URL can target the billing tab (`/settings?tab=billing`).
- The browser Back button restores the previous tab.

`useSearchParams` from `react-router` reads and writes `?tab`. Invalid slugs fall back to the default tab (`account`). Tab switching preserves any other existing query params via a `URLSearchParams` copy.

### Five tabs replace the single-scroll layout

| Slug | Content |
|------|---------|
| `account` | Placeholder card — profile editing coming soon |
| `billing` | `SubscriptionSection` (existing component, unchanged) |
| `notifications` | `NotificationSettingsForm` (existing component, unchanged) |
| `categories` | `ManageCategoriesSection` (existing component, unchanged) |
| `watchlist` | `WatchlistManager` (existing component, unchanged) |

All section components are reused as-is; only the layout wrapper changes.

### Responsive layout: sidebar on desktop, horizontal scroll strip on mobile

- **≥ md:** flex row — fixed `w-44` sidebar of tab buttons + `flex-1` content area.
- **< md:** horizontally scrollable tab strip above the content panel.

Two separate render paths (one hidden per breakpoint via Tailwind) keep the DOM clean and avoid layout shifts.

### `AboutPage` follows the standard card style

The About page uses two `rounded-xl border bg-white p-6 dark:bg-white/4 dark:border-white/6` cards — identical to PricingPage and SettingsPage. No custom glows, no inline SVG decorations. Social links (`href="#"`) and the coffee-support button are placeholders, trivially replaceable without touching i18n.

---

## Links updated to use tab anchors

Two existing deep links now target a specific tab instead of the bare `/settings` URL:

| Location | Old link | New link |
|----------|----------|----------|
| `ContextualSignalsWidget` "Manage watchlist" | `/settings` | `/settings?tab=watchlist` |
| `MarketPage` "Manage watchlist" | `/settings` | `/settings?tab=watchlist` |
| Stripe portal `returnUrl` in `PricingPage` | `/settings` | `/settings?tab=billing` |

---

## Files Changed

| File | Action |
|------|--------|
| `src/shared/i18n/en.ts` | Add `nav.pricing`, `nav.about`; settings tab keys; `about` namespace |
| `src/shared/i18n/vi.ts` | Mirror Vietnamese translations |
| `src/pages/about/ui/AboutPage.tsx` | Create — two-card layout with identity, version, pricing link, support link, author |
| `src/pages/about/index.ts` | Create — barrel export |
| `src/pages/settings/ui/SettingsPage.tsx` | Replace — convert to tabbed layout with URL-persisted active tab |
| `src/widgets/navbar/ui/Navbar.tsx` | Modify — add Settings / Plan & Billing / About buttons + divider in dropdown |
| `src/app/App.tsx` | Modify — add `/about` route |
| `src/widgets/contextual-signals/ui/ContextualSignalsWidget.tsx` | Update link to `/settings?tab=watchlist` |
| `src/pages/market/ui/MarketPage.tsx` | Update link to `/settings?tab=watchlist` |
| `src/pages/pricing/ui/PricingPage.tsx` | Update portal `returnUrl` to `/settings?tab=billing` |
