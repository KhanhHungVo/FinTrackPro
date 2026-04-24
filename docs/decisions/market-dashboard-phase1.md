# Market Dashboard — Phase 1

## Context

The Market page previously showed a Fear & Greed gauge, a bare-bones list of 7 trending coin
names, and a recent signals feed. Users (casual, conservative, active-trader) needed richer market
signals on a single page without manual refreshes.

This document covers the three-section dashboard shipped in Phase 1: an upgraded Trending Coins
widget (top 10 with price and % change), a new Top Market Cap table, and a new Watchlist Analysis
section (live price + RSI on two timeframes). It also covers the migration of all external service
caches from `IMemoryCache` to `HybridCache` (stampede-safe, Redis-ready).

---

## 1. Key Decisions

### HybridCache replaces IMemoryCache across all external services

`HybridCache` (`Microsoft.Extensions.Caching.Hybrid`) provides built-in stampede protection: only
one caller fetches from source per key; all others wait on a `TaskCompletionSource` internally.
`IMemoryCache` has no such guarantee — a cache miss under concurrent load triggers N simultaneous
upstream HTTP calls.

`AddHybridCache()` is registered with in-memory L1 only for Phase 1. Adding
`AddStackExchangeRedisCache()` later enables L2 Redis with zero service-code changes.
`AddMemoryCache()` is removed because no other consumers exist after the four service files are
migrated.

### Two-step fetch for Trending Coins (trending IDs → /coins/markets batch)

CoinGecko's `/search/trending` endpoint returns metadata (rank, name, symbol) but no price data.
A second batch call to `/coins/markets?ids=<ids>&vs_currency=usd&price_change_percentage=1h,24h,7d`
enriches each coin with price and % change. The two-step pattern avoids the 250-call-per-minute
rate limit that individual per-coin calls would hit for 10 coins per page load.

### GetWatchlistAnalysisQuery injects HybridCache directly (ADR-1)

The watchlist analysis handler assembles data from three parallel Binance calls per symbol. The
natural caching point is the assembled result, not any individual service call. Extracting an
`IWatchlistAnalysisCacheService` interface would add indirection with no benefit —
`Microsoft.Extensions.Caching.Hybrid` is a Microsoft framework package (like `ILogger`), not a
third-party infrastructure concern, so injecting it into the Application layer is acceptable.

### GetTrendingCoinsQuery wraps the existing direct service call (ADR-2)

`MarketController.GetTrending()` previously called `ICoinGeckoService` directly. The
`GetExchangeRatesQuery` handler already follows the MediatR pattern; consistency requires a handler
here too. The controller is thinned to a single `Mediator.Send()` dispatch; `ICoinGeckoService` is
removed from its constructor.

### Nullable price fields in TickerDto (ADR-3)

Binance omits `lastPrice` and `priceChangePercent` for delisted or inactive symbols. Both fields
are `decimal?`. The handler propagates null; the widget renders `—`. This prevents a row crash
from breaking the rest of the table.

### 100 klines per RSI call (not 14 or 28)

Skender uses Welles Wilder's RSI (RMA / Wilder smoothing), identical to TA-Lib and
TradingView/Binance. The RMA seed is a simple average of the first 14 deltas; each subsequent bar
applies exponential decay `avg = (avg × 13 + current) / 14`. The seed error halves roughly every
10 bars — with only 28 candles there is ~1 warm-up bar and the RSI diverges significantly from
TradingView. With 100 candles (~7× the period) the RMA has fully converged, matching TradingView
to within ±0.1 point. The extra candles add negligible latency — Binance returns them in the same
single HTTP call.

### Empty watchlist returns HTTP 200 with [] (not 404)

