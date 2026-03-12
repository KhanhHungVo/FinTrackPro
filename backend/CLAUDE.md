# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet restore
dotnet build
dotnet run --project src/FinTrackPro.API              # http://localhost:5018

dotnet test                                            # all tests
dotnet test --filter "FullyQualifiedName~<TestName>"  # single test
dotnet test tests/FinTrackPro.Application.Tests       # single project

# EF Core migrations (always specify both projects)
dotnet ef migrations add <Name> --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API
dotnet ef database update --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API
```

Developer endpoints (local): `/scalar` (API docs), `/hangfire` (job dashboard).

## Architecture

Clean Architecture with strict inward-only dependencies:

```
API → Application → Domain
Infrastructure → Domain
BackgroundJobs → Application
```

- **Domain** — entities, value objects, domain exceptions, repository interfaces. Zero external dependencies.
- **Application** — MediatR commands/queries/handlers, FluentValidation validators, DTOs with explicit `operator` conversions (no AutoMapper).
- **Infrastructure** — EF Core Code-First (`ApplicationDbContext`), repository implementations, Keycloak JWT, external HTTP clients (Binance, CoinGecko, Fear & Greed), Telegram.Bot, Skender.Stock.Indicators.
- **API** — thin controllers delegating entirely to MediatR, `ExceptionHandlingMiddleware`, DI wiring via `AddApplicationServices()` / `AddInfrastructureServices()`.
- **BackgroundJobs** — Hangfire job classes; registered in `Program.cs` as recurring jobs.

## Adding a New Feature

Follow this pattern for every new capability:

1. **Domain** — add/extend entity; define repository interface method if needed.
2. **Application** — create `<Action><Resource>Command` (or `Query`) + `<Action><Resource>CommandHandler` in a feature folder. Add a FluentValidation `<Action><Resource>CommandValidator`. Add a DTO with an explicit `operator` conversion from the domain entity.
3. **Infrastructure** — implement any new repository method on the EF Core repository class.
4. **API** — add a one-line controller action calling `_mediator.Send(command)`.
5. **Migration** — only if the schema changed.

## MediatR Pipeline

Every command/query automatically passes through, in order:
1. `ValidationBehavior` — runs all registered FluentValidation validators; throws `ValidationException` (→ HTTP 400) on failure.
2. `LoggingBehavior` — logs request name and elapsed time.

## Exception Handling

`ExceptionHandlingMiddleware` maps to HTTP codes:

| Exception | HTTP |
|---|---|
| `ValidationException` | 400 |
| `DomainException` | 400 |
| `NotFoundException` | 404 |
| Unhandled | 500 |

Throw `DomainException` for business-rule violations inside domain entities. Throw `NotFoundException` from handlers when a required resource is missing.

## Current User

Inject `ICurrentUserService` to get the authenticated user's ID from the JWT claim. Available in Application layer handlers.

## Testing Conventions

- **Domain.Tests** — pure unit tests, no mocks needed.
- **Application.Tests** — mock repositories and `ICurrentUserService` with **NSubstitute**. Do not mock `MediatR` itself; test handlers directly.
- **Infrastructure.Tests** — use real EF Core with an in-memory or SQLite provider; do not mock the database.
- Assertions use **FluentAssertions** throughout.

## External Services

All external HTTP clients are registered as typed `HttpClient` factories in `AddInfrastructureServices()`:

| Interface | Purpose |
|---|---|
| `IBinanceService` | Spot market data |
| `ICoinGeckoService` | Crypto prices & metadata |
| `IFearGreedService` | Fear & Greed index sentiment |
| `ITelegramBotClient` | Push notifications |

`Telegram__BotToken` must be set via environment variable — never in `appsettings.json`.

## Key Configuration

| Variable | Where |
|---|---|
| `ConnectionStrings__DefaultConnection` | `appsettings.json` / env |
| `Keycloak__Authority` | `appsettings.json` |
| `Keycloak__Audience` | `appsettings.json` |
| `Telegram__BotToken` | env var only |

Infrastructure dependencies (hybrid dev): `docker compose up -d sqlserver keycloak` from the repo root.
