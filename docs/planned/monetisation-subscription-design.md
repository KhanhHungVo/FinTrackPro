# Monetisation / Subscription System

## Context

FinTrackPro currently has no revenue model — all features are available to every authenticated
user at no cost. This document describes the design for a Freemium subscription system with
two initial plans (Free and Pro), payment-gateway-agnostic billing, and config-driven feature
limits that can be adjusted without code changes.

The payment gateway (Stripe by default) is selected via a single config key
(`PaymentGateway:Provider`) and sits entirely behind interfaces in the Application layer —
the same pattern used for IAM providers (`IdentityProvider:Provider`). Swapping to a
different billing provider (e.g. Paddle, LemonSqueezy) requires only a new Infrastructure
implementation and a config change; no Application or Domain code changes.

Key design constraints:
- **Roles ≠ Plans** — `User` / `Admin` roles stay in the IAM provider (Keycloak/Auth0). Plan
  (`Free` / `Pro`) is stored only in the DB on `AppUser`. The two are orthogonal.
- **Admin bypass** — users with the `Admin` IAM role are exempt from all limits regardless
  of their subscription plan.
- **Config-driven limits** — every per-tier limit lives in `appsettings.json` under
  `SubscriptionPlans`. Setting any value to `-1` means unlimited, enabling easy limit
  relaxation during development or launch without code deploys.
- **Extensible tiers** — adding a future `Premium` tier requires only extending the enum and
  adding a config section; no schema migration is needed.

---

## 1. System Architecture Overview

```
Domain:         SubscriptionPlan enum + AppUser subscription fields + PlanLimitExceededException
Application:    ISubscriptionLimitService + SubscriptionPlanOptions + Subscription CQRS
                IPaymentGatewayService + IPaymentWebhookHandler interfaces
Infrastructure: SubscriptionLimitService + StripePaymentGatewayService + StripeWebhookHandler
                CurrentUserService (IsAdmin)
                Repository count methods (ITransactionRepository, IBudgetRepository,
                ITradeRepository, IWatchedSymbolRepository)
API:            SubscriptionController (GET status, POST checkout, POST portal)
                PaymentWebhookController (POST /api/payment/webhook — AllowAnonymous)
                ExceptionHandlingMiddleware 402 mapping
Frontend:       entities/subscription/ + features/plan-badge/ + features/upgrade/
                pages/pricing/ + FreePlanAdBanner + paywall guards on forms
```

### Naming Convention

Follows the existing codebase pattern at every layer:

| Layer | Convention | Example |
|---|---|---|
| Domain enum | PascalCase | `SubscriptionPlan` |
| Domain exception | `{Name}Exception` | `PlanLimitExceededException` |
| App options class | `{Name}Options` | `SubscriptionPlanOptions` |
| App interface | `I{Name}Service` | `ISubscriptionLimitService`, `IPaymentGatewayService` |
| App folder | `Subscription/` | `Application/Subscription/` |
| Command/Query | `{Action}{Noun}Command` | `CreateCheckoutSessionCommand` |
| DTO | `{Noun}Dto` | `SubscriptionStatusDto` |
| Controller | `{Noun}Controller` | `SubscriptionController` |
| API route | kebab-case | `/api/subscription/status` |
| Frontend entity | kebab-case | `entities/subscription/` |
| React Query key | kebab-case | `['subscription-status']` |

**Key invariants:**
- Subscription state is always read from the DB — never from the JWT claim
- All limit checks throw `PlanLimitExceededException` (→ HTTP 402) with a `feature` field
  so the frontend can open a targeted upgrade modal
- Payment gateway webhooks are the single source of truth for plan activation/deactivation
- `AppUser.PaymentCustomerId` is retained on `CancelSubscription()` so users can re-subscribe
  without the payment provider creating duplicate customer records
- The active payment gateway is selected by `PaymentGateway:Provider` in config — mirrors
  the `IdentityProvider:Provider` pattern; no Application or Domain code changes needed to
  swap providers

---

## 2. Plan Limits

| Feature | Free | Pro |
|---|---|---|
| Transactions per month | 50 | unlimited |
| Transaction history | 90 days | unlimited |
| Active budgets | 3 | unlimited |
| Trades stored (total) | 20 | unlimited |
| Watchlist symbols | 3 | unlimited |
| Signal history view | 7 days | unlimited |
| Telegram notifications | disabled | enabled |
| Dashboard ads | shown | hidden |

