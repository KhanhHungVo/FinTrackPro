namespace FinTrackPro.Domain.Entities;

public class UserIdentity
{
    public Guid Id { get; private set; }
    public string ExternalUserId { get; private set; } = string.Empty;
    public string Provider { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public AppUser User { get; private set; } = null!;

    private UserIdentity() { }

    public UserIdentity(string externalUserId, string provider, Guid userId)
    {
        Id = Guid.NewGuid();
        ExternalUserId = externalUserId;
        Provider = provider;
        UserId = userId;
    }
}
