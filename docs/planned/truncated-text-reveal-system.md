# TruncatedText Reveal System

## Context

Text is truncated with raw Tailwind classes (`truncate`, `whitespace-nowrap overflow-hidden text-ellipsis`) scattered across five sites in the codebase. Users on any device — mobile or desktop — have no mechanism to read the full content. This document describes the design and the implementation plan for a reusable `TruncatedText` component system that reveals full text on demand.

**Problem sites:**

| File | Line | Content |
|---|---|---|
| `widgets/signals-list/ui/SignalsList.tsx` | 52 | `signal.message` |
| `widgets/trending-coins-widget/ui/TrendingCoinsWidget.tsx` | 83 | `coin.name` (inline beside ticker) |
| `widgets/top-market-cap-widget/ui/TopMarketCapWidget.tsx` | ~88 | `coin.name` (inline beside ticker) |
| `widgets/recent-activity/ui/ActivityItem.tsx` | 45 | `item.label` |
| `pages/transactions/ui/TransactionsPage.tsx` | 203, 205 | category label + note |

---

## Decision

Implement three reveal patterns keyed on viewport width, orchestrated by a single `<TruncatedText>` component:

| Breakpoint | Trigger | Pattern |
|---|---|---|
| `< 768px` | tap | Bottom sheet slides up from bottom |
| `768–1279px` | tap | Anchored popover card |
| `≥ 1280px` | hover | Fade-in tooltip |

**Alternatives considered:**

- *Native `title` attribute* — no dark mode, no styling, not accessible on touch; rejected.
- *Always bottom sheet* — wastes space on desktop, poor UX for hover-capable devices; rejected.
- *Third-party tooltip library* — adds a dependency for a problem solvable in ~80 lines of Tailwind; rejected.

**Consequences:**

- Five raw-truncation sites become interactive with zero per-site logic.
- Three new shared primitives are added to `shared/ui/`.
- Two new hooks (`useClickOutside`, `useEscapeKey`) are added to `shared/lib/`.
- No external animation library dependency introduced.

---

## ASCII Wireframes

### Desktop — Hover Tooltip (≥ 1280px)

```
┌─────────────────────────────────────────────┐
│  Pudgy Pengu...  │  $0.31  │  -0.32%        │
│    ┌─────────────────────────────────────┐  │
│    │  Pudgy Penguin NFT Floor Price      │  │  ← opacity fade, z-50
│    └─────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
CSS: opacity-0 → opacity-100  duration-200 ease-out
Rendered via ReactDOM.createPortal to avoid overflow-x-auto clipping in grid containers.
```

### Tablet — Tap Popover (768–1279px)

```
┌─────────────────────────────────────────────┐
│  Pudgy Pengu...  │  $0.31  │  -0.32%        │
│  ┌───────────────────────────────────────┐  │
│  │  Pudgy Penguin NFT Floor Price   [×]  │  │  ← scale+opacity, anchored
│  └───────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
CSS: opacity-0 scale-95 → opacity-100 scale-100  duration-200 ease-out
Close: click outside (useClickOutside), Escape (useEscapeKey), [×] button.
```

### Mobile — Bottom Sheet (< 768px)

```
┌─────────────────────────────────────────────┐
│         [dimmed content above]              │
│▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓│  ← bg-black/40 backdrop
├─────────────────────────────────────────────┤
│               ──────                        │  ← drag handle pill
│  Full text                             [×]  │
│  Pudgy Penguin NFT Floor Price              │
│  displayed here without truncation.         │
└─────────────────────────────────────────────┘
CSS: translate-y-full → translate-y-0  duration-300 ease-out
Close: backdrop tap, Escape (useEscapeKey), [×] button.
```

---

## Architecture

```
shared/lib/useClickOutside.ts       ← new hook
shared/lib/useEscapeKey.ts          ← new hook
shared/ui/Tooltip.tsx               ← new primitive (portal-based)
shared/ui/BottomSheet.tsx           ← new primitive
shared/ui/TruncatedText.tsx         ← orchestrator
shared/ui/index.ts                  ← add 3 exports
shared/i18n/en.ts                   ← add common.close + truncatedText.*
shared/i18n/vi.ts                   ← same keys in Vietnamese
```

FSD constraint: `shared/` is importable by all layers above it. Widgets and pages importing from `shared/ui/` is already the established pattern (`ConfirmDeleteDialog`, `Pagination`, `DataFreshnessBadge`).

