namespace FinTrackPro.Application.Common.Interfaces;

public record IamUserInfo(string ExternalId, bool IsEnabled);

public interface IIamProviderService
{
    /// <summary>
    /// Returns all users from the active IAM provider with their enabled status.
    /// </summary>
    Task<List<IamUserInfo>> GetAllUsersAsync(CancellationToken cancellationToken = default);
}