All values are configurable in `appsettings.json`. Set any numeric limit to `-1` for
unlimited. Boolean `TelegramNotificationsEnabled` controls the Telegram paywall.

---

## 3. Configuration

### `appsettings.json` additions

```json
"SubscriptionPlans": {
  "Free": {
    "MonthlyTransactionLimit": 50,
    "TransactionHistoryDays": 90,
    "ActiveBudgetLimit": 3,
    "TotalTradeLimit": 20,
    "WatchlistSymbolLimit": 3,
    "SignalHistoryDays": 7,
    "TelegramNotificationsEnabled": false
  },
  "Pro": {
    "MonthlyTransactionLimit": -1,
    "TransactionHistoryDays": -1,
    "ActiveBudgetLimit": -1,
    "TotalTradeLimit": -1,
    "WatchlistSymbolLimit": -1,
    "SignalHistoryDays": -1,
    "TelegramNotificationsEnabled": true
  }
},
"PaymentGateway": {
  "Provider": "stripe",
  "PriceId": ""
},
"Stripe": {
  "SecretKey": "",
  "WebhookSecret": ""
}
```

> `PaymentGateway:PriceId` is the logical Pro plan price identifier — provider-neutral concept.
> `Stripe:SecretKey` and `Stripe:WebhookSecret` are provider-specific credentials that live in
> their own section (same pattern as `Keycloak:` / `Auth0:`).
> To add a future provider, add its own config section and set `PaymentGateway:Provider`.

> Dev/launch tip: override all Free limits to `-1` in `appsettings.Development.json` to run
> without any restrictions. No code changes required.

### Strongly-typed options

**`Application/Common/Options/SubscriptionPlanOptions.cs`** (Application layer — no infra dependency):
```csharp
public class PlanLimits {
    public int  MonthlyTransactionLimit      { get; init; }
    public int  TransactionHistoryDays       { get; init; }
    public int  ActiveBudgetLimit            { get; init; }
    public int  TotalTradeLimit              { get; init; }
    public int  WatchlistSymbolLimit         { get; init; }
    public int  SignalHistoryDays            { get; init; }
    public bool TelegramNotificationsEnabled { get; init; }
}
public class SubscriptionPlanOptions {
    public const string SectionName = "SubscriptionPlans";
    public PlanLimits Free { get; init; } = new();
    public PlanLimits Pro  { get; init; } = new();
    // Future: public PlanLimits Premium { get; init; } = new();
}
```

**`Application/Common/Options/PaymentGatewayOptions.cs`** (Application layer — provider-neutral):
```csharp
public class PaymentGatewayOptions {
    public const string SectionName = "PaymentGateway";
    public string Provider { get; init; } = "stripe";
    public string PriceId  { get; init; } = "";   // logical Pro plan price ID
}
```

**`Infrastructure/Stripe/StripeOptions.cs`** (Infrastructure layer — Stripe-specific credentials):
```csharp
public class StripeOptions {
    public const string SectionName = "Stripe";
    public string SecretKey     { get; init; } = "";
    public string WebhookSecret { get; init; } = "";
}
```

---

## 4. API Contract

### GET /api/subscription/status
Returns the current user's subscription state. Requires `[Authorize]`.

**Response 200:**
```json
{
  "plan": "Pro",
  "isActive": true,
  "expiresAt": "2027-04-06T00:00:00Z"
}
```

> `PaymentCustomerId` / `PaymentSubscriptionId` are internal implementation details and are
> intentionally excluded from the public API contract. The frontend never needs them — it
> redirects to provider-issued URLs returned by the checkout and portal endpoints.

### POST /api/subscription/checkout
Creates a Stripe Checkout session. Requires `[Authorize]`.

**Request:**
```json
{ "successUrl": "https://app.fintrackpro.dev/settings?subscribed=1", "cancelUrl": "https://app.fintrackpro.dev/pricing" }
```

**Response 200:**
```json
{ "sessionUrl": "https://checkout.stripe.com/pay/cs_test_..." }
```

### POST /api/subscription/portal
Creates a Stripe Customer Portal session for self-serve management. Requires `[Authorize]`.

**Request:**
```json
{ "returnUrl": "https://app.fintrackpro.dev/settings" }
```

**Response 200:**
```json
{ "portalUrl": "https://billing.stripe.com/session/..." }
```

