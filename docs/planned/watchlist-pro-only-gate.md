# Watchlist Pro-Only Gate + Upsell Ad Banners + Admin Subscription Management

## Context

Three related problems solved together:

1. **Security gap** — Free-plan and expired-Pro users currently have unlimited read access to
   expensive watchlist/signals endpoints. These become Pro-only.

2. **Zero upsell on blocked features** — when Free users hit the watchlist area they see nothing.
   Replace with a rich ad block describing exactly what they're missing.

3. **Manual activation requires direct DB access** — when a user pays via bank transfer, the admin
   currently has no UI to activate or renew their plan. A dedicated admin page eliminates
   raw DB queries and makes it impossible to forget to set `SubscriptionExpiresAt`.

Stripe auto-renewal is out of scope for this phase (handled by existing webhooks when Stripe is
configured). This plan covers manual bank-transfer activation and the Pro-only gating.

---

## Part A — Backend: Read-access gate + soft expiry

### A1. Soft expiry helper

**File:** `backend/src/FinTrackPro.Infrastructure/Services/SubscriptionLimitService.cs`

```csharp
private static bool IsProActive(AppUser user) =>
    user.Plan == SubscriptionPlan.Pro &&
    (!user.SubscriptionExpiresAt.HasValue || user.SubscriptionExpiresAt.Value > DateTime.UtcNow);
```

Used by both the new read gate and refactored `GetLimits`.

### A2. New `EnforceWatchlistReadAccessAsync`

**Interface:** `backend/src/FinTrackPro.Application/Common/Interfaces/ISubscriptionLimitService.cs`

```csharp
Task EnforceWatchlistReadAccessAsync(AppUser user, CancellationToken ct = default);
```

**Implementation** (pure plan check — no DB query):
```csharp
public Task EnforceWatchlistReadAccessAsync(AppUser user, CancellationToken ct = default)
{
    if (IsUnlimited()) return Task.CompletedTask;
    if (IsProActive(user)) return Task.CompletedTask;

    throw new PlanLimitExceededException("watchlist",
        "Watchlist and trading signals are available on the Pro plan.");
}
```

### A3. Wire into three ungated read handlers

Add `ISubscriptionLimitService` injection + `EnforceWatchlistReadAccessAsync` call at the top of `Handle` in:

| Handler | Path |
|---------|------|
| `GetWatchedSymbolsQueryHandler` | `Application/Trading/Queries/GetWatchedSymbols/` |
| `GetWatchlistAnalysisQueryHandler` | `Application/Trading/Queries/GetWatchlistAnalysis/` |
| `GetSignalsQueryHandler` | `Application/Signals/Queries/GetSignals/` |

### A4. Config

**File:** `backend/src/FinTrackPro.API/appsettings.json`

```json
"SubscriptionPlans": {
  "Free": { "WatchlistSymbolLimit": 0 }
}
```

---

## Part B — Backend: Admin subscription management endpoints

### B1. Domain — extend `AppUser`

**File:** `backend/src/FinTrackPro.Domain/Entities/AppUser.cs`

Add `RenewSubscription` to extend from existing expiry (avoids penalising late renewals):

```csharp
public void RenewSubscription(BillingPeriod period)
{
    var baseDate = SubscriptionExpiresAt.HasValue && SubscriptionExpiresAt.Value > DateTime.UtcNow
        ? SubscriptionExpiresAt.Value
        : DateTime.UtcNow;

    var newExpiry = period == BillingPeriod.Yearly
        ? baseDate.AddYears(1)
        : baseDate.AddMonths(1);

    Plan                  = SubscriptionPlan.Pro;
    PaymentSubscriptionId = $"bank_{Guid.NewGuid()}";
    SubscriptionExpiresAt = newExpiry;
}
```

**File:** `backend/src/FinTrackPro.Domain/Enums/BillingPeriod.cs` (new)

```csharp
public enum BillingPeriod { Monthly = 0, Yearly = 1 }
```

### B2. Application — CQRS commands

New folder: `backend/src/FinTrackPro.Application/Admin/`

**`AdminActivateSubscriptionCommand`** + handler:
- Input: `Guid UserId`, `BillingPeriod Period`
- Load user by Id → call `user.RenewSubscription(Period)` → save
- Return `SubscriptionStatusDto`

