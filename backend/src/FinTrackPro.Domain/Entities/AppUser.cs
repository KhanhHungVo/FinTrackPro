using FinTrackPro.Domain.Common;

namespace FinTrackPro.Domain.Entities;

public class AppUser : AggregateRoot
{
    private readonly List<UserIdentity> _identities = new();

    public string? Email { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<UserIdentity> Identities => _identities.AsReadOnly();

    private AppUser() { }

    public static AppUser Create(string? email, string displayName)
    {
        return new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email?.Trim().ToLowerInvariant(),
            DisplayName = displayName.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void AddIdentity(string externalId, string provider)
    {
        if (_identities.Any(x => x.ExternalUserId == externalId && x.Provider == provider))
            return;
        _identities.Add(new UserIdentity(externalId, provider, Id));
    }

    /// <summary>
    /// Updates profile fields and reactivates the user if deactivated.
    /// Returns true if anything changed (triggers a DB save).
    /// </summary>
    public bool UpdateProfile(string? email, string displayName)
    {
        var normEmail = email?.Trim().ToLowerInvariant() ?? Email;
        var normName  = displayName.Trim();
        var changed   = Email != normEmail || DisplayName != normName || !IsActive;

        Email       = normEmail;
        DisplayName = normName;
        IsActive    = true;

        return changed;
    }

    public void Deactivate() => IsActive = false;
}
