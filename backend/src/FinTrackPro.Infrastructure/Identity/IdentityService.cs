using System.Security.Claims;
using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Models;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using FinTrackPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinTrackPro.Infrastructure.Identity;

internal record LoginContext(
    string ExternalId,
    string Provider,
    string? Email,
    bool EmailVerified,
    string DisplayName);

public class IdentityService(
    IUserIdentityRepository userIdentityRepository,
    IUserRepository userRepository,
    ApplicationDbContext db,
    ILogger<IdentityService> logger) : IIdentityService
{
    public async Task<CurrentUser> ResolveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var ctx = new LoginContext(
            ExternalId:    principal.GetExternalId(),
            Provider:      principal.GetProvider(),
            Email:         principal.GetEmail(),
            EmailVerified: principal.IsEmailVerified(),
            DisplayName:   principal.FindFirstValue("name")
                        ?? principal.FindFirstValue(ClaimTypes.Name)
                        ?? principal.GetExternalId());

        // Fast path — returning user (UserIdentity row exists)
        var identity = await userIdentityRepository.GetAsync(ctx.ExternalId, ctx.Provider, cancellationToken);
        if (identity is not null)
        {
            if (identity.User.UpdateProfile(ctx.Email, ctx.DisplayName))
                await db.SaveChangesAsync(cancellationToken);

            return CurrentUser.From(identity.User);
        }

        // Slow path — new user or new provider link
        var user = await ResolveUserAsync(ctx, cancellationToken);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // The AppUser row was stale in the change tracker (e.g. Keycloak volume was wiped and
            // the same email got a new sub — the Users row may have been truncated and re-provisioned
            // between our read and save). Clear the tracker and retry once with a fresh load.
            logger.LogWarning(ex, "Concurrency conflict linking externalId={ExternalId}, provider={Provider} — retrying with fresh state", ctx.ExternalId, ctx.Provider);

            db.ChangeTracker.Clear();

            var existing = await userIdentityRepository.GetAsync(ctx.ExternalId, ctx.Provider, cancellationToken);
            if (existing is not null)
                return CurrentUser.From(existing.User);

            var retryUser = await ResolveUserAsync(ctx, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return CurrentUser.From(retryUser);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Concurrent first-login race for externalId={ExternalId}, provider={Provider} — re-querying winner", ctx.ExternalId, ctx.Provider);

            db.ChangeTracker.Clear();

            var winner = await userIdentityRepository.GetAsync(ctx.ExternalId, ctx.Provider, cancellationToken);
            if (winner is null) throw;

            return CurrentUser.From(winner.User);
        }

        return CurrentUser.From(user);
    }

    private async Task<AppUser> ResolveUserAsync(LoginContext ctx, CancellationToken cancellationToken)
    {
        AppUser? user = null;

        if (ctx.Email is not null && ctx.EmailVerified)
            user = await userRepository.GetByEmailAsync(ctx.Email, cancellationToken);

        if (user is null)
        {
            user = AppUser.Create(ctx.Email, ctx.DisplayName);
            userRepository.Add(user);
        }
        else
        {
            // Reset state so EF relationship fixup from AddIdentity below does not mark
            // AppUser as Modified and emit a spurious UPDATE "Users" that races under
            // concurrent logins with the same provider.
            db.Entry(user).State = EntityState.Unchanged;
        }

        user.AddIdentity(ctx.ExternalId, ctx.Provider);
        return user;
    }
}
