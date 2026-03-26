namespace FinTrackPro.Application.Common.Interfaces;

public record IamUserInfo(string ExternalId, bool IsEnabled);

public interface IIamProviderService
{
    /// <summary>
    /// The JWT <c>iss</c> claim value emitted by this provider (used to filter UserIdentity rows).
    /// </summary>
    string ProviderIssuer { get; }

    /// <summary>
    /// Returns all users from the active IAM provider with their enabled status.
    /// </summary>
    Task<List<IamUserInfo>> GetAllUsersAsync(CancellationToken cancellationToken = default);
}
