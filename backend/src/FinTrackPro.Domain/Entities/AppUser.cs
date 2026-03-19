using FinTrackPro.Domain.Common;

namespace FinTrackPro.Domain.Entities;

public class AppUser : AggregateRoot
{
    public string ExternalUserId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string Provider { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private AppUser() { }

    public bool IsActive { get; private set; } = true;

    public static AppUser Create(string externalUserId, string email, string displayName, string provider)
    {
        return new AppUser
        {
            Id = Guid.NewGuid(),
            ExternalUserId = externalUserId,
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

    public bool SyncIdentity(string externalUserId, string email, string displayName, string provider)
    {
        var normalizedExternalUserId = externalUserId.Trim();
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var normalizedDisplayName = displayName.Trim();
        var normalizedProvider = provider.Trim();

        var changed =
            ExternalUserId != normalizedExternalUserId ||
            Email != normalizedEmail ||
            DisplayName != normalizedDisplayName ||
            Provider != normalizedProvider ||
            !IsActive;

        ExternalUserId = normalizedExternalUserId;
        Email = normalizedEmail;
        DisplayName = normalizedDisplayName;
        Provider = normalizedProvider;
        IsActive = true;

        return changed;
    }

    public void Deactivate() => IsActive = false;

    public void Reactivate() => IsActive = true;
}
