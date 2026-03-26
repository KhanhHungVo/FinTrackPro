using System.Security.Claims;
using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Models;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using FinTrackPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinTrackPro.Infrastructure.Identity;

public class IdentityService(
    IUserIdentityRepository userIdentityRepository,
    IUserRepository userRepository,
    ApplicationDbContext db,
    ILogger<IdentityService> logger) : IIdentityService
{
    public async Task<CurrentUser> ResolveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var externalId  = principal.GetExternalId();
        var provider    = principal.GetProvider();
        var email       = principal.GetEmail();
        var emailVerified = principal.IsEmailVerified();
        var displayName = principal.FindFirstValue("name")
                       ?? principal.FindFirstValue(ClaimTypes.Name)
                       ?? externalId;

        // Fast path — returning user (UserIdentity row exists)
        var identity = await userIdentityRepository.GetAsync(externalId, provider, cancellationToken);
        if (identity is not null)
        {
            if (identity.User.UpdateProfile(email, displayName))
                await db.SaveChangesAsync(cancellationToken);

            return CurrentUser.From(identity.User);
        }

        // Slow path — new user or new provider link
        AppUser? user = null;

        if (email is not null && emailVerified)
            user = await userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            user = AppUser.Create(email, displayName);
            userRepository.Add(user);
        }

        user.AddIdentity(externalId, provider);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Concurrent first-login race for externalId={ExternalId}, provider={Provider} — re-querying winner", externalId, provider);

            db.ChangeTracker.Clear();

            var winner = await userIdentityRepository.GetAsync(externalId, provider, cancellationToken);
            if (winner is null) throw;

            return CurrentUser.From(winner.User);
        }

        return CurrentUser.From(user);
    }
}
