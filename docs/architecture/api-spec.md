# FinTrackPro — API Specification

Base URL: `http://localhost:5018`
Auth: All endpoints require `Authorization: Bearer <jwt>` unless noted.
API docs UI: `GET /scalar` (development only)
OpenAPI JSON: `GET /openapi/v1.json` (development only)

---

## Transactions

### `GET /api/transactions`
Returns a paginated list of transactions for the authenticated user.

**Query params:**
| Param | Type | Default | Description |
|---|---|---|---|
| `page` | int | 1 | Page number (≥ 1) |
| `pageSize` | int | 20 | Items per page (1–100) |
| `search` | string | — | Free-text search on note and category name |
| `month` | string (YYYY-MM) | — | Filter by budget month |
| `type` | `Income` \| `Expense` | — | Filter by transaction type |
| `categoryId` | guid | — | Filter by category |
| `sortBy` | string | `date` | Column to sort: `date`, `amount`, `category` |
| `sortDir` | `asc` \| `desc` | `desc` | Sort direction |

**Response 200:**
```json
{
  "items": [
    {
      "id": "guid",
      "type": "Expense",
      "amount": 120.50,
      "currency": "VND",
      "rateToUsd": 25000.0,
      "category": "food_beverage",
      "categoryId": "guid-or-null",
      "note": "Grocery run",
      "budgetMonth": "2026-03",
      "createdAt": "2026-03-12T10:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 42,
  "totalPages": 3,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

**Validation errors (400):**
- `page` must be ≥ 1
- `pageSize` must be between 1 and 100
- `sortBy` must be one of `date`, `amount`, `category`
- `month` must match `YYYY-MM` if provided

---

### `GET /api/transactions/summary`
Returns aggregate income/expense totals for the authenticated user. Applies the same filter params as `GET /api/transactions` (minus page/sort). Use this to compute KPI cards over the full filtered dataset without transferring rows.

Totals are returned **in `preferredCurrency`**: when a transaction's stored currency matches `preferredCurrency` the amount is used as-is (no round-trip); otherwise it is normalized via `amount / rateToUsd * preferredRate`. This ensures the KPI cards match the sum of individual rows exactly.

**Query params:**
| Param | Type | Description |
|---|---|---|
| `month` | string (YYYY-MM) | Filter by budget month (optional) |
| `type` | `Income` \| `Expense` | Filter by type (optional) |
| `categoryId` | guid | Filter by category (optional) |
| `preferredCurrency` | string (ISO 4217) | Target display currency — totals are returned in this currency (optional, defaults to `USD`) |
| `preferredRate` | decimal | Exchange rate for `preferredCurrency` (units per 1 USD). Required when `preferredCurrency` is not `USD` (optional, defaults to `1`) |

**Response 200:**
```json
{
  "totalIncome": 3500.00,
  "totalExpense": 2100.00,
  "netBalance": 1400.00
}
```

> All three values are expressed in `preferredCurrency`.

---

### `POST /api/transactions`
Create a new transaction. The handler resolves and stores `rateToUsd` from the exchange-rate cache at creation time.

**Body:**
```json
{
  "type": "Expense",
  "amount": 120.50,
  "currency": "VND",
  "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "note": "Grocery run",
  "budgetMonth": "2026-03"
}
```

**Response 201:** `"guid"`

**Validation errors (400):**
- `amount` must be > 0
- `categoryId` must be a non-empty GUID (resolved to `category` slug by handler)
- `budgetMonth` must match `YYYY-MM`
- `currency` required, max 3 chars

---

### `PATCH /api/transactions/{id}`
Update an existing transaction (owner only). `rateToUsd` and `budgetMonth` are immutable and cannot be changed.

**Body:**
```json
{
  "type": "Expense",
  "amount": 250.00,
  "currency": "USD",
  "category": "food_beverage",
  "note": "Updated note",
  "categoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Validation errors (400):**
- `amount` must be > 0
- `currency` required, max 3 chars
- `category` required, max 100 chars
- `note` max 500 chars (optional)

**Response 204** on success. **400** validation error. **403** not owner. **404** not found.

---

### `DELETE /api/transactions/{id}`
Delete a transaction (owner only).

**Response 204** on success. **404** if not found. **400** if not owner.

---

## Transaction Categories

### `GET /api/transaction-categories`
Returns system categories + the caller's active custom categories, sorted system-first then by `SortOrder`.

**Query params:**
| Param | Type | Description |
|---|---|---|
| `type` | `Income` \| `Expense` | Filter by type (optional) |

**Response 200:**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "slug": "food_beverage",
    "labelEn": "Food & Beverage",
    "labelVi": "Ăn uống",
    "icon": "🍜",
    "type": "Expense",
    "isSystem": true,
    "sortOrder": 1
  }
]
```

---

### `POST /api/transaction-categories`
Create a custom category for the authenticated user.

**Body:**
```json
{
  "type": "Expense",
  "slug": "pet_care",
  "labelEn": "Pet Care",
  "labelVi": "Thú cưng",
  "icon": "🐶"
}
```

**Response 201:** `"guid"`

**Validation errors (400):** `slug` must match `^[a-z][a-z0-9_]{1,98}$`.
**409** if slug already exists for this user or as a system category.

---

### `PATCH /api/transaction-categories/{id}`
Update labels/icon of a user-owned custom category.

**Body:** `{ "labelEn": "Pets", "labelVi": "Thú nuôi", "icon": "🐾" }`

**Response 204.** **403** if `isSystem = true` or category belongs to another user. **404** if not found.

---

### `DELETE /api/transaction-categories/{id}`
Soft-delete (sets `isActive = false`) a user-owned custom category.

**Response 204.** **403** if `isSystem = true`. **404** if not found.

---

## Budgets

### `GET /api/budgets/{month}`
Returns all budgets for the user in the given month (YYYY-MM).

**Response 200:**
```json
[
  {
    "id": "guid",
    "category": "Food",
    "limitAmount": 500.00,
    "currency": "USD",
    "rateToUsd": 1.0,
    "month": "2026-03",
    "createdAt": "2026-03-01T00:00:00Z"
  }
]
```

---

### `POST /api/budgets`
Create a budget for a category and month. The handler resolves and stores `rateToUsd` at creation time.

**Body:**
```json
{
  "category": "Food",
  "limitAmount": 500.00,
  "currency": "USD",
  "month": "2026-03"
}
```

**Response 201:** `"guid"`

---

### `PATCH /api/budgets/{id}`
Update the limit amount of an existing budget (owner only).

**Body:**
```json
{
  "limitAmount": 750.00
}
```

**Response 204** on success. **404** if not found. **400** if not owner or invalid amount.

---

### `DELETE /api/budgets/{id}`
Delete a budget (owner only).

**Response 204** on success. **404** if not found. **400** if not owner.

---

## Trades

### `GET /api/trades`
Returns a paginated list of trades for the user (open and closed).

**Query params:**
| Param | Type | Default | Description |
|---|---|---|---|
| `page` | int | 1 | Page number (≥ 1) |
| `pageSize` | int | 20 | Items per page (1–100) |
| `search` | string | — | Free-text search on symbol |
| `status` | `Open` \| `Closed` | — | Filter by trade status |
| `direction` | `Long` \| `Short` | — | Filter by direction |
| `dateFrom` | date (YYYY-MM-DD) | — | Filter to trades on or after this date |
| `dateTo` | date (YYYY-MM-DD) | — | Filter to trades on or before this date |
| `sortBy` | string | `date` | Column to sort: `date`, `pnl`, `symbol`, `entryPrice`, `positionSize`, `fees` |
| `sortDir` | `asc` \| `desc` | `desc` | Sort direction |

**Response 200:**
```json
{
  "items": [
    {
      "id": "guid",
      "symbol": "BTCUSDT",
      "direction": "Long",
      "status": "Closed",
      "entryPrice": 60000.0,
      "exitPrice": 65000.0,
      "currentPrice": null,
      "positionSize": 0.1,
      "fees": 5.0,
      "currency": "USD",
      "rateToUsd": 1.0,
      "result": 495.0,
      "unrealizedResult": null,
      "notes": null,
      "createdAt": "2026-03-10T14:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 15,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

> `result` = realized P&L for Closed trades, computed server-side, not stored. `unrealizedResult` = estimated P&L for Open trades with a current price; `null` if no current price.

**Validation errors (400):**
- `page` must be ≥ 1
- `pageSize` must be between 1 and 100
- `sortBy` must be one of `date`, `pnl`, `symbol`, `entryPrice`, `positionSize`, `fees`
- `dateFrom` must be ≤ `dateTo` if both provided

---

### `GET /api/trades/summary`
Returns aggregate P&L statistics for the authenticated user. Applies the same filter params as `GET /api/trades` (minus page/sort). KPI cards on the Trades page source from this endpoint — totals are unaffected by the current page or sort.

Totals are returned **in `preferredCurrency`**: when a trade's stored currency matches `preferredCurrency` the P&L is used as-is (no round-trip); otherwise it is normalized via `pnl / rateToUsd * preferredRate`.

**Query params:**
| Param | Type | Description |
|---|---|---|
| `status` | `Open` \| `Closed` | Filter by trade status (optional) |
| `direction` | `Long` \| `Short` | Filter by direction (optional) |
| `dateFrom` | date (YYYY-MM-DD) | Filter start date (optional) |
| `dateTo` | date (YYYY-MM-DD) | Filter end date (optional) |
| `preferredCurrency` | string (ISO 4217) | Target display currency — totals are returned in this currency (optional, defaults to `USD`) |
| `preferredRate` | decimal | Exchange rate for `preferredCurrency` (units per 1 USD). Required when `preferredCurrency` is not `USD` (optional, defaults to `1`) |

**Response 200:**
```json
{
  "totalPnl": 1240.50,
  "winRate": 65,
  "totalTrades": 20,
  "unrealizedPnl": 320.00
}
```

> `winRate` is an integer percentage (0–100). `unrealizedPnl` sums unrealized P&L across all Open trades that have a current price. Both `totalPnl` and `unrealizedPnl` are expressed in `preferredCurrency`.

---

### `POST /api/trades`
Log a new trade (open or closed). The handler resolves and stores `rateToUsd` at creation time.

**Body:**
```json
{
  "symbol": "BTCUSDT",
  "direction": "Long",
  "status": "Closed",
  "entryPrice": 60000.0,
  "exitPrice": 65000.0,
  "currentPrice": null,
  "positionSize": 0.1,
  "fees": 5.0,
  "currency": "USD",
  "notes": "Breakout trade"
}
```

For an open position, omit `exitPrice` and pass an optional `currentPrice`:
```json
{
  "symbol": "BTCUSDT",
  "direction": "Long",
  "status": "Open",
  "entryPrice": 60000.0,
  "exitPrice": null,
  "currentPrice": 62000.0,
  "positionSize": 0.1,
  "fees": 0.0,
  "currency": "USD",
  "notes": null
}
```

**Response 201:** `"guid"`

**Errors:**
- `400` if `status = Closed` and `exitPrice` is missing/zero
- `400` other validation failures

---

### `PUT /api/trades/{id}`
Update all editable fields of an existing trade (owner only). Re-resolves `rateToUsd` for the new currency.

**Body:** same shape as POST body (including `status`, `exitPrice`, `currentPrice`).

**Response 200:** updated `TradeDto` (same shape as GET response)

**Errors:**
- `400` validation failures (same rules as POST)
- `403` if trade belongs to another user
- `404` if trade not found

---

### `PATCH /api/trades/{id}/close`
Close an open position — sets `status = Closed`, stores `exitPrice`, clears `currentPrice`. Atomic single transaction.

**Body:**
```json
{
  "exitPrice": 65000.0,
  "fees": 5.0
}
```

**Response 200:** updated `TradeDto` with `status = "Closed"`, `exitPrice` set, `currentPrice = null`.

**Errors:**
- `400` if `exitPrice` is missing or ≤ 0
- `403` if trade belongs to another user
- `404` if trade not found
- `409` if trade is already closed

---

### `DELETE /api/trades/{id}`
Delete a trade (owner only).

**Response 204** | **404** | **403**

---

## Watched Symbols

### `GET /api/watchedsymbols`
Returns the user's watchlist.

**Response 200:**
```json
[
  { "id": "guid", "symbol": "BTCUSDT", "createdAt": "2026-03-01T00:00:00Z" }
]
```

---

### `POST /api/watchedsymbols`
Add a symbol to watchlist. Validated against Binance `exchangeInfo`. Duplicate check applied.

**Body:** `{ "symbol": "ETHUSDT" }`

**Response 201:** `"guid"`

---

### `DELETE /api/watchedsymbols/{id}`
Remove a symbol from the watchlist (owner only).

**Response 204** on success. **404** if not found. **400** if not owner.

---

## Signals

### `GET /api/signals`
Returns the latest signals for the user.

**Query params:**
| Param | Type | Default |
|---|---|---|
| `count` | int | 20 |

**Response 200:**
```json
[
  {
    "id": "guid",
    "symbol": "BTCUSDT",
    "signalType": "RsiOversold",
    "message": "RSI 28.5 — BTCUSDT is oversold. Consider DCA.",
    "value": 28.5,
    "timeframe": "1W",
    "isNotified": true,
    "createdAt": "2026-03-12T08:00:00Z"
  }
]
```

**SignalType values:** `RsiOversold | RsiOverbought | VolumeSpike | FundingRate | EmaCross | BbSqueeze`

---

## Market (public data, cached)

### `GET /api/market/fear-greed`
Returns latest Fear & Greed Index (cached 1 hour).

**Response 200:**
```json
{ "value": 42, "label": "Fear", "timestamp": "2026-03-12T00:00:00Z" }
```

---

### `GET /api/market/trending`
Returns top 7 trending coins from CoinGecko (cached 15 minutes).

**Response 200:**
```json
[
  { "id": "bitcoin", "name": "Bitcoin", "symbol": "BTC", "marketCapRank": 1 }
]
```

---

### `GET /api/market/rates`
Returns exchange rates (units of currency per 1 USD) for the requested codes. Served from the in-memory cache (8h TTL) populated by `ExchangeRateSyncJob`.

**Query params:**
| Param | Type | Description |
|---|---|---|
| `currencies` | string | Comma-separated ISO 4217 codes (e.g. `USD,VND,EUR`) |

**Response 200:**
```json
{
  "USD": 1.0,
  "VND": 25432.0,
  "EUR": 0.92
}
```

---

## Users

### `GET /api/users/preferences`
Returns the authenticated user's language and currency preferences.

**Response 200:**
```json
{
  "language": "en",
  "currency": "USD"
}
```

---

### `PATCH /api/users/preferences`
Update the authenticated user's language and currency preferences.

**Body:**
```json
{
  "language": "vi",
  "currency": "VND"
}
```

**Response 204**

**Validation errors (400):**
- `language` must be one of `["en", "vi"]`
- `currency` required, max 3 chars

---

## Notifications

### `GET /api/notifications/preferences`
Returns the user's notification preference, or `null` if not set.

**Response 200:**
```json
{
  "id": "guid",
  "channel": "Telegram",
  "telegramChatId": "123456789",
  "isEnabled": true
}
```

---

### `POST /api/notifications/preferences`
Save (create or update) notification preferences.

**Body:**
```json
{
  "telegramChatId": "123456789",
  "isEnabled": true
}
```

**Response 204**

---

## Error Response Shape

All errors use RFC 7807 Problem Details (`application/problem+json`):

```json
{
  "status": 400,
  "title": "Validation failed",
  "instance": "/api/budgets",
  "traceId": "0HN9Q...",
  "errors": {
    "amount": ["Amount must be greater than zero."],
    "category": ["Category is required."]
  }
}
```

The `errors` field is only present for validation failures. The `traceId` field correlates with server logs.

| HTTP Status | Cause |
|---|---|
| 400 | Validation failure (`ValidationException`) or domain validation (`DomainException`) |
| 402 | Plan limit exceeded (`PlanLimitExceededException`) — see Subscription section |
| 403 | Ownership check failed (`AuthorizationException`) |
| 404 | Entity not found (`NotFoundException`) |
| 409 | State conflict (`ConflictException`) |
| 401 | Missing or invalid JWT |

---

## Subscription

### `GET /api/subscription/status`
Returns the current user's subscription state. Requires `[Authorize]`.

**Response 200:**
```json
{
  "plan": "Pro",
  "isActive": true,
  "expiresAt": "2027-04-06T00:00:00Z"
}
```

`plan` is `"Free"` or `"Pro"`. `expiresAt` is `null` for Free users. `isActive` is `false` if the Pro subscription has lapsed.

---

### `POST /api/subscription/checkout`
Creates a payment gateway Checkout session. Requires `[Authorize]`.

**Body:**
```json
{ "successUrl": "https://app.fintrackpro.dev/settings?subscribed=1", "cancelUrl": "https://app.fintrackpro.dev/pricing" }
```

**Response 200:**
```json
{ "sessionUrl": "https://checkout.stripe.com/pay/cs_test_..." }
```

Lazily creates the payment customer record on first call. The frontend redirects to `sessionUrl`.

---

### `POST /api/subscription/portal`
Creates a payment gateway Customer Portal session for self-serve management. Requires `[Authorize]`.

**Body:**
```json
{ "returnUrl": "https://app.fintrackpro.dev/settings?tab=billing" }
```

**Response 200:**
```json
{ "portalUrl": "https://billing.stripe.com/session/..." }
```

---

### `POST /api/payment/webhook`
Receives payment gateway lifecycle events. `[AllowAnonymous]`. Signature verification is delegated to `IPaymentWebhookHandler` — the controller has no provider-specific logic. Returns `400` on invalid signature, `200` on success.

Handled event types (Stripe defaults):
- `customer.subscription.updated` / `invoice.payment_succeeded` → activate Pro
- `customer.subscription.deleted` / `invoice.payment_failed` → revert to Free

---

### `402` Plan Limit Error Response
Returned when any per-plan limit is exceeded:

```json
{
  "status": 402,
  "title": "Budget limit of 3 reached for your current plan.",
  "instance": "/api/budgets",
  "traceId": "...",
  "feature": ["budget"]
}
```

The `feature` field identifies which limit was hit. Possible values: `"transaction"`, `"budget"`, `"trade"`, `"watchlist"`, `"transaction_history"`, `"signal_history"`, `"telegram"`. The frontend uses this to open a targeted upgrade modal.
| 500 | Unhandled server error |
