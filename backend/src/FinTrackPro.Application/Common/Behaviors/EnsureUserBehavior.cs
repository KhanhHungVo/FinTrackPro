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
            var user = await userRepository.GetByExternalIdAsync(externalId, cancellationToken);
            if (user is null)
            {
                userRepository.Add(AppUser.Create(
                    externalUserId: externalId,
                    email: currentUser.Email ?? string.Empty,
                    displayName: currentUser.DisplayName ?? externalId,
                    provider: currentUser.ProviderName));
                await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                var jwtDisplayName = currentUser.DisplayName ?? externalId;
                var jwtEmail = currentUser.Email ?? string.Empty;
                var needsUpdate = user.DisplayName != jwtDisplayName || user.Email != jwtEmail;
                var needsReactivation = !user.IsActive;

                if (needsReactivation) user.Reactivate();
                if (needsUpdate) user.UpdateProfile(jwtDisplayName, jwtEmail);
                if (needsUpdate || needsReactivation) await db.SaveChangesAsync(cancellationToken);
            }
        }
        return await next();
    }
}
