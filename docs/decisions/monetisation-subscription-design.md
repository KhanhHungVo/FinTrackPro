# Monetisation / Subscription System

## Context

FinTrackPro uses a two-tier Freemium model (Free / Pro). The payment gateway (Stripe by default) is selected via `PaymentGateway:Provider` and sits behind interfaces in the Application layer — the same provider-swap pattern as IAM providers. Plan state is stored in the DB on `AppUser`, orthogonal to IAM roles.

Key constraints:
- **Roles ≠ Plans** — `User`/`Admin` roles are IAM-only. `Plan` (`Free`/`Pro`) is DB-only.
- **Admin bypass** — `Admin` IAM role exempts from all limits regardless of plan.
- **Config-driven limits** — all per-tier limits live in `SubscriptionPlans` in `appsettings.json`; `-1` means unlimited.
- **Extensible tiers** — adding a `Premium` tier requires only extending the enum and config; no schema migration.

---

## 0. Prerequisites & Initial Setup

### 0.1 Stripe Setup

1. **Create a Stripe account** at [stripe.com](https://stripe.com) and complete identity verification.
2. **Create a Product and Price**
   - Dashboard → Products → Add product → name "FinTrackPro Pro"
   - Add a recurring price (e.g. 9 USD / month)
   - Copy the **Price ID** (looks like `price_1Abc...`)
3. **Register a Webhook endpoint**
   - Dashboard → Developers → Webhooks → Add endpoint
   - URL: `https://<your-domain>/api/payment/webhook`
   - Events to listen to: `customer.subscription.updated`, `customer.subscription.deleted`, `invoice.payment_succeeded`, `invoice.payment_failed`
   - Copy the **Webhook Signing Secret** (looks like `whsec_...`)
4. **Collect your keys** from Dashboard → Developers → API keys:
   - **Secret key** (`sk_live_...` / `sk_test_...`)
   - **Publishable key** (not needed by the backend — ignore for now)
5. **Enable the Customer Portal** (optional but required for "Manage subscription")
   - Dashboard → Settings → Billing → Customer portal → Activate

#### Where to put the keys

| Value | Where |
|---|---|
| `Stripe:SecretKey` | `dotnet user-secrets` (dev) or env var (prod) |
| `Stripe:WebhookSecret` | `dotnet user-secrets` (dev) or env var (prod) |
| `PaymentGateway:PriceId` | `appsettings.json` → `"PaymentGateway": { "PriceId": "price_1Abc..." }` |

```bash
# Local dev — run from repo root
dotnet user-secrets set "Stripe:SecretKey"     "sk_test_..."  --project backend/src/FinTrackPro.API
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."    --project backend/src/FinTrackPro.API
```

> **Test mode tip:** use `sk_test_*` / `whsec_test_*` keys during development. Stripe's test card `4242 4242 4242 4242` triggers a successful checkout without real charges.

---

### 0.2 Bank Transfer Setup

All bank transfer details are purely frontend — no backend changes required.

#### 1. Prepare the QR image

Generate a VietQR (or your bank's static transfer QR) and save it as:

```
frontend/fintrackpro-ui/src/shared/assets/bank-qr.png
```

The file is already imported by `BankTransferModal.tsx` at that exact path. Replace the placeholder image with your real QR; the build will bundle it automatically.

#### 2. Set the environment variables

Copy `.env.example` → `.env` (if you haven't already) and fill in the bank transfer section:

```dotenv
# frontend/fintrackpro-ui/.env

VITE_ADMIN_TELEGRAM=your_telegram_handle      # without the "@" — rendered as t.me/<handle>
VITE_ADMIN_EMAIL=admin@yourapp.dev

VITE_BANK_NAME=Techcombank
VITE_BANK_ACCOUNT_NUMBER=1234567890
VITE_BANK_ACCOUNT_NAME=Nguyen Van A
VITE_BANK_TRANSFER_AMOUNT=99000               # monthly price in VND; shown as "99,000 VND / month"
```

`VITE_ADMIN_TELEGRAM` and `VITE_ADMIN_EMAIL` are optional — their contact buttons in `BankTransferModal` render only when the variable is set.

#### 3. Verify in the UI

1. Start the dev server: `npm run dev`
2. Navigate to `/pricing` → click "or pay via bank transfer →"
3. Confirm the modal shows your QR image, bank details, and contact buttons.

---

## 1. System Architecture

```
Domain:         SubscriptionPlan enum · AppUser subscription fields · PlanLimitExceededException
Application:    ISubscriptionLimitService · SubscriptionPlanOptions · Subscription CQRS
                IPaymentGatewayService · IPaymentWebhookHandler
Infrastructure: SubscriptionLimitService · StripePaymentGatewayService · StripeWebhookHandler
                CurrentUserService (IsAdmin) · Repository count methods
API:            SubscriptionController (GET status, POST checkout, POST portal)
                PaymentWebhookController (POST /api/payment/webhook — AllowAnonymous)
                ExceptionHandlingMiddleware (402 mapping)
Frontend:       entities/subscription/ · features/plan-badge/ · features/upgrade/
                pages/pricing/ · FreePlanAdBanner · paywall guards on forms
```

Key invariants:
- Subscription state is always read from the DB — never from the JWT.
- All limit checks throw `PlanLimitExceededException` (→ HTTP 402) with a `feature` field so the frontend can open a targeted upgrade modal.
- Webhooks are the single source of truth for plan activation/deactivation.
- `AppUser.PaymentCustomerId` is retained on cancellation to prevent duplicate customer records on re-subscription.

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

All values are configurable in `appsettings.json`. Set any numeric limit to `-1` for unlimited.

---

## 3. Configuration

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

Strongly-typed options: `SubscriptionPlanOptions` + `PlanLimits` (Application layer), `PaymentGatewayOptions` (Application, provider-neutral), `StripeOptions` (Infrastructure, Stripe-specific). Dev tip: override all Free limits to `-1` in `appsettings.Development.json` to disable enforcement without code changes.

---

## 4. API Contract

### GET /api/subscription/status — `[Authorize]`
```json
{ "plan": "Pro", "isActive": true, "expiresAt": "2027-04-06T00:00:00Z" }
```

### POST /api/subscription/checkout — `[Authorize]`
```json
// Request
{ "successUrl": "https://app.fintrackpro.dev/settings?tab=billing&subscribed=1", "cancelUrl": "https://app.fintrackpro.dev/pricing" }
// Response
{ "sessionUrl": "https://checkout.stripe.com/pay/cs_test_..." }
```

### POST /api/subscription/portal — `[Authorize]`
```json
// Request
{ "returnUrl": "https://app.fintrackpro.dev/settings?tab=billing" }
// Response
{ "portalUrl": "https://billing.stripe.com/session/..." }
```

### POST /api/payment/webhook — `[AllowAnonymous]`
Delegates to `IPaymentWebhookHandler`. Returns `400` on invalid signature, `200` on success.

Handled events: `customer.subscription.updated` / `invoice.payment_succeeded` → activate Pro; `customer.subscription.deleted` / `invoice.payment_failed` → revert to Free.

### 402 Plan Limit Error
```json
{
  "status": 402,
  "title": "Budget limit reached for your current plan.",
  "instance": "/api/budgets",
  "extensions": { "feature": ["budget"] }
}
```

---

## 5. Backend Components

**Domain**
- `Domain/Enums/SubscriptionPlan.cs` — `Free = 0`, `Pro = 1`
- `Domain/Entities/AppUser.cs` — 4 subscription fields (`Plan`, `PaymentCustomerId`, `PaymentSubscriptionId`, `SubscriptionExpiresAt`) + `ActivateSubscription()`, `CancelSubscription()`, `SetPaymentCustomerId()`
- `Domain/Exceptions/PlanLimitExceededException.cs` — inherits `DomainException`, carries `string Feature`
- `Domain/Repositories/IUserRepository.cs` — `GetByPaymentCustomerIdAsync`
- Repository count methods: `ITransactionRepository.CountByUserAndMonthAsync`, `IBudgetRepository.CountByUserAndMonthAsync`, `ITradeRepository.CountByUserAsync`, `IWatchedSymbolRepository.CountByUserAsync`

**Application**
- `Application/Common/Options/` — `SubscriptionPlanOptions.cs`, `PaymentGatewayOptions.cs`
- `Application/Common/Interfaces/ICurrentUser.cs` — `bool IsAdmin { get; }` added
- `Application/Common/Interfaces/ISubscriptionLimitService.cs` — 7 `Enforce*Async` methods
- `Application/Common/Interfaces/IPaymentGatewayService.cs` — `CreateCustomerAsync`, `CreateCheckoutSessionAsync`, `CreateBillingPortalSessionAsync`
- `Application/Common/Interfaces/IPaymentWebhookHandler.cs` — `HandleAsync(payload, headers, ct)` → `PaymentWebhookResult`
- `Application/Subscription/` — CQRS: `GetSubscriptionStatusQuery` + `SubscriptionStatusDto`, `CreateCheckoutSessionCommand`, `CreateBillingPortalSessionCommand`
- Handlers with limit guards: `CreateTransactionCommandHandler`, `CreateBudgetCommandHandler`, `GetTransactionsQueryHandler`, `CreateTradeCommandHandler`, `AddWatchedSymbolCommandHandler`, `GetSignalsQueryHandler`, `UpdateNotificationPreferenceCommandHandler`

**Infrastructure**
- `Infrastructure/Identity/CurrentUserAccessor.cs` — `IsAdmin` via `ClaimTypes.Role`
- `Infrastructure/Services/SubscriptionLimitService.cs` — admin bypass + `-1` sentinel + count queries
- `Infrastructure/Stripe/` — `StripeOptions.cs`, `StripePaymentGatewayService.cs`, `StripeWebhookHandler.cs`
- `Infrastructure/Persistence/Configurations/AppUserConfiguration.cs` — 4 column configs + `HasIndex(u => u.PaymentCustomerId)`
- Repository implementations: `UserRepository`, `TransactionRepository`, `BudgetRepository`, `TradeRepository`, `WatchedSymbolRepository`
- `Infrastructure/DependencyInjection.cs` — `AddPaymentGateway()` helper; provider selected by `PaymentGateway:Provider` with `OrdinalIgnoreCase`

**API**
- `API/Middleware/ExceptionHandlingMiddleware.cs` — 402 case for `PlanLimitExceededException`
- `API/Controllers/SubscriptionController.cs` — GET status, POST checkout, POST portal
- `API/Controllers/PaymentWebhookController.cs` — `[AllowAnonymous]`; delegates entirely to `IPaymentWebhookHandler`

EF migration: `AddSubscriptionFieldsToAppUser`

---

## 6. Frontend Components

**`entities/subscription/`** — `SubscriptionPlan` type, `SubscriptionStatus` interface, React Query hooks (`useSubscriptionStatus`, `useCreateCheckoutSession`, `useCreatePortalSession`)

**`features/plan-badge/`** — `PlanBadge`: gray Free pill with arrow icon (clickable → `/pricing`) / blue Pro pill (display-only)

**`features/upgrade/`**
- `model/planLimitStore.ts` — Zustand: `{ open, feature, setLimit(feature), clear() }`
- `model/bankTransferStore.ts` — Zustand: `{ open, openModal(), closeModal() }`
- `ui/UpgradeButton.tsx` — initiates Stripe checkout; on error shows toast and opens `StripeUnavailableModal`
- `ui/PlanLimitModal.tsx` — subscribes to `planLimitStore`; targeted upgrade CTA when `open === true`
- `ui/BankTransferModal.tsx` — controlled by `bankTransferStore`; QR + contact + bank details
- `ui/StripeUnavailableModal.tsx` — shown inside `UpgradeButton` on checkout error; routes to `BankTransferModal`
- `ui/SubscriptionSection.tsx` — current plan, expiry, upgrade/manage buttons; handles `?subscribed=1` toast

**`pages/pricing/`** — two-card layout (Free / Pro); highlights active plan; UpgradeButton + bank transfer link for Free users; "Manage subscription" for Pro users

**Modified files**
- `shared/api/client.ts` — 402 interceptor → `planLimitStore.setLimit(feature)`
- `app/App.tsx` — `/pricing` route; `<PlanLimitModal />` + `<BankTransferModal />` mounted globally
- `pages/dashboard/ui/DashboardPage.tsx` — `<FreePlanAdBanner />` at top of page body
- `widgets/navbar/ui/Navbar.tsx` — `<PlanBadge />` in user dropdown
- `pages/settings/ui/SettingsPage.tsx` — `<SubscriptionSection />`
- `features/notification-settings/ui/NotificationSettingsForm.tsx` — disabled + upgrade overlay for Free users
- `shared/ui/FreePlanAdBanner.tsx` — blue gradient banner; hidden for Pro/Admin; navigates to `/pricing`

---

## 7. UI/UX

### PlanBadge (navbar user dropdown)

```
[ avatar  John Doe  ▾ ]
                        ┌──────────────────────┐
                        │  John Doe            │
                        │  john@example.com    │
                        │                      │
                        │  Plan  [ Free  › ]   │  ← gray pill; click → /pricing
                        │  ─────────────────   │
                        │  Settings            │
                        │  Sign out            │
                        └──────────────────────┘
```

Pro users see a blue "Pro" pill with no link.

---

### FreePlanAdBanner (dashboard — Free users only)

```
┌────────────────────────────────────────────────────────────────────┐
│  ✦  You're on the Free plan.  Unlock unlimited transactions,       │
│     budgets, and more.                    [ Upgrade to Pro → ]     │
└────────────────────────────────────────────────────────────────────┘
```

Full-width blue gradient banner. Button navigates to `/pricing`. Hidden for Pro users and admins.

---

### PlanLimitModal (global — triggered by any 402 response)

```
        ┌──────────────────────────────────────┐
        │  Budget limit reached            [×] │
        │  ────────────────────────────────    │
        │  Upgrade to Pro for unlimited access │
        │  to budgets, transactions, trades,   │
        │  and more.                           │
        │                                      │
        │           [ Upgrade to Pro ]         │
        │           [ Maybe later    ]         │
        └──────────────────────────────────────┘
```

Mounted globally in `App.tsx`. The `feature` field from the 402 response drives the headline. `[Upgrade to Pro]` triggers checkout via `UpgradeButton`.

---

### PricingPage (`/pricing`)

```
  Simple, transparent pricing

┌─────────────────────────┐   ┌────────────────────────────────┐
│          Free           │   │  ★ Popular   Pro      $9/mo    │  ← highlighted border
│  ─────────────────────  │   │  ─────────────────────────     │
│  50 transactions/mo     │   │  500 transactions/mo           │
│  60-day history         │   │  1-year history                │
│  3 active budgets       │   │  20 active budgets             │
│  20 trades stored       │   │  200 trades stored             │
│  1 watchlist symbol     │   │  20 watchlist symbols          │
│  7-day signal history   │   │  90-day signal history         │
│  ✗  Telegram alerts     │   │  ✓  Telegram alerts            │
│  ✗  Ad-free dashboard   │   │  ✓  Ad-free dashboard          │
│                         │   │                                │
│  [ Current plan ]       │   │  [ Upgrade to Pro ]            │
│  (disabled)             │   │  or pay via bank transfer →    │
└─────────────────────────┘   └────────────────────────────────┘

  ← Back
```

Active plan card button shows "Current plan" (disabled). Pro users see "Manage subscription" instead of UpgradeButton.

---

### SubscriptionSection (Settings page)

```
  Subscription
  ─────────────────────────────────────────────
  Plan          [ Pro ]                         ← blue badge
  Status        Active
  Renews        2027-04-06
  ─────────────────────────────────────────────
                [ Manage subscription ]         ← opens Stripe portal

  — Free user view —

  Plan          [ Free ]                        ← gray badge
  ─────────────────────────────────────────────
                [ Upgrade to Pro ]
```

On return from Stripe with `?subscribed=1` → toast "You're now on Pro!". Shows loading skeleton during fetch.

---

### BankTransferModal (global — opened from PricingPage or StripeUnavailableModal)

```
        ┌────────────────────────────────────────────┐
        │  Pay via Bank Transfer                 [×] │
        │  ──────────────────────────────────────    │
        │  ┌────────────────────────────────────┐    │
        │  │ ⚠  Please contact admin before    │    │  ← amber warning box
        │  │    transferring.                   │    │
        │  └────────────────────────────────────┘    │
        │                                            │
        │        [ Telegram: @admin ]                │
        │        [ Email: admin@... ]                │
        │                                            │
        │          ┌──────────────┐                  │
        │          │   QR code    │  200×200          │
        │          └──────────────┘                  │
        │                                            │
        │  ┌────────────────────────────────────┐    │
        │  │ Bank        Techcombank            │    │
        │  │ Account     1234567890             │    │
        │  │ Holder      Nguyen Van A           │    │
        │  │ Amount      99,000 VND/month       │    │  ← gray info box
        │  │ Note        your registered email  │    │
        │  └────────────────────────────────────┘    │
        │                                            │
        │               [ Close ]                    │
        └────────────────────────────────────────────┘
```

Controlled by `bankTransferStore`. Contact buttons render conditionally based on `VITE_ADMIN_TELEGRAM` / `VITE_ADMIN_EMAIL` env vars.

---

### StripeUnavailableModal (inline — shown by UpgradeButton on checkout error)

```
        ┌──────────────────────────────────────────┐
        │  Card payment temporarily            [×] │
        │  unavailable                             │
        │  ────────────────────────────────────    │
        │  You can upgrade via bank transfer       │
        │  instead.                                │
        │                                          │
        │       [ View Bank Transfer QR ]          │  ← primary blue; opens BankTransferModal
        │       [ Maybe later           ]          │  ← secondary gray
        └──────────────────────────────────────────┘
```

---

## 8. Testing

### Unit — `SubscriptionLimitService`
`tests/FinTrackPro.Application.UnitTests/Subscription/SubscriptionLimitServiceTests.cs`
- Free user at limit → throws `PlanLimitExceededException`
- Free user below limit → passes
- Pro user over Free limit → passes
- Admin (`IsAdmin = true`) → passes regardless of plan
- Limit set to `-1` → passes for Free user

### Unit — affected handlers
Modify existing test files for all 7 affected handlers; add `ISubscriptionLimitService` mock and `Handle_FreePlanAtLimit_ThrowsPlanLimitExceededException` case.

### Integration — payment webhook
`tests/FinTrackPro.Api.IntegrationTests/Features/Subscription/PaymentWebhookTests.cs`  
Override `IPaymentWebhookHandler` with a fake that bypasses signature verification.
- Activate event → `GET /api/subscription/status` returns `Pro`
- Cancel event → status reverts to `Free`
- Invalid signature → `400 BadRequest`

### Integration — plan limits end-to-end
`tests/FinTrackPro.Api.IntegrationTests/Features/Subscription/PlanLimitsTests.cs`
- Free user: POST 4th budget → `402` with `extensions.feature = ["budget"]`
- Pro user (Plan set in DB fixture): no `402`
- `ActiveBudgetLimit = -1` in test config → no `402` for Free user

---

## 9. Bank Transfer Payment (MVP)

Stripe does not support Vietnamese merchant accounts for production payouts. A static Techcombank QR is shown as a parallel payment path. Zero backend changes required — the flow is entirely frontend + manual admin confirmation.

| Path | When used | Automated? |
|---|---|---|
| Stripe Checkout | `Stripe:SecretKey` configured | Yes (webhook-driven) |
| Bank Transfer QR | Always available | No (admin confirms manually) |

**Direct bank transfer:** User clicks "or pay via bank transfer →" on PricingPage → `BankTransferModal` opens → user contacts admin and transfers → admin activates Pro manually.

**Stripe fallback:** User clicks "Upgrade to Pro" → checkout API fails → `StripeUnavailableModal` → "View Bank Transfer QR" → `BankTransferModal`.

### Environment variables

| Variable | Purpose |
|---|---|
| `VITE_ADMIN_TELEGRAM` | Telegram handle for admin contact button |
| `VITE_ADMIN_EMAIL` | Email for admin contact button |
| `VITE_BANK_NAME` | Bank name in transfer details |
| `VITE_BANK_ACCOUNT_NUMBER` | Account number (text fallback below QR) |
| `VITE_BANK_ACCOUNT_NAME` | Account holder name |
| `VITE_BANK_TRANSFER_AMOUNT` | Monthly Pro price in VND (default `99000`) |

### Migration path to PayOS

When manual confirmation becomes impractical, replace bank transfer with PayOS (VietQR-based, webhook-driven). Implement `PayOsPaymentGatewayService` implementing `IPaymentGatewayService`; set `PaymentGateway:Provider = "payos"`. No Application or Domain changes needed.