### POST /api/payment/webhook
Receives payment gateway lifecycle events. `[AllowAnonymous]`. Signature verification is
delegated to `IPaymentWebhookHandler` — the controller has no provider-specific logic.
Returns `400` on invalid signature, `200` on successful processing.

Handled events (Stripe defaults — other providers map equivalent events):
- `customer.subscription.updated` / `invoice.payment_succeeded` → activate Pro
- `customer.subscription.deleted` / `invoice.payment_failed` → revert to Free

### 402 Plan Limit Error Response
When any limit is exceeded, the API returns:
```json
{
  "status": 402,
  "title": "Budget limit reached for your current plan.",
  "instance": "/api/budgets",
  "extensions": { "feature": ["budget"] }
}
```

The `feature` field identifies which limit was hit so the frontend can open a targeted
upgrade modal.

---

## 5. Migration Strategy

### Phase 1 — AppUser extension (implement now)
- Add `Plan`, `PaymentCustomerId`, `PaymentSubscriptionId`, `SubscriptionExpiresAt` to `AppUser`
- All existing users default to `Plan = Free` (DB default value `0`)
- Add index on `PaymentCustomerId` for webhook lookup performance
- Generate migration: `AddSubscriptionFieldsToAppUser`

### Phase 2 — Soft launch (config only, no code)
- Set all Free limits to `-1` in `appsettings.Development.json` / production env vars
- Validate full Stripe checkout + webhook flow in staging before enforcing limits

### Phase 3 — Enforcement
- Remove the `-1` overrides from production config
- Free tier limits are now active

---

## 6. Backend Implementation — File by File

### Domain
| File | Action |
|---|---|
| `Domain/Enums/SubscriptionPlan.cs` | **Create** — `Free = 0`, `Pro = 1` |
| `Domain/Entities/AppUser.cs` | **Modify** — add 4 subscription fields (`Plan`, `PaymentCustomerId`, `PaymentSubscriptionId`, `SubscriptionExpiresAt`) + `ActivateSubscription()` + `CancelSubscription()` + `SetPaymentCustomerId()` |
| `Domain/Exceptions/PlanLimitExceededException.cs` | **Create** — inherits `DomainException`, carries `string Feature` |
| `Domain/Repositories/IUserRepository.cs` | **Modify** — add `GetByPaymentCustomerIdAsync` |
| `Domain/Repositories/ITransactionRepository.cs` | **Modify** — add `CountByUserAndMonthAsync` |
| `Domain/Repositories/IBudgetRepository.cs` | **Modify** — add `CountByUserAndMonthAsync` |
| `Domain/Repositories/ITradeRepository.cs` | **Modify** — add `CountByUserAsync` |
| `Domain/Repositories/IWatchedSymbolRepository.cs` | **Modify** — add `CountByUserAsync` |

### Application
| File | Action |
|---|---|
| `Application/Common/Options/SubscriptionPlanOptions.cs` | **Create** — `PlanLimits` + `SubscriptionPlanOptions` |
| `Application/Common/Options/PaymentGatewayOptions.cs` | **Create** — `Provider` + `PriceId`; provider-neutral |
| `Application/Common/Interfaces/ICurrentUserService.cs` | **Modify** — add `bool IsAdmin { get; }` |
| `Application/Common/Interfaces/ISubscriptionLimitService.cs` | **Create** — 7 `Enforce*Async` methods |
| `Application/Common/Interfaces/IPaymentGatewayService.cs` | **Create** — `CreateCustomerAsync`, `CreateCheckoutSessionAsync`, `CreateBillingPortalSessionAsync` |
| `Application/Common/Interfaces/IPaymentWebhookHandler.cs` | **Create** — `HandleAsync(payload, signature, ct)` → `PaymentWebhookResult` |
| `Application/Subscription/Queries/GetSubscriptionStatus/GetSubscriptionStatusQuery.cs` | **Create** |
| `Application/Subscription/Queries/GetSubscriptionStatus/SubscriptionStatusDto.cs` | **Create** — `Plan`, `IsActive`, `ExpiresAt` only; no provider-specific fields |
| `Application/Subscription/Queries/GetSubscriptionStatus/GetSubscriptionStatusQueryHandler.cs` | **Create** |
| `Application/Subscription/Commands/CreateCheckoutSession/` (2 files) | **Create** — command + handler |
| `Application/Subscription/Commands/CreateBillingPortalSession/` (2 files) | **Create** — command + handler |
| `Application/Finance/Commands/CreateTransaction/CreateTransactionCommandHandler.cs` | **Modify** — inject `ISubscriptionLimitService`, call `EnforceMonthlyTransactionLimitAsync` |
| `Application/Finance/Commands/CreateBudget/CreateBudgetCommandHandler.cs` | **Modify** — call `EnforceBudgetLimitAsync` |
| `Application/Finance/Queries/GetTransactions/GetTransactionsQueryHandler.cs` | **Modify** — call `EnforceTransactionHistoryAccessAsync` if date filter present |
| `Application/Trading/Commands/CreateTrade/CreateTradeCommandHandler.cs` | **Modify** — call `EnforceTradeLimitAsync` |
| `Application/Trading/Commands/AddWatchedSymbol/AddWatchedSymbolCommandHandler.cs` | **Modify** — call `EnforceWatchlistLimitAsync` |
| `Application/Trading/Queries/GetSignals/GetSignalsQueryHandler.cs` | **Modify** — call `EnforceSignalHistoryAccessAsync` if from-date present |
| `Application/Notifications/Commands/UpdateNotificationPreference/UpdateNotificationPreferenceCommandHandler.cs` | **Modify** — call `EnforceTelegramAsync` when enabling |

