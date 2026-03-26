using FinTrackPro.Domain.Entities;

namespace FinTrackPro.Domain.Repositories;

public interface IUserIdentityRepository
{
    /// <summary>Returns the identity (with User eagerly loaded) for the given (externalId, provider) pair.</summary>
    Task<UserIdentity?> GetAsync(string externalId, string provider, CancellationToken cancellationToken = default);

    /// <summary>Returns all identities (with User eagerly loaded) for the given provider issuer URL.</summary>
    Task<List<UserIdentity>> GetByProviderAsync(string provider, CancellationToken cancellationToken = default);
}
