# FinTrackPro — Implementation Roadmap

> Last updated: 2026-03-21

---

## Build Status

### ✅ Phase 1 — Backend Foundation
**Status: COMPLETE**

| Artifact | File | Status |
|---|---|---|
| Solution & projects | `backend/FinTrackPro.sln` | ✅ |
| Domain base classes | `Domain/Common/BaseEntity, AggregateRoot, BaseEvent` | ✅ |
| Domain entities | `AppUser, Transaction, Budget, Trade, WatchedSymbol, Signal, NotificationPreference` | ✅ |
| Domain enums | `TransactionType, TradeDirection, SignalType, NotificationChannel, UserRole` | ✅ |
| Domain exceptions | `DomainException, NotFoundException` | ✅ |
| Repository interfaces | `IUserRepository, ITransactionRepository, IBudgetRepository, ITradeRepository, IWatchedSymbolRepository, ISignalRepository, INotificationPreferenceRepository` | ✅ |
| ApplicationDbContext | `Infrastructure/Persistence/ApplicationDbContext.cs` | ✅ |
| EF Core configurations | All 7 entity configurations in `Infrastructure/Persistence/Configurations/` | ✅ |
| Repository implementations | All 7 in `Infrastructure/Persistence/Repositories/` | ✅ |

---

### ✅ Phase 2 — Authentication
**Status: COMPLETE (code) / MANUAL (Keycloak setup)**

