namespace FinTrackPro.Application.Common.Interfaces;

public record KeycloakUserInfo(string Id, bool Enabled);

public interface IKeycloakAdminService
{
    /// <summary>
    /// Returns all users in the realm with their enabled status.
    /// </summary>
    Task<List<KeycloakUserInfo>> GetAllUsersAsync(CancellationToken cancellationToken = default);
}
