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
        catch (DbUpdateConcurrencyException ex)
        {
            // The AppUser row was stale in the change tracker (e.g. Keycloak volume was wiped and
            // the same email got a new sub — the Users row may have been truncated and re-provisioned
            // between our read and save). Clear the tracker and retry once with a fresh load.
            logger.LogWarning(ex, "Concurrency conflict linking externalId={ExternalId}, provider={Provider} — retrying with fresh state", externalId, provider);

            db.ChangeTracker.Clear();

            var existing = await userIdentityRepository.GetAsync(externalId, provider, cancellationToken);
            if (existing is not null)
                return CurrentUser.From(existing.User);

            // Re-resolve user by email and re-link, or create new
            AppUser? retryUser = null;
            if (email is not null && emailVerified)
                retryUser = await userRepository.GetByEmailAsync(email, cancellationToken);

            if (retryUser is null)
            {
                retryUser = AppUser.Create(email, displayName);
                userRepository.Add(retryUser);
            }

            retryUser.AddIdentity(externalId, provider);
            await db.SaveChangesAsync(cancellationToken);
            return CurrentUser.From(retryUser);
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