| Artifact | File | Status |
|---|---|---|
| JWT Bearer middleware | `API/Program.cs` — `AddJwtBearer` provider-conditional (Keycloak or Auth0) | ✅ |
| Auth0 provider | `Auth0ClaimsTransformer`, `Auth0ManagementService` — `IdentityProvider:Provider = "auth0"` | ✅ |
| `ICurrentUserService` | `Application/Common/Interfaces/ICurrentUserService.cs` | ✅ |
| `CurrentUserService` | `Infrastructure/Services/CurrentUserService.cs` — reads `sub`, `email`, `name` from JWT claims | ✅ |
| `[Authorize]` on all controllers | All 7 controllers | ✅ |
| Keycloak realm setup | Manual — see [Manual Steps](#manual-steps) | ⏳ |

---

### ✅ Phase 3 — Finance Module
**Status: COMPLETE**

| Artifact | File | Status |
|---|---|---|
| `CreateTransactionCommand` + Handler + Validator | `Application/Finance/Commands/CreateTransaction/` | ✅ |
| `DeleteTransactionCommand` + Handler | `Application/Finance/Commands/DeleteTransaction/` | ✅ |
| `GetTransactionsQuery` + Handler + DTO | `Application/Finance/Queries/GetTransactions/` | ✅ |
| `CreateBudgetCommand` + Handler + Validator | `Application/Finance/Commands/CreateBudget/` | ✅ |
| `GetBudgetsQuery` + Handler + DTO | `Application/Finance/Queries/GetBudgets/` | ✅ |
| `TransactionsController` | `API/Controllers/TransactionsController.cs` | ✅ |
| `BudgetsController` | `API/Controllers/BudgetsController.cs` | ✅ |
| `BudgetOverrunJob` (Hangfire daily) | `BackgroundJobs/Jobs/BudgetOverrunJob.cs` | ✅ |

---

### ✅ Phase 4 — Trading Journal
**Status: COMPLETE**

| Artifact | File | Status |
|---|---|---|
| `CreateTradeCommand` + Handler + Validator | `Application/Trading/Commands/CreateTrade/` | ✅ |
| `DeleteTradeCommand` + Handler | `Application/Trading/Commands/DeleteTrade/` | ✅ |
| `GetTradesQuery` + Handler + DTO | `Application/Trading/Queries/GetTrades/` | ✅ |
| `AddWatchedSymbolCommand` + Handler | `Application/Trading/Commands/AddWatchedSymbol/` | ✅ |
| `RemoveWatchedSymbolCommand` + Handler | `Application/Trading/Commands/RemoveWatchedSymbol/` | ✅ |
| `GetWatchedSymbolsQuery` + Handler + DTO | `Application/Trading/Queries/GetWatchedSymbols/` | ✅ |
| Binance symbol validation (`exchangeInfo`) | `Infrastructure/ExternalServices/BinanceService.cs` | ✅ |
| `TradesController` | `API/Controllers/TradesController.cs` | ✅ |
| `WatchedSymbolsController` (GET, POST, DELETE) | `API/Controllers/WatchedSymbolsController.cs` | ✅ |
| P&L auto-calculation | `Domain/Entities/Trade.cs` — `Result` computed property | ✅ |

---

### ✅ Phase 5 — Market Intelligence
**Status: COMPLETE (Tier 1 signals)**

| Artifact | File | Status |
|---|---|---|
| `MarketSignalJob` (every 4h) | `BackgroundJobs/Jobs/MarketSignalJob.cs` | ✅ |
| RSI Oversold / Overbought via Skender | `MarketSignalJob.ProcessSymbolAsync` | ✅ |
| 24hr Volume Spike detection | `MarketSignalJob.CheckVolumeSpikeAsync` | ✅ |
| 24h deduplication (IsNotified + CreatedAt) | `ISignalRepository.ExistsRecentAsync` | ✅ |
| `IBinanceService` + implementation | `Application/Common/Interfaces/` + `Infrastructure/ExternalServices/` | ✅ |
| `IFearGreedService` + implementation | Alternative.me API, 1h cache | ✅ |
| `ICoinGeckoService` + implementation | CoinGecko trending API, 15min cache | ✅ |
| `GetSignalsQuery` + Handler + DTO | `Application/Signals/Queries/GetSignals/` | ✅ |
| `SignalsController` | `API/Controllers/SignalsController.cs` | ✅ |
| `MarketController` (fear-greed + trending) | `API/Controllers/MarketController.cs` | ✅ |
| IMemoryCache on all external APIs | Binance 24h, FearGreed 1h, CoinGecko 15min | ✅ |
| EMA Golden/Death Cross | — | 🔲 Tier 2 (future) |
| Bollinger Band Squeeze | — | 🔲 Tier 2 (future) |
| Funding Rate Sentiment | — | 🔲 Tier 2 (future) |

---

### ✅ Phase 6 — Notifications
**Status: COMPLETE**

| Artifact | File | Status |
|---|---|---|
| `INotificationChannel` interface | `Application/Common/Interfaces/INotificationChannel.cs` | ✅ |
| `INotificationService` interface | `Application/Common/Interfaces/INotificationService.cs` | ✅ |
| `TelegramNotificationChannel` | `Infrastructure/Services/TelegramNotificationChannel.cs` | ✅ |
| `NotificationService` | `Infrastructure/Services/NotificationService.cs` | ✅ |
| `SaveNotificationPreferenceCommand` | `Application/Notifications/Commands/` | ✅ |
| `GetNotificationPreferenceQuery` | `Application/Notifications/Queries/` | ✅ |
| `NotificationsController` | `API/Controllers/NotificationsController.cs` | ✅ |
| Telegram Bot token config | `appsettings.json` → `Telegram:BotToken` | ✅ |
| Email channel | — | 🔲 Future (`EmailNotificationChannel`) |

---

### ✅ Phase 7 — Frontend
**Status: COMPLETE**

| Artifact | Path | Status |
|---|---|---|
| React + Vite project (FSD) | `frontend/fintrackpro-ui/` | ✅ |
| FSD layer structure | `app / pages / widgets / features / entities / shared` | ✅ |
| TailwindCSS v4 | `@tailwindcss/vite` plugin | ✅ |
| React Query provider | `src/app/providers/QueryProvider.tsx` | ✅ |
| React Router | `src/app/App.tsx` | ✅ |
| Zustand auth store | `src/features/auth/model/authStore.ts` | ✅ |
| Axios API client + Bearer injection | `src/shared/api/client.ts` | ✅ |
| `transaction` entity (types + hooks) | `src/entities/transaction/` | ✅ |
| `trade` entity (types + hooks) | `src/entities/trade/` | ✅ |
| `signal` entity (types + FearGreed + Trending hooks) | `src/entities/signal/` | ✅ |
| `AddTransactionForm` feature | `src/features/add-transaction/` | ✅ |
| `AddTradeForm` feature | `src/features/add-trade/` | ✅ |
| `AddBudgetForm` feature | `src/features/add-budget/` | ✅ |
| `NotificationSettingsForm` feature | `src/features/notification-settings/` | ✅ |
| `WatchlistManager` feature | `src/features/manage-watchlist/` | ✅ |
| `FearGreedWidget` | `src/widgets/fear-greed-widget/` | ✅ |
| `SignalsList` widget | `src/widgets/signals-list/` | ✅ |
| `Navbar` widget | `src/widgets/navbar/` | ✅ |
| `DashboardPage` | `src/pages/dashboard/` | ✅ |
| `TransactionsPage` | `src/pages/transactions/` — list, month filter, income/expense summary, add, delete | ✅ |
| `TradesPage` | `src/pages/trades/` — table, P&L/win-rate stats, add, delete | ✅ |
| `BudgetsPage` | `src/pages/budgets/` — per-category progress bars, overrun highlight, add | ✅ |
| `SettingsPage` | `src/pages/settings/` — Telegram preferences + watchlist manager | ✅ |
| `budget` entity | `src/entities/budget/` — types + React Query hooks | ✅ |
| `watched-symbol` entity | `src/entities/watched-symbol/` — types + add/remove hooks | ✅ |
| `notification-preference` entity | `src/entities/notification-preference/` — types + get/save hooks | ✅ |
| Shared layout with `Navbar` + React Router `Outlet` | `src/app/App.tsx` | ✅ |

---

### ✅ Phase 8 — Infrastructure & CI/CD
**Status: COMPLETE (code) / MANUAL (cloud provisioning)**

| Artifact | File | Status |
|---|---|---|
| `docker-compose.yml` | Root — SQL Server + Keycloak + migrator + API | ✅ |
| `backend/Dockerfile` | Multi-stage .NET 10 runtime image | ✅ |
| `backend/Dockerfile.migrator` | SDK-based init container — runs `dotnet ef database update` then exits | ✅ |
| Azure SQL Database | `rg-fintrackpro-prod` / `sql-fintrackpro` — provisioned 2026-03-20 | ✅ |
| GitHub Actions CI | `.github/workflows/ci.yml` — build + test + Docker | ✅ |
| Terraform IaC (Render) | `infra/terraform/` — API + frontend, TF Cloud backend | ✅ |
| Azure App Service / Static Web Apps | — | 🔲 Not planned (using Render instead) |
| GitHub Actions CD secrets | `TELEGRAM_BOT_TOKEN`, `ConnectionStrings__DefaultConnection` | ⏳ Manual (set in TF Cloud) |

---

## Manual Steps Required

| # | Step | Notes |
|---|---|---|
| 1 | IAM provider | **Keycloak**: create realm `fintrackpro`, clients, audience mapper — see [docs/auth-setup.md](auth-setup.md#keycloak). **Auth0**: API, SPA app, M2M app, roles, post-login Action — see [docs/auth-setup.md](auth-setup.md#auth0-cloud-iam) |
| 2 | EF Core migration | `dotnet ef migrations add InitialCreate --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API` |
| 3 | Run migration | `dotnet ef database update --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API` |
| 4 | Azure SQL | Provision resource group, logical server, database, firewall — see [docs/dev-setup.md Mode C](dev-setup.md#mode-c--hybrid-dev-against-azure-sql); store connection string in User Secrets |
| 5 | Telegram Bot | Create via @BotFather → set token via env var or User Secrets (`Telegram:BotToken`) |
| 6 | Frontend env | Copy `.env.example` → `.env`, set `VITE_API_BASE_URL` and IAM provider vars |
| 7 | Start environment | `docker compose up` then `dotnet run` then `npm run dev` |
| 8 | Render deployment | Terraform deploy — see [docs/guides/render-deploy.md](guides/render-deploy.md) |

---

## Known Warnings

| Warning | Cause | Action |
|---|---|---|
| `NU1903: Newtonsoft.Json 11.0.1 high severity vulnerability` | Transitive dependency via `Hangfire.Core` | Pending Hangfire release with updated transitive deps; suppress or override with `<PackageReference Include="Newtonsoft.Json" Version="13.*" />` in `FinTrackPro.BackgroundJobs.csproj` |

---

## Deferred Items

| Item | Reason |
|---|---|
| Redis caching | Single-instance deployment — `IMemoryCache` sufficient; swap to `IDistributedCache` when scaling horizontally |
| Tier 2 market signals (EMA, BB Squeeze, Funding Rate) | Bollinger Band threshold and Funding Rate threshold TBD before building |
| Multi-provider account linking | Out of scope for v1 — Keycloak supports it but not wired |
| Test implementations | Test projects scaffolded and wired; test bodies pending |
| `EmailNotificationChannel` | `INotificationChannel` abstraction is in place — add when needed |
