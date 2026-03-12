namespace FinTrackPro.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? KeycloakUserId { get; }
    string? Email { get; }
    string? DisplayName { get; }
    bool IsAuthenticated { get; }
}
