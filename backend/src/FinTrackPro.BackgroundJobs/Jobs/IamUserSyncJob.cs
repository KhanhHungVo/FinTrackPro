using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace FinTrackPro.BackgroundJobs.Jobs;

/// <summary>
/// Runs nightly. Calls the active IAM provider's admin API to fetch all users,
/// then deactivates any local AppUser whose account is deleted or disabled in the IAM provider.
/// </summary>
public class IamUserSyncJob(
    IIamProviderService iamProvider,
    IUserIdentityRepository userIdentityRepository,
    IApplicationDbContext db,
    ILogger<IamUserSyncJob> logger)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("IamUserSyncJob started");

        var iamUsers = await iamProvider.GetAllUsersAsync(cancellationToken);
        if (iamUsers.Count == 0)
        {
            logger.LogWarning("IamUserSyncJob: no users returned from IAM provider — skipping to avoid mass deactivation");
            return;
        }

        // Build a lookup: externalId → isEnabled
        var iamIndex = iamUsers.ToDictionary(u => u.ExternalId, u => u.IsEnabled);

        var identities = await userIdentityRepository.GetByProviderAsync(
            iamProvider.ProviderIssuer, cancellationToken);

        var deactivated = 0;

        foreach (var identity in identities)
        {
            var existsAndEnabled = iamIndex.TryGetValue(identity.ExternalUserId, out var enabled) && enabled;

            if (!existsAndEnabled && identity.User.IsActive)
            {
                identity.User.Deactivate();
                deactivated++;
                logger.LogInformation(
                    "Deactivated AppUser {UserId} (ExternalId: {ExternalId}, Provider: {Provider}) — deleted or disabled in IAM provider",
                    identity.User.Id, identity.ExternalUserId, identity.Provider);
            }
        }

        if (deactivated > 0)
            await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("IamUserSyncJob completed — {Count} user(s) deactivated", deactivated);
    }
}
