# Integration Test Refactor — SQL Server → PostgreSQL (No Testcontainers)

## Why

The integration tests were originally written against SQL Server using Testcontainers.MsSql.
Two problems forced a migration:

1. **Docker API version mismatch** — Testcontainers v4 requests Docker API v1.44 but the local
   Docker daemon capped at v1.41, causing `DockerUnavailableException` on every run.
2. **Wrong DB engine** — the project migrated to PostgreSQL; EF Core migrations use PostgreSQL-specific
   types (`uuid`, `timestamp with time zone`, etc.) that are incompatible with SQL Server.

## What Changed

| File | Change |
|---|---|
| `tests/Tests.Common/Tests.Common.csproj` | Removed `Testcontainers.MsSql`; added `Npgsql` |
| `tests/Tests.Common/CustomWebApplicationFactory.cs` | Dropped `MsSqlContainer`; reads `TEST_DB_CONNECTION_STRING` env var; uses `UseNpgsql` |
| `tests/Tests.Common/DatabaseFixture.cs` | Removed container start/stop; Respawn uses `NpgsqlConnection` + `DbAdapter.Postgres` |

No changes to test classes, builders, or `AuthTokenFactory` — the test API surface is identical.

## How It Works

`CustomWebApplicationFactory` reads the `TEST_DB_CONNECTION_STRING` environment variable
(falling back to a default local connection string) and boots the full API against that database.
On `InitializeAsync`, EF Core migrations are applied automatically (`db.Database.MigrateAsync()`).
Respawn resets all tables between test classes so each test starts with a clean slate.

## One-Time Setup

```bash
# 1. Start local PostgreSQL (already needed for hybrid dev)
docker compose up -d postgres

# 2. Create the dedicated test database
psql -h localhost -U postgres -c "CREATE DATABASE fintrackpro_test;"

# 3. Export the connection string (add to ~/.zshrc or ~/.bashrc to persist)
export TEST_DB_CONNECTION_STRING="Host=localhost;Port=5432;Database=fintrackpro_test;Username=postgres;Password=YourStrong@Passw0rd"
```

## Running Integration Tests

```bash
cd backend
export TEST_DB_CONNECTION_STRING="Host=localhost;Port=5432;Database=fintrackpro_test;Username=postgres;Password=YourStrong@Passw0rd"

dotnet test tests/FinTrackPro.Api.IntegrationTests --filter "Category=Integration"
```

## Adding New Integration Tests

1. Create a class in `tests/FinTrackPro.Api.IntegrationTests/Features/<Feature>/`
2. Decorate with:
   ```csharp
   [Trait("Category", "Integration")]
   [Collection(nameof(IntegrationTestCollection))]
   ```
3. Implement `IAsyncLifetime` — call `await _fixture.ResetAsync()` in `InitializeAsync()`
4. Inject `DatabaseFixture` via the constructor and use `_fixture.Factory.CreateClient()`

Example skeleton:
```csharp
[Trait("Category", "Integration")]
[Collection(nameof(IntegrationTestCollection))]
public class MyFeatureTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly HttpClient _client;

    public MyFeatureTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", AuthTokenFactory.GenerateToken("user-id", "User"));
    }

    public async Task InitializeAsync() => await _fixture.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MyEndpoint_ValidRequest_Returns200()
    {
        var response = await _client.GetAsync("/api/myfeature");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```
