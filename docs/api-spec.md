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
    "category": "Food",
    "note": "Grocery run",
    "budgetMonth": "2026-03",
    "createdAt": "2026-03-12T10:00:00Z"
  }
]
```

---

### `POST /api/transactions`
Create a new transaction.

**Body:**
```json
{
  "type": "Expense",
  "amount": 120.50,
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
    "month": "2026-03",
    "createdAt": "2026-03-01T00:00:00Z"
  }
]
```

---

### `POST /api/budgets`
Create a budget for a category and month.

**Body:**
```json
{
  "category": "Food",
  "limitAmount": 500.00,
  "month": "2026-03"
}
```

**Response 201:** `"guid"`

---

## Trades

### `GET /api/trades`
Returns all trades for the user, ordered by date descending.

**Response 200:**
```json
[
  {
    "id": "guid",
    "symbol": "BTCUSDT",
    "direction": "Long",
    "entryPrice": 60000.0,
    "exitPrice": 65000.0,
    "positionSize": 0.1,
    "fees": 5.0,
    "result": 495.0,
    "notes": null,
    "createdAt": "2026-03-10T14:00:00Z"
  }
]
```

> `result` = `(exitPrice - entryPrice) × positionSize - fees` — computed server-side, not stored.

---

### `POST /api/trades`
Log a new trade. Symbol is validated against Binance `exchangeInfo`.

**Body:**
```json
{
  "symbol": "BTCUSDT",
  "direction": "Long",
  "entryPrice": 60000.0,
  "exitPrice": 65000.0,
  "positionSize": 0.1,
  "fees": 5.0,
  "notes": "Breakout trade"
}
```

**Response 201:** `"guid"`

**Errors:**
- `400` if symbol is not a valid Binance pair
- `400` validation failures

---

### `DELETE /api/trades/{id}`
Delete a trade (owner only).

**Response 204** | **404** | **400**

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

All errors follow this envelope:

```json
{
  "title": "Validation failed",
  "errors": {
    "amount": ["Amount must be greater than zero."],
    "category": ["Category is required."]
  }
}
```

| HTTP Status | Cause |
|---|---|
| 400 | Validation failure or `DomainException` |
| 401 | Missing or invalid JWT |
| 404 | Entity not found |
| 500 | Unhandled server error |
