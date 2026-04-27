# FinTrackPro — Backend

.NET 10 REST API following Clean Architecture. Authentication via Keycloak JWT, background jobs via Hangfire, persistence via EF Core + PostgreSQL.

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
- PostgreSQL on port 5432 — `docker compose up -d postgres`
  (also required for integration tests — see [tests/README.md](tests/README.md))
- **or** Render PostgreSQL (production — auto-provisioned by Terraform, external URL via `terraform output -raw db_external_url`)
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
# Unit tests only (fast, no external dependencies)
dotnet test --filter "Category!=Integration"

# Integration tests (requires local PostgreSQL — see tests/README.md)
dotnet test --filter "Category=Integration"

# All tests
dotnet test
```

## EF Core Migrations

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API

# Apply migrations to the database
dotnet ef database update --project src/FinTrackPro.Infrastructure --startup-project src/FinTrackPro.API
```

## Configuration

See the [Key Configuration table in root CLAUDE.md](../../CLAUDE.md#key-configuration) for the full variable reference. Override any key via environment variables using double-underscore notation:
```bash
export Telegram__BotToken="your-token-here"
```

## Local Dev Secrets (User Secrets)

Use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) to store sensitive values locally without committing them. Secrets are stored in `%APPDATA%\Microsoft\UserSecrets\` and automatically override `appsettings.Development.json` in the `Development` environment.

```bash
# One-time init (already done if UserSecretsId exists in the .csproj)
dotnet user-secrets init --project src/FinTrackPro.API

# Set secrets
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<connection-string>" --project src/FinTrackPro.API
dotnet user-secrets set "IdentityProvider:AdminClientSecret" "<your-secret>" --project src/FinTrackPro.API
dotnet user-secrets set "CoinGecko:ApiKey" "<your-api-key>" --project src/FinTrackPro.API
dotnet user-secrets set "Telegram:BotToken" "<your-bot-token>" --project src/FinTrackPro.API

# List / remove
dotnet user-secrets list --project src/FinTrackPro.API
dotnet user-secrets remove "ConnectionStrings:DefaultConnection" --project src/FinTrackPro.API
```

`Telegram:BotToken` is optional. If it is not set, Telegram notifications are skipped and the API still starts.

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

See [../docs/architecture/overview.md](../docs/architecture/overview.md) for the full architecture overview.
