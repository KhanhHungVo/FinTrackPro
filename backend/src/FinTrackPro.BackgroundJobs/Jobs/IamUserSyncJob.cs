using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace FinTrackPro.BackgroundJobs.Jobs;

/// <summary>
/// Runs nightly. Calls the active IAM provider's admin API to fetch all users,
/// then deactivates any local AppUser whose account is deleted or disabled in the IAM provider.
/// </summary>
public class IamUserSyncJob
{
    private readonly IIamProviderService _iamProvider;
    private readonly IUserRepository _userRepository;
    private readonly IApplicationDbContext _db;
    private readonly ILogger<IamUserSyncJob> _logger;

    public IamUserSyncJob(
        IIamProviderService iamProvider,
        IUserRepository userRepository,
        IApplicationDbContext db,
        ILogger<IamUserSyncJob> logger)
    {
        _iamProvider = iamProvider;
        _userRepository = userRepository;
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("IamUserSyncJob started");

        var iamUsers = await _iamProvider.GetAllUsersAsync(cancellationToken);
        if (iamUsers.Count == 0)
        {
            _logger.LogWarning("IamUserSyncJob: no users returned from IAM provider — skipping to avoid mass deactivation");
            return;
        }

        // Build a lookup: externalId → isEnabled
        var iamIndex = iamUsers.ToDictionary(u => u.ExternalId, u => u.IsEnabled);

        var localUsers = await _userRepository.GetAllAsync(cancellationToken);
        var deactivated = 0;

        foreach (var user in localUsers)
        {
            var existsAndEnabled = iamIndex.TryGetValue(user.ExternalUserId, out var enabled) && enabled;

            if (!existsAndEnabled && user.IsActive)
            {
                user.Deactivate();
                deactivated++;
                _logger.LogInformation(
                    "Deactivated AppUser {UserId} (External ID: {ExternalId}) — deleted or disabled in IAM provider",
                    user.Id, user.ExternalUserId);
            }
        }

        if (deactivated > 0)
            await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("IamUserSyncJob completed — {Count} user(s) deactivated", deactivated);
    }
}