**`AdminRevokeSubscriptionCommand`** + handler:
- Input: `Guid UserId`
- Load user → call `user.CancelSubscription()` → save

**`AdminGetUsersQuery`** + handler:
- Input: `int Page`, `int PageSize`, `string? EmailFilter`
- Returns paged list of `AdminUserDto` with: `Id`, `Email`, `DisplayName`, `Plan`, `SubscriptionExpiresAt`, `IsActive`

### B3. API — Admin controller

**File:** `backend/src/FinTrackPro.API/Controllers/AdminSubscriptionController.cs`

```csharp
[Authorize(Roles = UserRole.Admin)]
[Route("api/admin")]
public class AdminSubscriptionController : BaseApiController
{
    [HttpGet("users")]
    public Task<ActionResult<PagedResult<AdminUserDto>>> GetUsers(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? email = null)

    [HttpPost("users/{userId:guid}/subscription")]
    public Task<ActionResult<SubscriptionStatusDto>> Activate(
        Guid userId, [FromBody] AdminActivateSubscriptionCommand command)

    [HttpDelete("users/{userId:guid}/subscription")]
    public Task<ActionResult> Revoke(Guid userId)
}
```

---

## Part C — Frontend: Admin tab in Settings page

Admin subscription management lives as a **conditional tab inside the existing Settings page**
(`/settings?tab=admin`), not as a separate route. The tab is only injected into the sidebar
when `isAdmin === true` — regular users never see it. This keeps the navbar clean and places
admin billing controls contextually adjacent to the existing "Plan & Billing" tab.

```
Settings sidebar (admin user)           Settings sidebar (regular user)

  Account                                 Account
  Plan & Billing                          Plan & Billing
  Notifications                           Notifications
  Categories                              Categories
  Watchlist                               Watchlist
  ──────────────
  ⚙ Admin           ← admin-only
```

### C1. Settings page — add conditional admin tab

**File:** `frontend/fintrackpro-ui/src/pages/settings/ui/SettingsPage.tsx`

Extend `TabSlug` union and `useSettingsTabs()`:

```tsx
type TabSlug = 'account' | 'billing' | 'notifications' | 'categories' | 'watchlist' | 'admin'

function useSettingsTabs(isAdmin: boolean) {
  const { t } = useTranslation()
  const tabs = [
    { slug: 'account'       as TabSlug, label: t('settings.account')          },
    { slug: 'billing'       as TabSlug, label: t('settings.billing')           },
    { slug: 'notifications' as TabSlug, label: t('settings.notifications_tab') },
    { slug: 'categories'    as TabSlug, label: t('settings.categories_tab')    },
    { slug: 'watchlist'     as TabSlug, label: t('settings.watchlist_tab')     },
  ]
  if (isAdmin) tabs.push({ slug: 'admin', label: '⚙ Admin' })
  return tabs
}
```

Add tab content panel (both desktop sidebar and mobile strip):
```tsx
{activeTab === 'admin' && isAdmin && <AdminSubscriptionPanel />}
```

If a non-admin manually navigates to `?tab=admin`, the valid-slug guard already falls back to
`'account'` — no extra protection needed.

### C2. New admin panel component

**File (create):** `frontend/fintrackpro-ui/src/features/admin-subscription/ui/AdminSubscriptionPanel.tsx`

Layout:
```
[ Search by email...                              🔍 ]

┌──────────────────┬────────┬──────────────┬───────────────────────┐
│ User             │ Plan   │ Expires      │ Actions               │
├──────────────────┼────────┼──────────────┼───────────────────────┤
│ alice@example.co │ [Pro]  │ 2025-05-01   │ [+1m] [+1y] [Revoke] │
│ bob@example.com  │ [Free] │ —            │ [+1m] [+1y]           │
└──────────────────┴────────┴──────────────┴───────────────────────┘

← Prev   Page 1 of 3   Next →
```

- `[+1m]` / `[+1y]` call `POST /api/admin/users/{id}/subscription`
- `[Revoke]` shows an inline confirmation chip before calling `DELETE`
- Plan badge reuses existing `PlanBadge` component
- Component lives in `features/admin-subscription/` per FSD

### C3. Entities / API hooks

**File:** `frontend/fintrackpro-ui/src/entities/subscription/api/subscriptionApi.ts` (extend)

