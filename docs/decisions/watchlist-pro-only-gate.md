# Watchlist Pro-Only Gate + Admin Subscription Management

## Context

Three related problems solved together:

1. **Security gap** — Free-plan and expired-Pro users had unlimited read access to the watchlist,
   analysis, and signals endpoints. All three are now Pro-only reads.

2. **Zero upsell on blocked features** — Free users hitting the watchlist area saw nothing.
   Replaced with `ProFeatureLock` components that render a blurred mock with an upgrade overlay.

3. **Manual activation required direct DB access** — when a user paid via bank transfer, there
   was no UI to activate or renew their plan. An Admin tab in Settings now handles this.

Stripe auto-renewal is out of scope (handled by existing webhooks). This covers manual
bank-transfer activation, soft expiry, and the Pro-only read gate.

---

## Part A — Backend: Read-access gate + soft expiry

### A1. Soft expiry helper

**File:** `backend/src/FinTrackPro.Infrastructure/Services/SubscriptionLimitService.cs`

```csharp
private static bool IsProActive(AppUser user) =>
    user.Plan == SubscriptionPlan.Pro &&
    (!user.SubscriptionExpiresAt.HasValue || user.SubscriptionExpiresAt.Value > DateTime.UtcNow);
```

Used by both the read gate and the refactored `GetLimits` check.

### A2. `EnforceWatchlistReadAccessAsync`

**Interface:** `backend/src/FinTrackPro.Application/Common/Interfaces/ISubscriptionLimitService.cs`

```csharp
Task EnforceWatchlistReadAccessAsync(AppUser user, CancellationToken ct = default);
```

**Implementation:**
```csharp
public Task EnforceWatchlistReadAccessAsync(AppUser user, CancellationToken ct = default)
{
    if (IsUnlimited()) return Task.CompletedTask;
    if (IsProActive(user)) return Task.CompletedTask;

    throw new PlanLimitExceededException("watchlist",
        "Watchlist and trading signals are available on the Pro plan.");
}
```

`IsUnlimited()` returns true when the admin override is configured (e.g. in development). Free users
and Pro users with a lapsed `SubscriptionExpiresAt` both throw, producing HTTP 402 with
`feature: "watchlist"`.

### A3. Wired into three read handlers

`ISubscriptionLimitService` injected; `EnforceWatchlistReadAccessAsync` called at the top of
`Handle` in:

| Handler | File |
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

Setting `WatchlistSymbolLimit: 0` means Free users cannot add symbols via the write gate either —
consistent with the read gate. Pro limit remains 20.

---

## Part B — Backend: Admin subscription management

### B1. Domain — `AppUser.RenewSubscription`

**File:** `backend/src/FinTrackPro.Domain/Entities/AppUser.cs`

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

Extends from the existing expiry when still active, from now when expired or null. The
`bank_` prefix distinguishes manual bank-transfer activations from Stripe-issued IDs.

**File:** `backend/src/FinTrackPro.Domain/Enums/BillingPeriod.cs` (new)

```csharp
public enum BillingPeriod { Monthly = 0, Yearly = 1 }
```

### B2. Application — CQRS commands

New folder: `backend/src/FinTrackPro.Application/Admin/`

| Command / Query | Input | Action |
|---|---|---|
| `AdminActivateSubscriptionCommand` | `Guid UserId`, `BillingPeriod Period` | Loads user → `RenewSubscription(Period)` → saves → returns `SubscriptionStatusDto` |
| `AdminRevokeSubscriptionCommand` | `Guid UserId` | Loads user → `CancelSubscription()` → saves |
| `AdminGetUsersQuery` | `int Page`, `int PageSize`, `string? EmailFilter` | Returns paged `AdminUserDto` list |

`AdminUserDto` fields: `Id`, `Email`, `DisplayName`, `Plan`, `SubscriptionExpiresAt`, `IsActive`.

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