---

## Component Interfaces

### `useClickOutside`

```typescript
function useClickOutside<T extends HTMLElement = HTMLElement>(
  ref: React.RefObject<T | null>,
  handler: (event: MouseEvent | TouchEvent) => void,
  enabled?: boolean,   // default true — set false to pause without removing the hook
): void
```

Listens on `document` for `mousedown` + `touchstart`. Guards `enabled` to avoid conditional hook rules at call sites.

### `useEscapeKey`

```typescript
function useEscapeKey(
  handler: () => void,
  enabled?: boolean,   // default true
): void
```

Listens on `document` for `keydown`, fires when `event.key === 'Escape'` and `enabled`.

### `Tooltip`

```typescript
interface TooltipProps {
  content: string
  children: React.ReactNode
  className?: string
}
```

Hover-driven. Renders the tooltip via `ReactDOM.createPortal(tooltipEl, document.body)`, positioned with `getBoundingClientRect` on the trigger ref. This avoids clipping by `overflow-x-auto` scroll containers in `TrendingCoinsWidget` and `TopMarketCapWidget`.

### `BottomSheet`

```typescript
interface BottomSheetProps {
  open: boolean
  onClose: () => void
  title?: string
  children: React.ReactNode
}
```

Both backdrop and panel stay mounted (CSS transition needs the element in the DOM for slide-out). Uses `pointer-events-none` on the closed backdrop to suppress interaction. Calls `useEscapeKey(onClose, open)`.

### `TruncatedText` (orchestrator)

```typescript
interface TruncatedTextProps {
  text: string
  maxChars?: number    // default 40
  className?: string
  as?: 'p' | 'span'   // default 'span'
}
```

**Breakpoint state** — initialized from `getBreakpoint()` (calls `window.matchMedia`) and updated via resize listeners. Safe: app is Vite SPA, no SSR.

**Render logic:**
1. `text.length <= maxChars` → render plain text, no overhead
2. `breakpoint === 'desktop'` → `<Tooltip content={text}>` wrapping truncated span
3. `breakpoint === 'tablet'` → truncated span + absolute popover + `useClickOutside` + `useEscapeKey`
4. `breakpoint === 'mobile'` → truncated span + `<BottomSheet>`

Truncated span always has `role="button"`, `tabIndex={0}`, `onKeyDown` for Enter/Space.

---

## CSS Transition Specifications

| Component | Property | Closed | Open | Duration | Easing |
|---|---|---|---|---|---|
| Tooltip | opacity | `opacity-0` | `opacity-100` | `duration-200` | `ease-out` |
| Popover | opacity + scale | `opacity-0 scale-95` | `opacity-100 scale-100` | `duration-200` | `ease-out` |
| BottomSheet panel | translateY | `translate-y-full` | `translate-y-0` | `duration-300` | `ease-out` |
| BottomSheet backdrop | opacity | `opacity-0 pointer-events-none` | `opacity-100 pointer-events-auto` | `duration-300` | `ease-out` |

---

## Dark Mode

All new components follow the existing `ConfirmDeleteDialog` color palette:

| Surface | Light | Dark |
|---|---|---|
| Sheet / popover background | `bg-white` | `dark:bg-[#161a25]` |
| Sheet / popover border | `border-gray-200` | `dark:border-white/10` |
| Sheet / popover text | `text-gray-800` | `dark:text-slate-200` |
| Drag handle pill | `bg-gray-300` | `dark:bg-slate-600` |
| Tooltip background | `bg-gray-900` | `dark:bg-slate-700` |
| Tooltip text | `text-white` | `dark:text-slate-100` |
| Backdrop | `bg-black/40` | same |

---

## i18n Keys

Add to `shared/i18n/en.ts`:

```typescript
common: {
  // ... existing keys ...
  close: 'Close',          // new
},
truncatedText: {           // new group
  dragHandle: 'Drag to dismiss',
  viewFullText: 'Full text',
},
```

Vietnamese (`vi.ts`): `close: 'Đóng'`, `dragHandle: 'Kéo để đóng'`, `viewFullText: 'Toàn bộ nội dung'`.

---

## Wire-Up Details Per Site