Add:
- `useAdminUsers(page, emailFilter)` — `GET /api/admin/users`
- `useAdminActivateSubscription()` — `POST /api/admin/users/{userId}/subscription`
- `useAdminRevokeSubscription()` — `DELETE /api/admin/users/{userId}/subscription`

Both mutations invalidate the `adminUsers` query on success.

---

## Part D — Frontend: `ProFeatureLock` upsell widget

### D1. New component

**File (create):** `frontend/fintrackpro-ui/src/features/upgrade/ui/ProFeatureLock.tsx`

Wrapper: renders `children` for Pro users; locked shell for Free/expired.

**Props:**
```typescript
interface ProFeatureLockProps {
  title: string
  tagline: string
  features: { icon: string; label: string }[]
  compact?: boolean
  children: React.ReactNode
}
```

**Full variant** (Market page):
- `glass-card` + amber "Pro" badge in header
- 3 blurred mock data rows (`blur-sm`, `opacity-40`, `pointer-events-none`)
- Overlay: lock icon, title, tagline, feature bullets, `<UpgradeButton />`

**Compact variant** (`compact={true}`) — dashboard:
- Single `glass-card` row: signal icon + tagline + `<UpgradeButton />`

### D2. Export

**File:** `frontend/fintrackpro-ui/src/features/upgrade/index.ts`

```ts
export { ProFeatureLock } from './ui/ProFeatureLock'
```

### D3. Gate `WatchlistAnalysisWidget`

**File:** `frontend/fintrackpro-ui/src/widgets/watchlist-analysis-widget/ui/WatchlistAnalysisWidget.tsx`

Wrap entire return:
```tsx
<ProFeatureLock
  title="My Watchlist — Analysis"
  tagline="Track your favourite symbols with multi-timeframe RSI analysis and live price data."
  features={[
    { icon: '📈', label: 'RSI across 1h, 4h, daily & weekly timeframes' },
    { icon: '💰', label: 'Live price & 24 h change' },
    { icon: '🔗', label: 'Direct Binance trade links' },
  ]}
>
  {/* existing JSX — unchanged */}
</ProFeatureLock>
```

### D4. Gate signals section on `MarketPage`

**File:** `frontend/fintrackpro-ui/src/pages/market/ui/MarketPage.tsx`

```tsx
<ProFeatureLock
  title="Recent Trading Signals"
  tagline="Automated signals generated from your watchlist — RSI extremes, EMA crosses, BB squeezes."
  features={[
    { icon: '🔴', label: 'RSI oversold / overbought alerts' },
    { icon: '⚡', label: 'EMA cross & Bollinger Band squeeze detection' },
    { icon: '📊', label: 'Volume spike alerts' },
  ]}
>
  <div>
    <h2 className="text-lg font-semibold mb-3">{t('dashboard.recentSignals')}</h2>
    <SignalsList count={20} />
  </div>
</ProFeatureLock>
```

### D5. Update `ContextualSignalsWidget` (dashboard)

**File:** `frontend/fintrackpro-ui/src/widgets/contextual-signals/ui/ContextualSignalsWidget.tsx`

Free users see compact teaser instead of `null`:
```tsx
const isPro = status?.plan === 'Pro' && status?.isActive !== false
if (!isPro) {
  return (
    <ProFeatureLock compact
      title="Trading Signals"
      tagline="Upgrade to Pro to get automated trading signals for your watchlist symbols."
      features={[]}
    >{null}</ProFeatureLock>
  )
}
// Pro users: existing behaviour unchanged
```

---

## File Change Summary

