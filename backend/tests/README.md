# Test Projects

## Structure

| Project | Type | Trigger | Docker? |
|---|---|---|---|
| `FinTrackPro.Domain.UnitTests` | Unit tests | Every commit | No |
| `FinTrackPro.Application.UnitTests` | Unit tests | Every commit | No |
| `FinTrackPro.Api.IntegrationTests` | Integration tests | Pull request + nightly | Yes |
| `FinTrackPro.Infrastructure.UnitTests` | Unit tests | Every commit | No |
| `Tests.Common` | Shared fixtures | — | — |

## Running Tests

```bash
# Unit tests only (fast, no Docker)
dotnet test --filter "Category!=Integration"

# Integration tests (requires Docker)
dotnet test --filter "Category=Integration"

# All tests
dotnet test
```

## Integration Test Infrastructure

Integration tests use:
- **Testcontainers.MsSql** — spins up a real SQL Server 2022 container per test run
- **Respawn** — resets all tables to a clean state between each test class
- **FakeCurrentUserService** — simulates `KeycloakUserId = "test-keycloak-id"` (the `EnsureUserBehavior` pipeline auto-provisions the `AppUser` on the first request)
- **FakeBinanceService** — accepts all symbols as valid; avoids real Binance HTTP calls
- **AuthTokenFactory** — issues local symmetric-key JWTs accepted by the test API (Keycloak validation is replaced in `CustomWebApplicationFactory`)

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

# Integration tests — PR and nightly (Docker required)
dotnet test --filter "Category=Integration"
```
