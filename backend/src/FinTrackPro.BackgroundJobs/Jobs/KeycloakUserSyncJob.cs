using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace FinTrackPro.BackgroundJobs.Jobs;

/// <summary>
/// Runs nightly. Calls the Keycloak Admin REST API to fetch all realm users,
/// then deactivates any local AppUser whose Keycloak account is deleted or disabled.
/// </summary>
public class KeycloakUserSyncJob
{
    private readonly IKeycloakAdminService _keycloakAdmin;
    private readonly IUserRepository _userRepository;
    private readonly IApplicationDbContext _db;
    private readonly ILogger<KeycloakUserSyncJob> _logger;

    public KeycloakUserSyncJob(
        IKeycloakAdminService keycloakAdmin,
        IUserRepository userRepository,
        IApplicationDbContext db,
        ILogger<KeycloakUserSyncJob> logger)
    {
        _keycloakAdmin = keycloakAdmin;
        _userRepository = userRepository;
        _db = db;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("KeycloakUserSyncJob started");

        var keycloakUsers = await _keycloakAdmin.GetAllUsersAsync(cancellationToken);
        if (keycloakUsers.Count == 0)
        {
            _logger.LogWarning("KeycloakUserSyncJob: no users returned from Keycloak — skipping to avoid mass deactivation");
            return;
        }

        // Build a lookup: keycloakId → enabled
        var keycloakIndex = keycloakUsers.ToDictionary(u => u.Id, u => u.Enabled);

        var localUsers = await _userRepository.GetAllAsync(cancellationToken);
        var deactivated = 0;

        foreach (var user in localUsers)
        {
            var existsAndEnabled = keycloakIndex.TryGetValue(user.KeycloakUserId, out var enabled) && enabled;

            if (!existsAndEnabled && user.IsActive)
            {
                user.Deactivate();
                deactivated++;
                _logger.LogInformation(
                    "Deactivated AppUser {UserId} (Keycloak ID: {KeycloakId}) — deleted or disabled in Keycloak",
                    user.Id, user.KeycloakUserId);
            }
        }

        if (deactivated > 0)
            await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("KeycloakUserSyncJob completed — {Count} user(s) deactivated", deactivated);
    }
}