### Infrastructure
| File | Action |
|---|---|
| `Infrastructure/Identity/CurrentUserService.cs` | **Create** — implement `ICurrentUserService` via `IHttpContextAccessor`; `IsAdmin` checks `ClaimTypes.Role` |
| `Infrastructure/Services/SubscriptionLimitService.cs` | **Create** — implements `ISubscriptionLimitService`; admin bypass + `-1` sentinel + count queries |
| `Infrastructure/Stripe/StripeOptions.cs` | **Create** — Stripe-specific credentials only (`SecretKey`, `WebhookSecret`) |
| `Infrastructure/Stripe/StripePaymentGatewayService.cs` | **Create** — implements `IPaymentGatewayService` using `Stripe.net` NuGet |
| `Infrastructure/Stripe/StripeWebhookHandler.cs` | **Create** — implements `IPaymentWebhookHandler`; Stripe-specific signature verification (`Stripe-Signature` header + `EventUtility.ConstructEvent`) and event dispatch |
| `Infrastructure/Persistence/Configurations/AppUserConfiguration.cs` | **Modify** — add column configs + `HasIndex(u => u.PaymentCustomerId)` |
| `Infrastructure/Persistence/Repositories/UserRepository.cs` | **Modify** — implement `GetByPaymentCustomerIdAsync` |
| `Infrastructure/Persistence/Repositories/TransactionRepository.cs` | **Modify** — implement `CountByUserAndMonthAsync` |
| `Infrastructure/Persistence/Repositories/BudgetRepository.cs` | **Modify** — implement `CountByUserAndMonthAsync` |
| `Infrastructure/Persistence/Repositories/TradeRepository.cs` | **Modify** — implement `CountByUserAsync` |
| `Infrastructure/Persistence/Repositories/WatchedSymbolRepository.cs` | **Modify** — implement `CountByUserAsync` |
| `Infrastructure/DependencyInjection.cs` | **Modify** — register `ICurrentUserService`, `ISubscriptionLimitService`; configure `SubscriptionPlanOptions` + `PaymentGatewayOptions`; select `IPaymentGatewayService` + `IPaymentWebhookHandler` implementation based on `PaymentGateway:Provider` (mirrors IAM provider selection pattern) |

### API
| File | Action |
|---|---|
| `API/Middleware/ExceptionHandlingMiddleware.cs` | **Modify** — add 402 case for `PlanLimitExceededException` before `DomainException` case |
| `API/Controllers/SubscriptionController.cs` | **Create** — `[Authorize]`: GET `/api/subscription/status`, POST `/api/subscription/checkout`, POST `/api/subscription/portal` |
| `API/Controllers/PaymentWebhookController.cs` | **Create** — `[AllowAnonymous]` POST `/api/payment/webhook`; reads raw body and delegates entirely to `IPaymentWebhookHandler` — zero provider-specific logic in the controller |

**Generate migration:**
```bash
cd backend
dotnet ef migrations add AddSubscriptionFieldsToAppUser \
  --project src/FinTrackPro.Infrastructure \
  --startup-project src/FinTrackPro.API
```

---

## 7. Key Implementation Details

