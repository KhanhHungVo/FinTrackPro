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
        var keycloakId = currentUser.KeycloakUserId;
        if (!string.IsNullOrWhiteSpace(keycloakId))
        {
            var user = await userRepository.GetByKeycloakIdAsync(keycloakId, cancellationToken);
            if (user is null)
            {
                userRepository.Add(AppUser.Create(
                    keycloakUserId: keycloakId,
                    email: currentUser.Email ?? string.Empty,
                    displayName: currentUser.DisplayName ?? keycloakId,
                    provider: "keycloak"));
                await db.SaveChangesAsync(cancellationToken);
            }
        }
        return await next();
    }
}
