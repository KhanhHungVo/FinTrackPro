# FinTrackPro — Backend

.NET 10 REST API following Clean Architecture. Authentication via Keycloak JWT, background jobs via Hangfire, persistence via EF Core + SQL Server.

## Stack

| Component | Technology |
|---|---|
| Framework | ASP.NET Core 10 |
| Architecture | Clean Architecture + MediatR |
| ORM | Entity Framework Core 10 |
| Auth | Keycloak / Auth0 (JWT Bearer) |
| Background jobs | Hangfire |
| API docs | Scalar |
| Logging | Serilog |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server 2022 on port 1433 — `docker compose up -d sqlserver` (local dev) **or** Azure SQL (cloud dev, see [Mode C](../docs/dev-setup.md#mode-c--hybrid-dev-against-azure-sql))
- Keycloak 24 on port 8080 — `docker compose up -d keycloak` *(only when `IdentityProvider:Provider = "keycloak"`; Auth0 requires no local container)*

## Commands

### Restore and build

```bash
cd backend
dotnet restore
dotnet build
```

### Run (development)

```bash
dotnet run --project src/FinTrackPro.API
```

API listens on `http://localhost:5018`.

### Run tests

```bash
# Unit tests only (fast, no Docker)
dotnet test --filter "Category!=Integration"

# Integration tests (requires Docker — SQL Server spun up via Testcontainers)
dotnet test --filter "Category=Integration"

# All tests
dotnet test
```

> Integration tests pull `mcr.microsoft.com/mssql/server:2022-latest` automatically via Testcontainers. Docker must be running.

## EF Core Migrations

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API

# Apply migrations to the database
dotnet ef database update --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API
```

## Configuration

Key sections in `appsettings.json` / environment variables:

| Key | Description |
|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `IdentityProvider__Provider` | Active IAM provider: `"keycloak"` (default) or `"auth0"` |
| `IdentityProvider__Audience` | JWT audience — `https://api.fintrackpro.dev` (URI convention) |
| `IdentityProvider__AdminClientId` | M2M client ID for the active IAM provider's admin API |
| `IdentityProvider__AdminClientSecret` | M2M client secret — set via `appsettings.Development.json` or env var |
| `Keycloak__Authority` | Keycloak realm URL, e.g. `http://localhost:8080/realms/fintrackpro` |
| `Auth0__Domain` | Auth0 tenant domain, e.g. `your-tenant.auth0.com` |
| `Telegram__BotToken` | Telegram Bot API token — **never commit this value** |
| `CoinGecko__ApiKey` | CoinGecko Demo API key — required for `/market/trending` endpoint; get a free key at coingecko.com/en/api |
| `Cors__Origins` | Allowed CORS origins, e.g. `http://localhost:5173` |

Override any key via environment variables using double-underscore notation:
```bash
export Telegram__BotToken="your-token-here"
```

## Local Dev Secrets (User Secrets)

Use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) to store sensitive values locally without committing them. Secrets are stored in `%APPDATA%\Microsoft\UserSecrets\` and automatically override `appsettings.Development.json` in the `Development` environment.

```bash
# One-time init (already done if UserSecretsId exists in the .csproj)
dotnet user-secrets init --project src/FinTrackPro.API

# Set secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<azure-sql-connection-string>" --project src/FinTrackPro.API
dotnet user-secrets set "IdentityProvider:AdminClientSecret" "<your-secret>" --project src/FinTrackPro.API
dotnet user-secrets set "CoinGecko:ApiKey" "<your-api-key>" --project src/FinTrackPro.API

# List / remove
dotnet user-secrets list --project src/FinTrackPro.API
dotnet user-secrets remove "ConnectionStrings:DefaultConnection" --project src/FinTrackPro.API
```

## Developer Endpoints

| URL | Description |
|---|---|
| `http://localhost:5018/scalar` | Interactive API documentation (Scalar) |
| `http://localhost:5018/hangfire` | Hangfire dashboard — background job monitor |

## Project Structure

```
src/
├── FinTrackPro.API/            # Entry point — controllers, DI registration, middleware
├── FinTrackPro.Application/    # Use cases, MediatR commands/queries, DTOs, validators
├── FinTrackPro.Domain/         # Entities, value objects, domain events, interfaces
├── FinTrackPro.Infrastructure/ # EF Core DbContext, repositories, Auth0/Keycloak adapters, Telegram, external API clients
└── FinTrackPro.BackgroundJobs/ # Hangfire job definitions and schedules
```

See [../docs/architecture.md](../docs/architecture.md) for the full architecture overview.