### SubscriptionLimitService — admin bypass and `-1` sentinel
```csharp
private bool IsUnlimited(AppUser user) => _currentUserService.IsAdmin;

private PlanLimits GetLimits(AppUser user) => user.Plan switch {
    SubscriptionPlan.Pro => _options.Value.Pro,
    _                    => _options.Value.Free,
    // Future: SubscriptionPlan.Premium => _options.Value.Premium,
};

// Example: budget limit enforcement
public async Task EnforceBudgetLimitAsync(AppUser user, IBudgetRepository repo,
    string month, CancellationToken ct = default)
{
    if (IsUnlimited(user)) return;
    var limits = GetLimits(user);
    if (limits.ActiveBudgetLimit == -1) return;
    var count = await repo.CountByUserAndMonthAsync(user.Id, month, ct);
    if (count >= limits.ActiveBudgetLimit)
        throw new PlanLimitExceededException("budget",
            $"Budget limit of {limits.ActiveBudgetLimit} reached for your current plan.");
}
```

### DependencyInjection — provider selection (mirrors IAM pattern)
```csharp
var provider = config["PaymentGateway:Provider"];
if (provider == "stripe") {
    services.Configure<StripeOptions>(config.GetSection(StripeOptions.SectionName));
    services.AddScoped<IPaymentGatewayService, StripePaymentGatewayService>();
    services.AddScoped<IPaymentWebhookHandler, StripeWebhookHandler>();
}
// Future: else if (provider == "paddle") { ... }
```

### PaymentWebhookController — no provider-specific logic
```csharp
[AllowAnonymous, HttpPost("/api/payment/webhook")]
public async Task<IActionResult> HandleWebhook(
    [FromServices] IPaymentWebhookHandler handler, CancellationToken ct)
{
    var payload   = await new StreamReader(Request.Body).ReadToEndAsync();
    var signature = Request.Headers["Stripe-Signature"].ToString(); // header key stays in handler
    var result    = await handler.HandleAsync(payload, signature, ct);
    return result.SignatureValid ? Ok() : BadRequest();
}
```

### StripeWebhookHandler (Infrastructure) — all Stripe-specific logic isolated here
```csharp
public async Task<PaymentWebhookResult> HandleAsync(string payload, string signature, CancellationToken ct)
{
    Event stripeEvent;
    try {
        stripeEvent = EventUtility.ConstructEvent(payload, signature, _stripeOptions.WebhookSecret);
    } catch (StripeException) {
        return new PaymentWebhookResult(SignatureValid: false);
    }
    // dispatch to AppUser domain methods via IUserRepository...
    return new PaymentWebhookResult(SignatureValid: true);
}
```

### CreateCheckoutSessionCommandHandler — lazy customer creation
```csharp
if (user.PaymentCustomerId is null) {
    var customerId = await _paymentGateway.CreateCustomerAsync(user.Email!, user.DisplayName, ct);
    // persist immediately so concurrent requests don't create duplicate customers
    user.SetPaymentCustomerId(customerId);
    await _context.SaveChangesAsync(ct);
}
var sessionUrl = await _paymentGateway.CreateCheckoutSessionAsync(
    user.PaymentCustomerId, _gatewayOptions.Value.PriceId, request.SuccessUrl, request.CancelUrl, ct);
return new CheckoutSessionDto(sessionUrl);
```

---

## 8. UI/UX

### PlanBadge (navbar user dropdown)

```
[ avatar  John Doe  ▾ ]
                        ┌──────────────────────┐
                        │  John Doe            │
                        │  john@example.com    │
                        │                      │
                        │  Plan  [ Free  ▸ ]   │  ← gray pill; click → /pricing
                        │  ─────────────────   │
                        │  Settings            │
                        │  Sign out            │
                        └──────────────────────┘
```

- Gray pill `Free` / blue pill `Pro`; clicking `Free` navigates to `/pricing`
- Pro pill is display-only (no link)

---

### FreePlanAdBanner (dashboard — Free users only)

```
┌────────────────────────────────────────────────────────────────────┐
│  ✦  You're on the Free plan.  Unlock unlimited transactions,       │
│     budgets, and more.                    [ Upgrade to Pro → ]     │
└────────────────────────────────────────────────────────────────────┘
```

- Full-width gradient banner at top of dashboard page body
- Hidden entirely for Pro users and admins

---

### PlanLimitModal (global — triggered by any 402 response)