A user with zero watched symbols is a valid state, not an error. The frontend empty state ("No
symbols yet — add some in Settings → Watchlist") is driven by an empty array, not an error
response.

---

## 2. System Architecture

```
Domain:         (no changes)
Application:    GetTrendingCoinsQuery + Handler
                GetMarketCapCoinsQuery + Handler
                GetWatchlistAnalysisQuery + Handler
                TrendingCoinDto (enriched), MarketCapCoinDto (new), WatchlistAnalysisItemDto (new)
                TickerDto (enriched: LastPrice, PriceChangePercent)
                ICoinGeckoService (GetMarketCapCoinsAsync added)
Infrastructure: HybridCache migration — FearGreedService, BinanceService,
                                         CoinGeckoService, ExchangeRateService
                CoinGeckoService — GetTrendingCoinsAsync rewritten (two-step),
                                    GetMarketCapCoinsAsync added
                BinanceService — Get24HrTickerAsync maps lastPrice + priceChangePercent
API:            MarketController — GetTrending dispatches via MediatR; GetMarketCap new
                WatchedSymbolsController — GetAnalysis new
Frontend:       entities/signal — TrendingCoin type enriched, MarketCapCoin type new,
                                   useTrendingCoins updated, useMarketCapCoins new
                entities/watched-symbol — WatchlistAnalysisItem type new,
                                          useWatchlistAnalysis new
                widgets/trending-coins-widget — upgraded (price + % columns, top 10)
                widgets/top-market-cap-widget — NEW
                widgets/watchlist-analysis-widget — NEW
                pages/market — TopMarketCapWidget + WatchlistAnalysisWidget inserted
```

---

## 3. Caching Architecture

### Registration

```csharp
services.AddHybridCache(options =>
{
    options.DefaultEntryOptions = new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(5)
    };
});
// AddMemoryCache() removed — no remaining consumers
```

NuGet `Microsoft.Extensions.Caching.Hybrid` added to both `FinTrackPro.Infrastructure` and
`FinTrackPro.Application` (the watchlist analysis handler caches assembled results).

### Cache Key Registry

| Service | Key | TTL | Tags |
|---|---|---|---|
| FearGreedService | `"market:fear_greed"` | 1 h | `"market:fear_greed"` |
| CoinGeckoService — trending | `"market:trending"` | 2 min | `"market"` |
| CoinGeckoService — market cap | `"market:marketcap"` | 2 min | `"market"` |
| BinanceService — exchange info | `"binance:exchange_info"` | 24 h | — |
| ExchangeRateService — rates | `"rates_usd"` | 8 h | — |
| WatchlistAnalysis handler — per symbol | `$"watchlist:analysis:{symbol}"` | 5 min | — |

**ExchangeRateService special case:** The current implementation has a three-tier pattern
(memory → HTTP → config fallback). With HybridCache, the fallback lives inside the factory lambda
passed to `GetOrCreateAsync`. The fallback result is cached for the full 8 h TTL — acceptable
because `ExchangeRateSyncJob` calls `cache.RemoveAsync("rates_usd")` to force a refresh on its
schedule.

---

## 4. API Contract

### GET /api/market/trending (updated)

Returns top 10 trending coins. Previously top 7, no price data.

**Response 200:**
```json
[
  {
    "id": "bitcoin",
    "name": "Bitcoin",
    "symbol": "BTC",
    "marketCapRank": 1,
    "price": 64230.50,
    "change1h": 0.12,
    "change24h": 2.45,
    "change7d": 5.30
  }
]
```
All numeric fields except `marketCapRank` are `number | null`. Cached 2 min.

### GET /api/market/marketcap (new)

Returns top 10 coins by market cap descending (CoinGecko). Empty list on upstream failure — no
error thrown to the controller.

**Response 200:**
```json
[
  {
    "rank": 1,
    "id": "bitcoin",
    "name": "Bitcoin",
    "symbol": "BTC",
    "price": 64230.50,
    "marketCap": 1260000000000,
    "change1h": 0.12,
    "change24h": 2.45,
    "change7d": 5.30
  }
]
```
All numeric fields are `number | null`. Cached 2 min.

### GET /api/watchedsymbols/analysis (new)

Returns live price + RSI (daily + weekly) for each of the authenticated user's watched symbols.
Requires `[Authorize]`. Empty watchlist → `[]` (HTTP 200).

**Response 200:**
```json
[
  {
    "symbol": "BTCUSDT",
    "price": 64230.50,
    "change24h": 2.45,
    "rsiDaily": 28.4,
    "rsiWeekly": 45.2
  }
]
```
All numeric fields are `number | null`. Results ordered by symbol ascending. Per-symbol cache 5 min.

---

## 5. Backend Components

### New files

| File | Purpose |
|---|---|
| `Application/Market/Queries/GetTrendingCoins/GetTrendingCoinsQuery.cs` | `record GetTrendingCoinsQuery : IRequest<IEnumerable<TrendingCoinDto>>` |
| `Application/Market/Queries/GetTrendingCoins/GetTrendingCoinsQueryHandler.cs` | Injects `ICoinGeckoService`; delegates to `GetTrendingCoinsAsync` |
| `Application/Market/Queries/GetMarketCapCoins/GetMarketCapCoinsQuery.cs` | `record GetMarketCapCoinsQuery : IRequest<IEnumerable<MarketCapCoinDto>>` |
| `Application/Market/Queries/GetMarketCapCoins/GetMarketCapCoinsQueryHandler.cs` | Injects `ICoinGeckoService`; returns `[]` on null/empty (never throws) |
| `Application/Common/Models/MarketCapCoinDto.cs` | New: `int Rank, string Id, string Name, string Symbol, decimal? Price, decimal? MarketCap, decimal? Change1h, decimal? Change24h, decimal? Change7d` |
| `Application/Trading/Queries/GetWatchlistAnalysis/GetWatchlistAnalysisQuery.cs` | `record GetWatchlistAnalysisQuery : IRequest<IEnumerable<WatchlistAnalysisItemDto>>` |
| `Application/Trading/Queries/GetWatchlistAnalysis/GetWatchlistAnalysisQueryHandler.cs` | Resolves user → fetches symbols → `Task.WhenAll` per symbol → `HybridCache.GetOrCreateAsync` per symbol → 3 parallel Binance calls + `ComputeRsi` |
| `Application/Trading/Queries/GetWatchlistAnalysis/WatchlistAnalysisItemDto.cs` | `record WatchlistAnalysisItemDto(string Symbol, decimal? Price, decimal? Change24h, double? RsiDaily, double? RsiWeekly)` |

### Modified files

| File | Change |
|---|---|
| `Application/Common/Models/TrendingCoinDto.cs` | Add `decimal? Price`, `decimal? Change1h`, `decimal? Change24h`, `decimal? Change7d` |
| `Application/Common/Models/TickerDto.cs` | Add `decimal? LastPrice`, `decimal? PriceChangePercent` |
| `Application/Common/Interfaces/ICoinGeckoService.cs` | Add `Task<IEnumerable<MarketCapCoinDto>> GetMarketCapCoinsAsync(CancellationToken ct = default)` |
| `Infrastructure/ExternalServices/FearGreedService.cs` | Replace `IMemoryCache` with `HybridCache`; key `"market:fear_greed"`, TTL 1 h |
| `Infrastructure/ExternalServices/BinanceService.cs` | Replace `IMemoryCache` with `HybridCache`; map `lastPrice`/`priceChangePercent` from Binance JSON |
| `Infrastructure/ExternalServices/CoinGeckoService.cs` | Replace `IMemoryCache` with `HybridCache`; rewrite `GetTrendingCoinsAsync` (two-step fetch, top 10); add `GetMarketCapCoinsAsync` |
| `Infrastructure/ExternalServices/ExchangeRate/ExchangeRateService.cs` | Replace `IMemoryCache` with `HybridCache`; fallback inside factory lambda |
| `Infrastructure/DependencyInjection.cs` | Replace `AddMemoryCache()` with `AddHybridCache(options => ...)` |
| `API/Controllers/MarketController.cs` | `GetTrending` dispatches via `Mediator.Send`; `ICoinGeckoService` removed from constructor; `GetMarketCap` action added |
| `API/Controllers/WatchedSymbolsController.cs` | `GetAnalysis` action added |

### Handler algorithm — GetWatchlistAnalysisQueryHandler

```
1. Resolve AppUser via ICurrentUser + IUserRepository → throw NotFoundException if missing
2. Fetch symbols via IWatchedSymbolRepository.GetByUserAsync(userId)
3. If empty → return []
4. Task.WhenAll over symbols:
   For each symbol:
     HybridCache.GetOrCreateAsync(
       $"watchlist:analysis:{symbol}",
       factory: async ct =>
         var ticker = await binanceService.Get24HrTickerAsync(symbol, ct)
         var dailyKlines = await binanceService.GetKlinesAsync(symbol, "1d", 100, ct)
         var weeklyKlines = await binanceService.GetKlinesAsync(symbol, "1w", 100, ct)
         return new WatchlistAnalysisItemDto(
           symbol,
           ticker?.LastPrice,
           ticker?.PriceChangePercent,
           ComputeRsi(dailyKlines),
           ComputeRsi(weeklyKlines))
       TTL = 5 min
     )
5. Return ordered by symbol ascending
```

```csharp
private static double? ComputeRsi(IEnumerable<KlineDto> klines)
{
    var list = klines.ToList();
    if (list.Count < 14) return null;
    var quotes = list.Select(k => new Quote { Date = k.OpenTime, Close = k.Close });
    return quotes.GetRsi(14).LastOrDefault()?.Rsi;
}
```

Reuses the identical Skender pattern proven in `MarketSignalJob`.

---

## 6. Frontend Components

### New files

| File | Purpose |
|---|---|
| `widgets/top-market-cap-widget/ui/TopMarketCapWidget.tsx` | NEW — table with Rank, Name/Symbol, Price, Market Cap, 1h/24h/7d %; 10 skeleton rows; "Data temporarily unavailable" error state |
| `widgets/watchlist-analysis-widget/ui/WatchlistAnalysisWidget.tsx` | NEW — table with Symbol, Price, 24h%, RSI Daily, RSI Weekly; empty state with Settings link; RSI badges |

### Modified files

| File | Change |
|---|---|
| `entities/signal/model/types.ts` | `TrendingCoin`: add `price?`, `change1h?`, `change24h?`, `change7d?`. Add `MarketCapCoin` interface |
| `entities/signal/api/signalApi.ts` | `useTrendingCoins`: `staleTime` → `60_000`, add `refetchInterval: 90_000`. Add `useMarketCapCoins()` |
| `entities/signal/index.ts` | Export `MarketCapCoin`, `useMarketCapCoins` |
| `entities/watched-symbol/model/types.ts` | Add `WatchlistAnalysisItem` interface |
| `entities/watched-symbol/api/watchedSymbolApi.ts` | Add `useWatchlistAnalysis()`: `staleTime: 120_000`, `refetchInterval: 180_000` |
| `entities/watched-symbol/index.ts` | Export `WatchlistAnalysisItem`, `useWatchlistAnalysis` |
| `widgets/trending-coins-widget/ui/TrendingCoinsWidget.tsx` | Grid `[40px_1fr_100px_64px_64px_64px]`; Price, 1h%, 24h%, 7d% columns; green/red % coloring; null → `—`; `overflow-x-auto`; `SkeletonRow` extended to 6 columns |
| `pages/market/ui/MarketPage.tsx` | `<TopMarketCapWidget />` inserted after the two-column row; `<WatchlistAnalysisWidget />` inserted after it |

### Polling strategy

| Section | Backend TTL | `refetchInterval` | `staleTime` |
|---|---|---|---|
| Fear & Greed | 1 h | 5 min | 4 min |
| Trending Coins | 2 min | 90 s | 60 s |
| Top Market Cap | 2 min | 90 s | 60 s |
| Watchlist Analysis | 5 min per symbol | 3 min | 2 min |
| Signals | 4 min | 4 min | 3 min |

React Query refetches silently in the background — no loading spinner on subsequent fetches, only
on initial load.

---

## 7. UI/UX

### Market Page Layout

```
┌─────────────────────────────────────────────────────┐
│  MARKET                                             │
├──────────────────────┬──────────────────────────────┤
│  Fear & Greed Gauge  │  🔥 Trending Coins (top 10)  │
│  (existing)          │  (upgraded with price/%)     │
├──────────────────────┴──────────────────────────────┤
│  🏦 Top Market Cap (new)                            │
├─────────────────────────────────────────────────────┤
│  ⭐ My Watchlist — Analysis (new)                   │
├─────────────────────────────────────────────────────┤
│  Recent Signals (existing, unchanged)               │
└─────────────────────────────────────────────────────┘
```

### TrendingCoinsWidget (upgraded)

```
┌─────────────────────────────────────────────────────────────────────┐
│  🔥 Trending Coins                                                   │
├──────┬────────────────────────┬────────────┬───────┬────────┬───────┤
│  #   │  Name          Symbol  │    Price   │  1h%  │  24h%  │  7d%  │
├──────┼────────────────────────┼────────────┼───────┼────────┼───────┤
│  #1  │  Bitcoin        BTC    │  $64,230   │ +0.12%│ +2.45% │ +5.30%│
│  #2  │  Ethereum       ETH    │   $3,210   │ -0.08%│ -1.20% │ +3.10%│
│  ··· │                        │            │       │        │       │
│  #10 │  Solana         SOL    │     $180   │ +1.50%│ +8.20% │+15.40%│
└──────┴────────────────────────┴────────────┴───────┴────────┴───────┘
```

- % color: `text-green-600 dark:text-green-400` (positive) / `text-red-600 dark:text-red-400` (negative)
- % format: `toFixed(2)` with explicit `+` prefix for positive values
- Null price or %: render `—`
- Horizontal scroll wrapper on mobile: `overflow-x-auto`

### TopMarketCapWidget (new)

```
┌───────────────────────────────────────────────────────────────────────────┐
│  🏦 Top Market Cap                                                         │
├──────┬────────────────────────┬────────────┬────────────┬───────┬────┬────┤
│  #   │  Name          Symbol  │    Price   │  Mkt Cap   │  1h%  │24h%│ 7d%│
├──────┼────────────────────────┼────────────┼────────────┼───────┼────┼────┤
│  #1  │  Bitcoin        BTC    │  $64,230   │  $1.2T     │+0.12% │+2.4│+5.3│
│  #2  │  Ethereum       ETH    │   $3,210   │ $385.2B    │-0.08% │-1.2│+3.1│
└──────┴────────────────────────┴────────────┴────────────┴───────┴────┴────┘

[Error state]  Data temporarily unavailable
[Loading state] 10 skeleton rows (animate-pulse, matching grid columns)
```

Market cap formatter:
```ts
function formatMarketCap(v: number): string {
  if (v >= 1e12) return `$${(v / 1e12).toFixed(1)}T`
  if (v >= 1e9)  return `$${(v / 1e9).toFixed(1)}B`
  if (v >= 1e6)  return `$${(v / 1e6).toFixed(1)}M`
  return `$${v.toLocaleString()}`
}
```

### WatchlistAnalysisWidget (new)

```
┌───────────────────────────────────────────────────────────────────────┐
│  ⭐ My Watchlist — Analysis                                            │
├─────────────┬────────────┬────────┬─────────────────┬────────────────┤
│  Symbol     │  Price     │  24h%  │  RSI Daily      │  RSI Weekly    │
├─────────────┼────────────┼────────┼─────────────────┼────────────────┤
│  BTCUSDT    │ $64,230    │ +2.45% │  28.4  [OS]     │  45.2          │
│  ETHUSDT    │  $3,210    │ -1.20% │  72.1  [OB]     │  65.3          │
│  SOLUSDT    │    $180    │ +8.20% │  55.3           │  25.1  [OS]    │
│  XYZUSDT    │    —       │    —   │    —            │    —           │
└─────────────┴────────────┴────────┴─────────────────┴────────────────┘
  [OS] = Oversold (blue)   [OB] = Overbought (red)

[Empty state]
  ⭐ My Watchlist — Analysis
  No symbols yet — add some in Settings → Watchlist.
  [ → Go to Settings ]
```

RSI cell rendering:
```tsx
function RsiCell({ value }: { value: number | null }) {
  if (value === null) return <span className="text-muted-foreground">—</span>
  const badge =
    value < 30
      ? <span className="ml-1 rounded px-1 text-xs font-bold bg-blue-100 text-blue-700 dark:bg-blue-500/15 dark:text-blue-400">OS</span>
      : value > 70
      ? <span className="ml-1 rounded px-1 text-xs font-bold bg-red-100 text-red-700 dark:bg-red-500/15 dark:text-red-400">OB</span>
      : null
  return <span className="text-sm font-mono text-muted-foreground">{value.toFixed(1)}{badge}</span>
}
```

Settings link: `<Link to="/settings?tab=watchlist">Go to Settings</Link>`.

---

## 8. Testing

### Backend

| File | Type |
|---|---|
| `Application.UnitTests/Trading/Queries/GetWatchlistAnalysisQueryHandlerTests.cs` | Application unit — 6 scenarios (see below) |
| `Infrastructure.UnitTests/ExternalServices/BinanceServiceTests.cs` | Infrastructure unit — 2 new scenarios |
| `Infrastructure.UnitTests/ExternalServices/CoinGeckoServiceTests.cs` | Infrastructure unit — 5 new scenarios |

**GetWatchlistAnalysisQueryHandlerTests scenarios:**

| Test method | Assert |
|---|---|
| `Handle_ThreeSymbols_AllDataAvailable_ReturnsThreeFullyPopulatedRows` | 3 rows; all fields non-null |
| `Handle_OneSymbolBinanceReturnsNullTicker_ThatRowHasNullPriceAndChange_OthersUnaffected` | Null ticker row has `Price = null`, `Change24h = null`; other rows fully populated |
| `Handle_OneSymbolKlinesInsufficientForRsi_ThatRowHasNullRsi_OthersUnaffected` | Fewer than 14 klines → `RsiDaily = null`, `RsiWeekly = null` for that row |
| `Handle_EmptyWatchlist_ReturnsEmptyWithoutException` | Returns empty enumerable |
| `Handle_UserNotFound_ThrowsNotFoundException` | Throws `NotFoundException` |
| `Handle_ResultsOrderedBySymbolAscending` | Repo returns SOLUSDT/ETHUSDT/BTCUSDT → result is BTCUSDT/ETHUSDT/SOLUSDT |

RSI test data note: 100 identical close prices → `null` RSI (no gains/losses). Strictly increasing
close prices → RSI → 100. Strictly decreasing → RSI → 0. These are sufficient to confirm non-null
RSI is produced and propagated.

**BinanceService new tests:**

| Test method | Assert |
|---|---|
| `Get24HrTickerAsync_ValidPayloadWithPriceFields_MapsLastPriceAndPriceChangePercent` | `LastPrice == 64230.50m`, `PriceChangePercent == 2.45m` |
| `Get24HrTickerAsync_MissingPriceFields_ReturnsNullableFieldsAsNull` | `LastPrice == null`, `PriceChangePercent == null`; `Symbol`, `Volume`, `QuoteVolume` still populated |

**CoinGeckoService new tests:**

| Test method | Assert |
|---|---|
| `GetMarketCapCoinsAsync_ValidResponse_ReturnsMappedCoins` | All fields mapped correctly from `/coins/markets` response |
| `GetMarketCapCoinsAsync_HttpError_ReturnsEmptyList` | HTTP 500 → empty list, no throw |
| `GetMarketCapCoinsAsync_CachedResponse_DoesNotCallHttpClient` | Cache hit on second call |
| `GetTrendingCoinsAsync_ValidResponseWithPriceFields_MapsPriceAndChangePercentages` | `Price`, `Change1h`, `Change24h`, `Change7d` non-null and correctly mapped |
| `GetTrendingCoinsAsync_MissingPriceFields_ReturnsNullableFieldsAsNull` | Price fields null; `Id`, `Name`, `Symbol`, `MarketCapRank` still populated |

### API E2E (Newman)

**Upgraded — GET /api/market/trending:**
- Returns up to 10 items
- Each item has keys: `id`, `name`, `symbol`, `marketCapRank`, `price`, `change1h`, `change24h`, `change7d`

**New — GET /api/market/marketcap:**
- Status 200; returns array of up to 10 items
- Each item has keys: `rank`, `id`, `name`, `symbol`, `price`, `marketCap`, `change1h`, `change24h`, `change7d`
- `rank` is a number ≥ 1

**New folder — Watchlist Analysis:**
1. `POST /api/watchedsymbols` → 201 (seed BTCUSDT)
2. `GET /api/watchedsymbols/analysis` → 200; each item has `symbol`, `price`, `change24h`, `rsiDaily`, `rsiWeekly` (all numeric or null); if `rsiDaily` is a number it is between 0 and 100
3. `DELETE /api/watchedsymbols/{id}` → 204 (cleanup)

### Frontend

No new Playwright spec files are required for Phase 1. The three widgets are data-display
components backed by external APIs unreliable in a local E2E environment. Newman API E2E + manual
verification is sufficient. If a market spec is added in future it belongs at
`tests/e2e/market.spec.ts` and should mock the API layer via `page.route()`.

---

## 9. Verification Checklist

### Backend
- [ ] `dotnet build` passes with zero warnings after HybridCache migration
- [ ] `dotnet test --filter "Category!=Integration"` — all new handler + service tests pass
- [ ] `GET /api/market/trending` returns 10 rows each with `price`, `change1h`, `change24h`, `change7d`
- [ ] `GET /api/market/marketcap` returns 10 rows ordered by market cap descending (BTC first)
- [ ] `GET /api/watchedsymbols/analysis` with 3 watched symbols returns 3 rows with price, change24h, rsiDaily, rsiWeekly
- [ ] RSI for BTCUSDT matches TradingView (1D RSI-14) within ±1.0 point
- [ ] Delisted symbol row returns all-null fields; other rows unaffected
- [ ] Empty watchlist returns `[]` HTTP 200
- [ ] Cache-warm p95: trending ≤ 300 ms, marketcap ≤ 300 ms

### Frontend
- [ ] Trending widget shows 10 rows; price and all three % columns populated
- [ ] Positive % values render in green with `+` prefix; negative in red
- [ ] Null price/% cells render `—`
- [ ] Top Market Cap shows market cap formatted as `$1.2T` / `$1.2B`
- [ ] Top Market Cap error state: "Data temporarily unavailable" (no crash)
- [ ] Watchlist Analysis RSI Daily and RSI Weekly columns present
- [ ] RSI < 30 → blue `OS` badge; RSI > 70 → red `OB` badge; 30–70 → muted number
- [ ] Watchlist Analysis empty state: message + working link to `/settings?tab=watchlist`
- [ ] All three tables scroll horizontally at 375 px viewport
- [ ] `npm run build` passes with no TypeScript errors

---

## 10. Out of Scope — Phase 1

- Inline ★ watchlist toggle on Trending / Market Cap rows
- Hourly RSI (1 h timeframe)
- WebSocket / SSE real-time streaming
- MACD, EMA Cross, Bollinger Band signals
- Redis deployment (HybridCache wired for it but L2 not provisioned)
- Multi-currency price display (USD only)
- CoinMarketCap integration