Non-admin users receive HTTP 403.

---

## Part C — Frontend: Admin tab in Settings

Admin subscription management lives as a **conditional tab inside the existing Settings page**
(`/settings?tab=admin`). The tab is only injected into the sidebar when `isAdmin === true`.

### C1. Settings page changes

**File:** `frontend/fintrackpro-ui/src/pages/settings/ui/SettingsPage.tsx`

`TabSlug` union extended with `'admin'`. `useSettingsTabs(isAdmin)` conditionally pushes the Admin
tab. Non-admins navigating directly to `?tab=admin` fall back to `'account'` via the existing
valid-slug guard.

### C2. `AdminSubscriptionPanel`

**File:** `frontend/fintrackpro-ui/src/features/admin-subscription/ui/AdminSubscriptionPanel.tsx`

Table: email search, paginated user list, `[+1m]` / `[+1y]` activate buttons, inline-confirm
`[Revoke]` button. Plan badge reuses the existing `PlanBadge` component.

### C3. API hooks

**File:** `frontend/fintrackpro-ui/src/entities/subscription/api/subscriptionApi.ts`

Three hooks added:
- `useAdminUsers(page, emailFilter)` — `GET /api/admin/users`
- `useAdminActivateSubscription()` — `POST /api/admin/users/{userId}/subscription`
- `useAdminRevokeSubscription()` — `DELETE /api/admin/users/{userId}/subscription`

Both mutations invalidate the `adminUsers` query on success.

---

## Part D — Frontend: `ProFeatureLock` upsell widget

### D1. `ProFeatureLock` component

**File:** `frontend/fintrackpro-ui/src/features/upgrade/ui/ProFeatureLock.tsx`

```typescript
interface ProFeatureLockProps {
  title: string
  tagline: string
  features: { icon: string; label: string }[]
  compact?: boolean
  children: React.ReactNode
}
```

Pro users (`plan === 'Pro' && isActive !== false`): renders `children` unchanged.

Free / expired Pro users:

- **Full variant** (default): `glass-card` + amber "Pro" badge; 3 blurred mock rows (`blur-sm`,
  `opacity-40`, `pointer-events-none`); overlay with lock icon, title, tagline, feature bullets,
  and `<UpgradeButton />`.
- **Compact variant** (`compact={true}`): single `glass-card` row with signal icon + tagline +
  `<UpgradeButton />`.

### D2. Gates applied

| Location | File | Variant |
|---|---|---|
| Watchlist + Analysis | `widgets/watchlist-analysis-widget/ui/WatchlistAnalysisWidget.tsx` (via `MarketPage`) | Full |
| Signals section | `pages/market/ui/MarketPage.tsx` | Full |
| `ContextualSignalsWidget` (dashboard) | `widgets/contextual-signals/ui/ContextualSignalsWidget.tsx` | Compact |
| Watchlist manager | `features/manage-watchlist/ui/WatchlistManager.tsx` | Full |

---

## Key decisions

| Decision | Rationale |
|---|---|
| Read gate in handler (not middleware) | Keeps enforcement co-located with business logic; consistent with existing write limits. |
| Soft expiry check in-process | No DB query needed — `SubscriptionExpiresAt` is already on the loaded `AppUser`. |
| `bank_` prefix on `PaymentSubscriptionId` | Makes manual vs. Stripe activations distinguishable in the DB and logs without a separate column. |
| Admin tab in Settings, not a separate route | Keeps nav clean; contextually adjacent to the existing Plan & Billing tab; `isAdmin` guard is already in the auth store. |
| `ProFeatureLock` wraps children | Pro users get zero overhead — existing JSX is returned unchanged; the lock is purely additive for Free users. |

---

## Out of scope (this phase)

- Stripe auto-renewal period selection — handled by existing webhooks
- `SubscriptionExpiryJob` (nightly auto-downgrade) — separate PR
- Renewal reminder notifications — separate PR