```
        ┌──────────────────────────────────────┐
        │  Budget limit reached            [×] │
        │  ────────────────────────────────    │
        │  You've used all 3 budgets on your   │
        │  Free plan.                          │
        │                                      │
        │  Upgrade to Pro for unlimited        │
        │  budgets, transactions, and more.    │
        │                                      │
        │           [ Upgrade to Pro ]         │
        │           [ Maybe later    ]         │
        └──────────────────────────────────────┘
```

- Mounted globally in `App.tsx`; opens when `planLimitStore.open === true`
- `feature` field from the 402 response drives the headline and body copy
- `[ Upgrade to Pro ]` calls `useCreateCheckoutSession()` and redirects to Stripe

---

### PricingPage (`/pricing`)

```
┌─────────────────────────┐   ┌─────────────────────────┐
│          Free           │   │   ✦  Pro          $9/mo  │  ← highlighted border
│  ─────────────────────  │   │  ─────────────────────   │
│  50 transactions/mo     │   │  Unlimited transactions  │
│  90-day history         │   │  Full history            │
│  3 active budgets       │   │  Unlimited budgets       │
│  20 trades stored       │   │  Unlimited trades        │
│  3 watchlist symbols    │   │  Unlimited watchlist     │
│  7-day signal history   │   │  Full signal history     │
│  ✗  Telegram alerts     │   │  ✓  Telegram alerts      │
│  ✗  Ad-free dashboard   │   │  ✓  Ad-free dashboard    │
│                         │   │                          │
│  [ Current plan ]       │   │  [ Upgrade to Pro ]      │
└─────────────────────────┘   └─────────────────────────┘
```

- Active plan card has highlighted border; its button reads "Current plan" (disabled)
- Pro card button calls `useCreateCheckoutSession()` for Free users; calls `useCreatePortalSession()` for Pro users (→ "Manage subscription")

---

### SubscriptionSection (Settings page)

```
  Subscription
  ─────────────────────────────────────────────
  Plan          Pro                             ← blue badge
  Status        Active
  Renews        2027-04-06
  ─────────────────────────────────────────────
                [ Manage subscription ]         ← opens Stripe portal

  — or for Free users —

  Plan          Free                            ← gray badge
  ─────────────────────────────────────────────
                [ Upgrade to Pro ]
```

- On successful return from Stripe (`?subscribed=1` query param) → toast "You're now on Pro!"

---

## 9. Frontend Implementation — File by File

### New entity: `entities/subscription/`
| File | Content |
|---|---|
| `model/types.ts` | `SubscriptionPlan = 'Free' \| 'Pro'`, `SubscriptionStatus` interface |
| `api/subscriptionApi.ts` | `useSubscriptionStatus()`, `useCreateCheckoutSession()`, `useCreatePortalSession()` |
| `index.ts` | Barrel export |

### New feature: `features/plan-badge/`
| File | Content |
|---|---|
| `ui/PlanBadge.tsx` | Pill component: gray "Free" / blue "Pro"; clicking "Free" navigates to `/pricing` |
| `index.ts` | Barrel export |

### New feature: `features/upgrade/`
| File | Content |
|---|---|
| `model/planLimitStore.ts` | Zustand store: `{ open, feature, setLimit(feature), clear() }` |
| `ui/UpgradeButton.tsx` | Calls `useCreateCheckoutSession()`, redirects to `sessionUrl` on success |
| `ui/PlanLimitModal.tsx` | Subscribes to `planLimitStore`; shows targeted upgrade CTA when `open === true` |
| `ui/SubscriptionSection.tsx` | Current plan, expiry, upgrade/manage buttons; handles `?subscribed=1` toast |
| `index.ts` | Barrel export |

### New page: `pages/pricing/`
| File | Content |
|---|---|
| `ui/PricingPage.tsx` | Two cards (Free/Pro) + feature comparison table; highlights active plan; upgrade / manage button |
| `index.ts` | Barrel export |

### Modified files
| File | Change |
|---|---|
| `shared/api/client.ts` | Add 402 interceptor → `usePlanLimitStore.getState().setLimit(feature)` |
| `app/App.tsx` | Add `/pricing` route; mount `<PlanLimitModal />` globally after `<Toaster>` |
| `pages/dashboard/ui/DashboardPage.tsx` | Add `<FreePlanAdBanner />` at top of page body |
| `widgets/navbar/ui/Navbar.tsx` | Add `<PlanBadge />` + upgrade/manage link in user dropdown |
| `pages/settings/ui/SettingsPage.tsx` | Add Subscription section using `<SubscriptionSection />` |
| `features/notification-settings/ui/NotificationSettingsForm.tsx` | Disable form + overlay + `<UpgradeButton feature="telegram" />` when plan is Free |
| `shared/ui/FreePlanAdBanner.tsx` | **Create** — full-width gradient banner, renders only for Free users |

