# Test Projects

## Structure

| Project | Type | Trigger | Docker? |
|---|---|---|---|
| `FinTrackPro.Domain.UnitTests` | Unit tests | Every commit | No |
| `FinTrackPro.Application.UnitTests` | Unit tests | Every commit | No |
| `FinTrackPro.Api.IntegrationTests` | Integration tests | Pull request + nightly | PostgreSQL |
| `FinTrackPro.Infrastructure.UnitTests` | Unit tests | Every commit | No |
| `Tests.Common` | Shared fixtures | — | — |

## Running Tests

```bash
# Unit tests only (fast, no external dependencies)
dotnet test --filter "Category!=Integration"

# Integration tests (requires local PostgreSQL — see setup below)
export TEST_DB_CONNECTION_STRING="Host=localhost;Port=5432;Database=fintrackpro_test;Username=postgres;Password=YourStrong@Passw0rd"
dotnet test --filter "Category=Integration"

# All tests
dotnet test
```

## Integration Test Setup (one-time)

Integration tests connect to a dedicated `fintrackpro_test` database on the PostgreSQL instance
already running via `docker compose up -d postgres`. No container is started per test run.

```bash
# 1. Start local PostgreSQL
docker compose up -d postgres

# 2. Create the test database
psql -h localhost -U postgres -c "CREATE DATABASE fintrackpro_test;"

# 3. Set the connection string (add to your shell profile to persist)
export TEST_DB_CONNECTION_STRING="Host=localhost;Port=5432;Database=fintrackpro_test;Username=postgres;Password=YourStrong@Passw0rd"
```

If `TEST_DB_CONNECTION_STRING` is not set, tests fall back to the default above.

## Integration Test Infrastructure

Integration tests use:
- **Local PostgreSQL** — connects to the dedicated `fintrackpro_test` database. EF Core migrations
  are applied automatically at the start of each test run via `db.Database.MigrateAsync()`.
- **Respawn** — resets all tables to a clean state between each test class (`DbAdapter.Postgres`)
- **AuthTokenFactory** — issues local symmetric-key JWTs (with `sub`, `iss`, `email`, `email_verified`
  claims) accepted by the test API (Keycloak validation is replaced in `CustomWebApplicationFactory`)
- **UserContextMiddleware + IdentityService** — auto-provisions the `AppUser` on the first
  authenticated request; no fake user service needed
- **FakeBinanceService** — accepts all symbols as valid; avoids real Binance HTTP calls

## Adding New Tests

- Domain logic tests → `FinTrackPro.Domain.UnitTests/<Feature>/`
- Application handler tests → `FinTrackPro.Application.UnitTests/<Feature>/` (use NSubstitute for repositories)
- Infrastructure service tests → `FinTrackPro.Infrastructure.UnitTests/<Layer>/` (use NSubstitute + MockHttpMessageHandler for HttpClient)
- API endpoint tests → `FinTrackPro.Api.IntegrationTests/Features/<Feature>/`

All integration test classes must be decorated with:
```csharp
[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestCollection))]
```

And implement `IAsyncLifetime` with `await _fixture.ResetAsync()` in `InitializeAsync()`.

## CI Filter Strategy

```yaml
# Unit tests — every push
dotnet test --filter "Category!=Integration"

# Integration tests — PR and nightly (requires TEST_DB_CONNECTION_STRING)
dotnet test --filter "Category=Integration"
```

## Newman API E2E (third test layer)

The `dotnet test` projects above use `WebApplicationFactory` with a local symmetric-key JWT — Keycloak is never involved. The Newman suite (`scripts/api-e2e-local.sh`) fills that gap: it hits a real running Docker API container with a real Keycloak-issued token, exercising the full auth stack (`iss`/`aud` validation, `KeycloakClaimsTransformer`, middleware ordering) end-to-end.

| Layer | Auth | API process | When |
|---|---|---|---|
| `dotnet test` (unit) | N/A | None | Every commit |
| `dotnet test` (integration) | Local symmetric JWT | In-process `WebApplicationFactory` | PR + nightly |
| Newman | Real Keycloak JWT | Real Docker container | After `Backend — Build & Test` passes |

See [docs/postman/api-e2e-plan.md](../../docs/postman/api-e2e-plan.md) for collection structure and CI setup.
