using FinTrackPro.Application.Common.Interfaces;

namespace Tests.Common;

/// <summary>
/// In-process replacement for ICurrentUserService used in integration tests.
/// Set ExternalUserId before each test to simulate an authenticated user.
/// </summary>
public class FakeCurrentUserService : ICurrentUserService
{
    public string? ExternalUserId { get; set; } = "test-external-id";
    public string? Email { get; set; } = "test@fintrackpro.dev";
    public string? DisplayName { get; set; } = "Test User";
    public bool IsAuthenticated => ExternalUserId is not null;
    public string ProviderName { get; set; } = "keycloak";
}
