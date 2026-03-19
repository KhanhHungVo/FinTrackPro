using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Common.Behaviors;

public class EnsureUserBehavior<TRequest, TResponse>(
    ICurrentUserService currentUser,
    IUserRepository userRepository,
    IApplicationDbContext db) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var externalId = currentUser.ExternalUserId;
        if (!string.IsNullOrWhiteSpace(externalId))
        {
            var email = currentUser.Email ?? string.Empty;
            var displayName = currentUser.DisplayName ?? externalId;
            var provider = currentUser.ProviderName;

            var user = await GetByIdentityAsync(externalId, email, cancellationToken);
            if (user is null)
            {
                var newUser = AppUser.Create(
                    externalUserId: externalId,
                    email: email,
                    displayName: displayName,
                    provider: provider);

                var canonicalUser = await userRepository.EnsureCreatedAsync(newUser, cancellationToken);
                if (canonicalUser.SyncIdentity(externalId, email, displayName, provider))
                    await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                if (user.SyncIdentity(externalId, email, displayName, provider))
                    await db.SaveChangesAsync(cancellationToken);
            }
        }
        return await next();
    }

    private async Task<AppUser?> GetByIdentityAsync(
        string externalId,
        string email,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByExternalIdAsync(externalId, cancellationToken);
        if (user is not null || string.IsNullOrWhiteSpace(email))
            return user;

        return await userRepository.GetByEmailAsync(email, cancellationToken);
    }
}