| File | Action |
|------|--------|
| `Domain/Enums/BillingPeriod.cs` | **Create** |
| `Domain/Entities/AppUser.cs` | Add `RenewSubscription(BillingPeriod)` |
| `Application/Common/Interfaces/ISubscriptionLimitService.cs` | Add `EnforceWatchlistReadAccessAsync` |
| `Infrastructure/Services/SubscriptionLimitService.cs` | Add `IsProActive` + implement new method |
| `Application/Trading/Queries/GetWatchedSymbols/GetWatchedSymbolsQueryHandler.cs` | Add enforce call |
| `Application/Trading/Queries/GetWatchlistAnalysis/GetWatchlistAnalysisQueryHandler.cs` | Add enforce call |
| `Application/Signals/Queries/GetSignals/GetSignalsQueryHandler.cs` | Add enforce call |
| `Application/Admin/AdminActivateSubscriptionCommand` + handler | **Create** |
| `Application/Admin/AdminRevokeSubscriptionCommand` + handler | **Create** |
| `Application/Admin/AdminGetUsersQuery` + handler + `AdminUserDto` | **Create** |
| `API/Controllers/AdminSubscriptionController.cs` | **Create** |
| `API/appsettings.json` | Set `Free.WatchlistSymbolLimit: 0` |
| `entities/subscription/api/subscriptionApi.ts` | Add 3 admin hooks |
| `features/upgrade/ui/ProFeatureLock.tsx` | **Create** |
| `features/upgrade/index.ts` | Export `ProFeatureLock` |
| `pages/admin-subscriptions/ui/AdminSubscriptionsPage.tsx` | **Create** |
| `app/App.tsx` | Add `/admin/subscriptions` route with admin guard |
| `widgets/watchlist-analysis-widget/ui/WatchlistAnalysisWidget.tsx` | Wrap with `ProFeatureLock` |
| `pages/market/ui/MarketPage.tsx` | Wrap signals section with `ProFeatureLock` |
| `widgets/contextual-signals/ui/ContextualSignalsWidget.tsx` | Add compact teaser for Free users |

---

## Out of Scope (this phase)

- Stripe auto-renewal period selection (monthly/yearly price IDs) — handled by existing webhooks
- `SubscriptionExpiryJob` (nightly auto-downgrade) — separate PR
- Renewal reminder notifications — separate PR

---

## Verification

**Backend:**
- `dotnet build` — no errors
- `dotnet test --filter "Category!=Integration"` — passes
- `GET /api/watchlist/analysis` as Free user → `402 feature: "watchlist"`
- `GET /api/signals` as Free user → `402 feature: "watchlist"`
- Same endpoints as Pro user → `200`
- `POST /api/admin/users/{id}/subscription` `{ period: "Monthly" }` → user.Plan = Pro,
  SubscriptionExpiresAt = max(existingExpiry, now) + 1 month
  (extends from existing expiry if still active; from now if expired or null)
- `DELETE /api/admin/users/{id}/subscription` → user.Plan = Free

**Frontend:**
- `npm run build` — no TypeScript errors
- Free user on Market page → `ProFeatureLock` shown for Watchlist and Signals with blurred rows
- Dashboard → compact teaser shown in `ContextualSignalsWidget` slot instead of empty
- Pro user → all widgets render normally
- Expired Pro (set DB manually) → soft expiry triggers lock immediately
- `/admin/subscriptions` only accessible to Admin role users
- Admin can activate Monthly / Yearly for a bank-transfer user and revoke Pro

---

## Part E — Test Requirements

All items below must be implemented alongside the feature code. No item should be marked
complete until its corresponding test(s) exist and pass.

---

### E1. Backend — Domain unit tests

**File:** `backend/tests/FinTrackPro.Domain.UnitTests/Users/AppUserTests.cs` (extend)

| Test | Assertion |
|---|---|
| `RenewSubscription_Monthly_FreeUser_SetsPlanProAndExpiryNowPlusOneMonth` | Plan = Pro; ExpiresAt ≈ UtcNow + 1 month |
| `RenewSubscription_Monthly_ActivePro_ExtendsFromExistingExpiry` | ExpiresAt = existingExpiry + 1 month (not from now) |
| `RenewSubscription_Monthly_ExpiredPro_ExtendsFromNow` | ExpiresAt ≈ UtcNow + 1 month |
| `RenewSubscription_Yearly_FreeUser_SetsPlanProAndExpiryNowPlusOneYear` | Plan = Pro; ExpiresAt ≈ UtcNow + 1 year |
| `RenewSubscription_Yearly_ActivePro_ExtendsFromExistingExpiry` | ExpiresAt = existingExpiry + 1 year |
| `RenewSubscription_SetsPaymentSubscriptionIdWithBankPrefix` | PaymentSubscriptionId starts with `"bank_"` |

---

### E2. Backend — Infrastructure unit tests

**File:** `backend/tests/FinTrackPro.Infrastructure.UnitTests/Services/SubscriptionLimitServiceTests.cs` (extend)

