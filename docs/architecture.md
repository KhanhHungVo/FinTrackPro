# FinTrackPro — Architecture

## Overview

Clean Architecture with CQRS. Dependencies point inward — outer layers depend on inner layers, never the reverse.

```
[ API / BackgroundJobs ]
         ↓
    [ Application ]
         ↓
       [ Domain ]
         ↑
  [ Infrastructure ]  (implements interfaces defined in Domain/Application)
```

## Layer Responsibilities

### Domain (`FinTrackPro.Domain`)
- Entities, enums, domain exceptions
- Repository interfaces (`IUserRepository`, etc.)
- Zero external dependencies

### Application (`FinTrackPro.Application`)
- CQRS commands and queries via MediatR
- FluentValidation validators
- Service interfaces (`ICurrentUserService`, `INotificationService`, `IBinanceService`, etc.)
- DTOs (explicit `operator` conversions, no AutoMapper)
- Pipeline behaviors: `ValidationBehavior`, `LoggingBehavior`

### Infrastructure (`FinTrackPro.Infrastructure`)
- EF Core `ApplicationDbContext` + entity configurations
- Repository implementations
- External services: `BinanceService`, `FearGreedService`, `CoinGeckoService`
- `TelegramNotificationChannel`, `NotificationService`
- `CurrentUserService` (reads JWT claims via `IHttpContextAccessor`)
- `IMemoryCache` for external API responses

### API (`FinTrackPro.API`)
- Thin controllers — delegate to `Mediator.Send()`
- `ExceptionHandlingMiddleware` maps exceptions to HTTP status codes
- Keycloak JWT Bearer authentication
- Hangfire dashboard + recurring job registration
- Scalar API UI (`/scalar`)
- CORS policy for SPA

### BackgroundJobs (`FinTrackPro.BackgroundJobs`)
- `MarketSignalJob` — every 4h: RSI + volume spike signals via Skender + Binance
- `BudgetOverrunJob` — daily: checks category spending vs budget limits

## Frontend Architecture (FSD)

Feature-Sliced Design — layers import only downward.

```
app → pages → widgets → features → entities → shared
```

| Layer | Contents |
|---|---|
| `app/` | QueryProvider, BrowserRouter + Outlet layout, global CSS |
| `pages/` | DashboardPage, TransactionsPage, BudgetsPage, TradesPage, SettingsPage |
| `widgets/` | Navbar, FearGreedWidget, SignalsList |
| `features/` | AddTransactionForm, AddTradeForm, AddBudgetForm, NotificationSettingsForm, WatchlistManager, authStore (Zustand) |
| `entities/` | transaction, trade, signal, budget, watched-symbol, notification-preference — types + React Query hooks |
| `shared/` | Axios client (Bearer injection + Keycloak redirect on 401), env config, `cn()` |

## Key Design Decisions

| Decision | Choice | Reason |
|---|---|---|
| ORM | EF Core 10 + SQL Server | Type-safe migrations, Clean Arch compatible |
| CQRS | MediatR 12 | Decoupled handlers, pipeline behaviors |
| Validation | FluentValidation 11 | Declarative, auto-wired via DI |
| Auth | Keycloak + JWT Bearer | External IdP, SSO, multi-provider |
| Background jobs | Hangfire + SQL Server storage | Persistent job history, retry policy |
| Indicators | Skender.Stock.Indicators | Free, NuGet, covers RSI/EMA/BB |
| Notifications | Telegram Bot | No cost, no email infra |
| Caching | IMemoryCache (in-process) | Single instance — swap to Redis when scaling |
| API docs | Scalar + .NET 10 built-in OpenAPI | Swashbuckle incompatible with .NET 10 |
| Frontend state | React Query (server) + Zustand (client) | Clear separation of concerns |
