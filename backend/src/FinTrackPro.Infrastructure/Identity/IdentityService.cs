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
        // Query AppUser directly to avoid loading UserIdentity into the tracker,
        // which would cause EF relationship fixup to mark it as Modified on save.
        var returning = await userRepository.GetByExternalIdAsync(ctx.ExternalId, ctx.Provider, cancellationToken);

        if (returning is not null)
        {
            if (returning.UpdateProfile(ctx.Email, ctx.DisplayName))
                await db.SaveChangesAsync(cancellationToken);

            return CurrentUser.From(returning);
        }

        // Slow path — new user or new provider link
        var user = await ResolveUserAsync(ctx, cancellationToken);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex,
                "Concurrent first-login race externalId={ExternalId} provider={Provider} — re-querying winner",
                ctx.ExternalId, ctx.Provider);

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
            // Attach the untracked AppUser as Unchanged so only the new UserIdentity
            // (Added via AddIdentity below) is written — no spurious UPDATE on AppUser.
            db.Attach(user);
        }

        user.AddIdentity(ctx.ExternalId, ctx.Provider);

        return user;
    }
}