| Test | Assertion |
|---|---|
| `EnforceWatchlistReadAccess_FreeUser_ThrowsPlanLimitExceededException_WithFeatureWatchlist` | Throws `PlanLimitExceededException` with `Feature == "watchlist"` |
| `EnforceWatchlistReadAccess_ActiveProUser_DoesNotThrow` | No exception |
| `EnforceWatchlistReadAccess_ExpiredProUser_ThrowsPlanLimitExceededException` | Pro user with `SubscriptionExpiresAt` in the past → throws |
| `EnforceWatchlistReadAccess_Admin_DoesNotThrow` | `isAdmin: true` → always passes regardless of plan |

---

### E3. Backend — Application unit tests (handler gate enforcement)

**File:** `backend/tests/FinTrackPro.Application.UnitTests/Trading/GetWatchedSymbolsHandlerTests.cs` (extend)

Requires `ISubscriptionLimitService` injected into `GetWatchedSymbolsQueryHandler`.

| Test | Assertion |
|---|---|
| `Handle_FreeUser_CallsEnforceWatchlistReadAccess_AndThrows` | `EnforceWatchlistReadAccessAsync` called; exception propagates to controller |
| `Handle_ProUser_PassesThroughToRepository` | No exception; symbols returned |

Apply the same two tests to:
- `GetWatchlistAnalysisQueryHandlerTests.cs`
- `GetSignalsHandlerTests.cs`

---

### E4. Backend — Application unit tests (admin commands)

**New file:** `backend/tests/FinTrackPro.Application.UnitTests/Admin/AdminActivateSubscriptionHandlerTests.cs`

| Test | Assertion |
|---|---|
| `Handle_FreeUser_Monthly_SetsPlanProAndCorrectExpiry` | user.Plan = Pro; ExpiresAt ≈ now + 1 month |
| `Handle_ActiveProUser_Monthly_ExtendsFromExistingExpiry` | ExpiresAt = old expiry + 1 month |
| `Handle_ExpiredProUser_Monthly_ExtendsFromNow` | ExpiresAt ≈ now + 1 month |
| `Handle_Yearly_SetsExpiryOneYearOut` | ExpiresAt ≈ now + 1 year (or existing + 1 year) |
| `Handle_UserNotFound_ThrowsNotFoundException` | `NotFoundException` |

**New file:** `backend/tests/FinTrackPro.Application.UnitTests/Admin/AdminRevokeSubscriptionHandlerTests.cs`

| Test | Assertion |
|---|---|
| `Handle_ProUser_SetsPlanToFreeAndClearsExpiry` | user.Plan = Free; ExpiresAt = null |
| `Handle_UserNotFound_ThrowsNotFoundException` | `NotFoundException` |

**New file:** `backend/tests/FinTrackPro.Application.UnitTests/Admin/AdminGetUsersHandlerTests.cs`

| Test | Assertion |
|---|---|
| `Handle_ReturnsPagedListOfUsers` | Returns correct page + totalCount |
| `Handle_EmailFilter_ReturnsOnlyMatchingUsers` | Only users matching partial email returned |

---

### E5. Backend — Integration tests

**File:** `backend/tests/FinTrackPro.Api.IntegrationTests/Features/Trading/WatchedSymbolsTests.cs` (extend)

| Test | Assertion |
|---|---|
| `GetWatchedSymbols_FreeUser_Returns402WithFeatureWatchlist` | Status 402; `feature` = `"watchlist"` in body |
| `GetWatchlistAnalysis_FreeUser_Returns402WithFeatureWatchlist` | Status 402; `feature` = `"watchlist"` in body |
| `GetSignals_FreeUser_Returns402WithFeatureWatchlist` | Status 402; `feature` = `"watchlist"` in body |

**New file:** `backend/tests/FinTrackPro.Api.IntegrationTests/Features/Admin/AdminSubscriptionTests.cs`

| Test | Assertion |
|---|---|
| `GetUsers_AsAdmin_Returns200WithPaginatedList` | Status 200; `items` array; pagination fields present |
| `GetUsers_AsUser_Returns403` | Status 403 |
| `ActivateSubscription_Monthly_AsAdmin_Returns200AndPlanIsPro` | Status 200; `plan = "Pro"` |
| `ActivateSubscription_AsUser_Returns403` | Status 403 |
| `RevokeSubscription_AsAdmin_Returns204AndPlanIsFree` | Status 204; follow-up GET status → `plan = "Free"` |

