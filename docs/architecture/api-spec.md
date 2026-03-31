# FinTrackPro — API Specification

Base URL: `http://localhost:5018`
Auth: All endpoints require `Authorization: Bearer <jwt>` unless noted.
API docs UI: `GET /scalar` (development only)
OpenAPI JSON: `GET /openapi/v1.json` (development only)

---

## Transactions

### `GET /api/transactions`
Returns all transactions for the authenticated user.

**Query params:**
| Param | Type | Description |
|---|---|---|
| `month` | string (YYYY-MM) | Filter by budget month (optional) |

**Response 200:**
```json
[
  {
    "id": "guid",
    "type": "Expense",
    "amount": 120.50,
    "currency": "VND",
    "rateToUsd": 25000.0,
    "category": "Food",
    "note": "Grocery run",
    "budgetMonth": "2026-03",
    "createdAt": "2026-03-12T10:00:00Z"
  }
]
```

---

### `POST /api/transactions`
Create a new transaction. The handler resolves and stores `rateToUsd` from the exchange-rate cache at creation time.

**Body:**
```json
{
  "type": "Expense",
  "amount": 120.50,
  "currency": "VND",
  "category": "Food",
  "note": "Grocery run",
  "budgetMonth": "2026-03"
}
```

**Response 201:** `"guid"`

**Validation errors (400):**
- `amount` must be > 0
- `category` required
- `budgetMonth` must match `YYYY-MM`
- `currency` required, max 3 chars

---

### `DELETE /api/transactions/{id}`
Delete a transaction (owner only).

**Response 204** on success. **404** if not found. **400** if not owner.

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
Returns all trades for the user (open and closed), ordered by date descending.

**Response 200:**
```json
[
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
  },
  {
    "id": "guid",
    "symbol": "ETHUSDT",
    "direction": "Long",
    "status": "Open",
    "entryPrice": 2000.0,
    "exitPrice": null,
    "currentPrice": 2200.0,
    "positionSize": 1.0,
    "fees": 0.0,
    "currency": "USD",
    "rateToUsd": 1.0,
    "result": 0.0,
    "unrealizedResult": 200.0,
    "notes": null,
    "createdAt": "2026-03-15T09:00:00Z"
  }
]
```

> `result` = realized P&L for Closed trades, computed server-side, not stored. `unrealizedResult` = estimated P&L for Open trades with a current price; `null` if no current price.

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
| 403 | Ownership check failed (`AuthorizationException`) |
| 404 | Entity not found (`NotFoundException`) |
| 409 | State conflict (`ConflictException`) |
| 401 | Missing or invalid JWT |
| 500 | Unhandled server error |