### Site 1 — `SignalsList.tsx:52`
```tsx
// Before
<p className="text-xs text-gray-500 dark:text-slate-400 truncate">{signal.message}</p>

// After
<TruncatedText text={signal.message} maxChars={60} className="text-xs text-gray-500 dark:text-slate-400" as="p" />
```

### Sites 2 & 3 — `TrendingCoinsWidget.tsx:83`, `TopMarketCapWidget.tsx:~88`
Coin name and ticker are siblings inside an `<a>` tag. Wrap `TruncatedText` in a `stopPropagation` span to prevent navigation on tap:
```tsx
<span className="flex items-center min-w-0 pr-2">
  <span onClick={(e) => e.stopPropagation()} className="min-w-0">
    <TruncatedText text={coin.name} maxChars={18} className="text-[13px] font-medium ..." />
  </span>
  <span className="ml-1.5 font-mono text-[11px] ... flex-shrink-0">
    {coin.symbol.toUpperCase()}
  </span>
</span>
```

### Site 4 — `ActivityItem.tsx:45`
```tsx
// Before
<p className="text-sm font-medium capitalize text-gray-800 dark:text-slate-200 truncate">{item.label}</p>

// After
<TruncatedText text={item.label} maxChars={30} className="text-sm font-medium capitalize text-gray-800 dark:text-slate-200" as="p" />
```

### Site 5 — `TransactionsPage.tsx:203, 205`
```tsx
// Before (line 203)
<p className="truncate text-sm font-medium">{resolveCategoryLabel(tx.category)}</p>
// After
<TruncatedText text={resolveCategoryLabel(tx.category)} maxChars={28} className="text-sm font-medium" as="p" />

// Before (line 205) — inside existing {tx.note && ...} guard
<p className="truncate text-xs text-gray-400 dark:text-slate-500">{tx.note}</p>
// After
<TruncatedText text={tx.note} maxChars={36} className="text-xs text-gray-400 dark:text-slate-500" as="p" />
```

---

## Implementation Plan

### Phase 1 — Shared Hooks
1. CREATE `src/shared/lib/useClickOutside.ts`
2. CREATE `src/shared/lib/useEscapeKey.ts`

### Phase 2 — i18n Keys
3. EDIT `src/shared/i18n/en.ts` — add `common.close` + `truncatedText` group
4. EDIT `src/shared/i18n/vi.ts` — mirror same keys in Vietnamese

### Phase 3 — UI Primitives
5. CREATE `src/shared/ui/Tooltip.tsx` — portal-based, hover, opacity fade
6. CREATE `src/shared/ui/BottomSheet.tsx` — `useEscapeKey`, slide-up transition
7. CREATE `src/shared/ui/TruncatedText.tsx` — orchestrator, breakpoint state, all three paths

### Phase 4 — Exports
8. EDIT `src/shared/ui/index.ts` — add `Tooltip`, `BottomSheet`, `TruncatedText`

### Phase 5 — Wire-Up
9. EDIT `widgets/signals-list/ui/SignalsList.tsx` — line 52, `maxChars={60}`
10. EDIT `widgets/trending-coins-widget/ui/TrendingCoinsWidget.tsx` — line 83, `maxChars={18}`, `stopPropagation` wrapper
11. EDIT `widgets/top-market-cap-widget/ui/TopMarketCapWidget.tsx` — same as step 10
12. EDIT `widgets/recent-activity/ui/ActivityItem.tsx` — line 45, `maxChars={30}`
13. EDIT `pages/transactions/ui/TransactionsPage.tsx` — lines 203 (`maxChars={28}`) and 205 (`maxChars={36}`)

---

## Verification Checklist

- [ ] `npm run build` passes with no TypeScript errors
- [ ] `npm run lint` passes clean
- [ ] **Desktop**: hovering a truncated coin name or signal shows tooltip; moving mouse away hides it
- [ ] **Tablet** (768–1279px DevTools): tapping truncated text shows anchored popover; clicking outside, Escape, or [×] closes it
- [ ] **Mobile** (< 768px DevTools): tapping truncated text opens bottom sheet; backdrop tap, Escape, and [×] all close it
- [ ] Dark mode: all three components render correctly
- [ ] Text shorter than `maxChars` renders as plain text with no cursor change or interaction wrapper
- [ ] Coin name sites: tapping name shows reveal UI; tapping elsewhere on the row still navigates (link intact)
- [ ] `TransactionsPage`: note field only renders `TruncatedText` when `tx.note` is truthy (existing guard preserved)