---

### E6. Postman E2E — `FinTrackPro.e2e.postman_collection.json`

Add two new folders **after** the current `Market` folder. These should be added when the endpoints
go live; do not add them before or the CI gate will fail.

**Folder: "Watchlist Pro Gate"** (uses `bearerToken2` — Free-plan user)

| Request | Method | Endpoint | Expected | Key assertion |
|---|---|---|---|---|
| Free user: GET watchedsymbols | GET | `/api/watchedsymbols` | 402 | `feature` = `"watchlist"` in body |
| Free user: GET analysis | GET | `/api/watchedsymbols/analysis` | 402 | `feature` = `"watchlist"` |
| Free user: GET signals | GET | `/api/signals` | 402 | `feature` = `"watchlist"` |

> Requires `user2@fintrackpro.dev` to have Free plan (the default — no activation needed).

**Folder: "Admin Subscription Management"** (uses `bearerToken` — Admin user)

| Request | Method | Endpoint | Expected | Key assertion |
|---|---|---|---|---|
| GET /api/admin/users | GET | `/api/admin/users` | 200 | `items` array; `id`, `email`, `plan`, `subscriptionExpiresAt` fields present |
| GET /api/admin/users as user2 → 403 | GET | `/api/admin/users` (bearerToken2) | 403 | Status 403 |
| Activate Monthly for user2 | POST | `/api/admin/users/{{user2Id}}/subscription` `{ "period": "Monthly" }` | 200 | `plan = "Pro"`; `expiresAt` ≈ now + 1 month |
| Activate again → extends expiry | POST | same | 200 | new `expiresAt` > previous `expiresAt` |
| Revoke user2 subscription | DELETE | `/api/admin/users/{{user2Id}}/subscription` | 204 | — |
| Verify user2 back to Free | GET | `/api/admin/users?email=user2` | 200 | user2 row shows `plan = "Free"` |

> Capture `user2Id` via `GET /api/admin/users?email=user2@fintrackpro.dev` in the folder's
> pre-request script (same pattern as `foodCategoryId` fetch in the Spending Flow folder).

Also update `docs/postman/api-e2e-test-cases.md` when adding these:
- Increment totals line from `33 requests · 52 assertions`
- Add new rows to the Coverage Matrix for `Watched Symbols — Pro gate` and `Admin`

---

### E7. Frontend — unit tests

**New file:** `frontend/fintrackpro-ui/src/features/upgrade/ui/ProFeatureLock.test.tsx`

| Test | Assertion |
|---|---|
| Pro user — children rendered | Given `plan = "Pro"` + `isActive = true`, `children` content visible; no lock overlay |
| Free user — locked shell shown | Given `plan = "Free"`, lock overlay visible; `children` aria-hidden or blurred |
| Free user — compact variant | `compact={true}` renders single-row teaser, not full blurred card |
| Upgrade button present when locked | `UpgradeButton` rendered inside locked shell |

**File:** `frontend/fintrackpro-ui/src/entities/subscription/api/subscriptionApi.test.ts` (extend)

Add tests for the three new hooks:

| Hook | Test |
|---|---|
| `useAdminUsers` | GET `GET /api/admin/users` called with page + emailFilter; returns paged list |
| `useAdminActivateSubscription` | POST `/api/admin/users/{userId}/subscription` called with `{ period }` |
| `useAdminRevokeSubscription` | DELETE `/api/admin/users/{userId}/subscription` called; invalidates `adminUsers` query |

---

### E8. Documentation — update on implementation

When this feature ships, update the following files in the same PR:

| File | What to add |
|---|---|
| `docs/architecture/api-spec.md` | New **Admin** section: `GET /api/admin/users`, `POST /api/admin/users/{id}/subscription`, `DELETE /api/admin/users/{id}/subscription`; update `feature` values list to include `"watchlist"` read-access gate note |
| `docs/features.md` | New **section 9** — Admin Subscription Management (user list, +1m/+1y buttons, revoke); update section 4 (Crypto Watchlist) and section 5 (Market Signals) with Pro-only note and upsell description |
| `docs/postman/api-e2e-plan.md` | Add the two new folders to the collection structure diagram; add environment variable `user2Id` |
| `docs/postman/api-e2e-test-cases.md` | Increment totals; add rows + coverage matrix entries for the two new folders |