---

## 10. Testing

### Unit — `SubscriptionLimitService`
**`tests/FinTrackPro.Application.UnitTests/Subscription/SubscriptionLimitServiceTests.cs`**
- NSubstitute + FluentAssertions (matches existing pattern)
- Cases:
  - Free user at limit → `throws PlanLimitExceededException`
  - Free user below limit → passes
  - Pro user over Free limit → passes (unlimited)
  - Admin user (`IsAdmin = true`) → passes regardless of plan
  - Limit set to `-1` in config → passes for Free user

### Unit — affected handlers
- Modify existing test files for all 7 affected handlers
- Add `ISubscriptionLimitService _limitService = Substitute.For<...>()` to setup
- Add case: `Handle_FreePlanAtLimit_ThrowsPlanLimitExceededException`

### Integration — payment webhook
**`tests/FinTrackPro.Api.IntegrationTests/Features/Subscription/PaymentWebhookTests.cs`**
- Uses `DatabaseFixture` + `CustomWebApplicationFactory` (existing pattern)
- Override `IPaymentWebhookHandler` with a fake that bypasses signature verification in tests
- Cases:
  - Activate event → `GET /api/subscription/status` returns `Pro`
  - Cancel event → status reverts to `Free`
  - Invalid signature (fake returns `SignatureValid: false`) → `400 BadRequest`

### Integration — plan limits end-to-end
**`tests/FinTrackPro.Api.IntegrationTests/Features/Subscription/PlanLimitsTests.cs`**
- Free user: POST 4 budgets for same month → 4th returns `402` with `extensions.feature = ["budget"]`
- Pro user (Plan set directly in DB fixture): no `402`
- Config override: set `ActiveBudgetLimit = -1` in test config → no `402` for Free user

---

## 11. Documentation Updates Required

Update these files as part of the same implementation task:

### `docs/architecture/api-spec.md`
- Add new section **Subscription** with all 4 endpoints:
  - `GET /api/subscription/status`
  - `POST /api/subscription/checkout`
  - `POST /api/subscription/portal`
  - `POST /api/payment/webhook`
- Document the `402` Plan Limit Error response shape (`status`, `title`, `instance`, `extensions.feature`)

### `docs/architecture/database.md`
- Add 4 new columns to the `AppUsers` table: `Plan`, `PaymentCustomerId`, `PaymentSubscriptionId`, `SubscriptionExpiresAt`
- Add index note: `IX_AppUsers_PaymentCustomerId` for webhook lookup
- Add migration name: `AddSubscriptionFieldsToAppUser`

### `docs/architecture/overview.md`
- Update Application layer description to mention `Subscription/` as a new feature group (CQRS commands + queries)
- Update Infrastructure layer description to mention `StripePaymentGatewayService`, `StripeWebhookHandler`, and `SubscriptionLimitService`
- Add `ISubscriptionLimitService`, `IPaymentGatewayService`, `IPaymentWebhookHandler` to the Application interfaces list
- Note payment gateway provider selection via `PaymentGateway:Provider` (same pattern as IAM)

### `docs/architecture/ui-flows.md`
- Add **Upgrade flow**: Free user hits limit → `PlanLimitModal` opens → clicks Upgrade → Stripe Checkout → returns to app → `?subscribed=1` toast
- Add **Pricing page** flow: user clicks `PlanBadge` or `FreePlanAdBanner` → `/pricing` → selects Pro → Stripe Checkout
- Add **Manage subscription** flow: Pro user opens Settings → Subscription section → clicks Manage → Stripe Customer Portal
- Add note: `FreePlanAdBanner` and `PlanBadge` are hidden for Admin users regardless of plan

### `CLAUDE.md`
- Add `PaymentGateway__Provider`, `PaymentGateway__PriceId`, `Stripe__SecretKey`, `Stripe__WebhookSecret` to the Key Configuration table
- Add `dotnet user-secrets set` examples for the Stripe credentials (same pattern as existing secrets)

### `README.md`
- Add Stripe environment variables to the backend setup prerequisites
