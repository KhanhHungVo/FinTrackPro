# FinTrackPro — Backend

.NET 10 REST API following Clean Architecture. Authentication via Keycloak JWT, background jobs via Hangfire, persistence via EF Core + SQL Server.

## Stack

| Component | Technology |
|---|---|
| Framework | ASP.NET Core 10 |
| Architecture | Clean Architecture + MediatR |
| ORM | Entity Framework Core 10 |
| Auth | Keycloak (JWT Bearer) |
| Background jobs | Hangfire |
| API docs | Scalar |
| Logging | Serilog |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server 2022 running on port 1433 (`docker compose up -d sqlserver`)
- Keycloak 24 running on port 8080 (`docker compose up -d keycloak`)

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

API listens on `http://localhost:5000`.

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
| `Keycloak__Authority` | Keycloak realm URL, e.g. `http://localhost:8080/realms/fintrackpro` |
| `Keycloak__Audience` | Expected JWT audience, e.g. `fintrackpro-api` |
| `Telegram__BotToken` | Telegram Bot API token — **never commit this value** |
| `Cors__Origins` | Allowed CORS origins, e.g. `http://localhost:5173` |

Override any key via environment variables using double-underscore notation:
```bash
export Telegram__BotToken="your-token-here"
```

## Developer Endpoints

| URL | Description |
|---|---|
| `http://localhost:5000/scalar` | Interactive API documentation (Scalar) |
| `http://localhost:5000/hangfire` | Hangfire dashboard — background job monitor |

## Project Structure

```
src/
├── FinTrackPro.API/            # Entry point — controllers, DI registration, middleware
├── FinTrackPro.Application/    # Use cases, MediatR commands/queries, DTOs, validators
├── FinTrackPro.Domain/         # Entities, value objects, domain events, interfaces
├── FinTrackPro.Infrastructure/ # EF Core DbContext, repositories, Keycloak/Telegram clients
└── FinTrackPro.BackgroundJobs/ # Hangfire job definitions and schedules
```

See [../docs/architecture.md](../docs/architecture.md) for the full architecture overview.
