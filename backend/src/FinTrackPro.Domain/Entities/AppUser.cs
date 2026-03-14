using FinTrackPro.Domain.Common;

namespace FinTrackPro.Domain.Entities;

public class AppUser : AggregateRoot
{
    public string KeycloakUserId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Provider { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private AppUser() { }

    public bool IsActive { get; private set; } = true;

    public static AppUser Create(string keycloakUserId, string email, string displayName, string provider)
    {
        return new AppUser
        {
            Id = Guid.NewGuid(),
            KeycloakUserId = keycloakUserId,
            Email = email.Trim().ToLowerInvariant(),
            DisplayName = displayName.Trim(),
            Provider = provider,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void UpdateProfile(string displayName, string email)
    {
        DisplayName = displayName.Trim();
        Email = email.Trim().ToLowerInvariant();
    }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
