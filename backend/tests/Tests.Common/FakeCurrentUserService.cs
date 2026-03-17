using FinTrackPro.Application.Common.Interfaces;

namespace Tests.Common;

/// <summary>
/// In-process replacement for ICurrentUserService used in integration tests.
/// Set KeycloakUserId before each test to simulate an authenticated user.
/// </summary>
public class FakeCurrentUserService : ICurrentUserService
{
    public string? KeycloakUserId { get; set; } = "test-keycloak-id";
    public string? Email { get; set; } = "test@fintrackpro.dev";
    public string? DisplayName { get; set; } = "Test User";
    public bool IsAuthenticated => KeycloakUserId is not null;
}
