using FinTrackPro.Domain.Common;
using FinTrackPro.Domain.Enums;

namespace FinTrackPro.Domain.Entities;

public class AppUser : AggregateRoot
{
    private readonly List<UserIdentity> _identities = new();

    public string? Email { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string PreferredLanguage { get; private set; } = "en";
    public string PreferredCurrency { get; private set; } = "USD";

    public bool IsActive { get; private set; } = true;

    // Subscription
    public SubscriptionPlan Plan { get; private set; } = SubscriptionPlan.Free;
    public string? PaymentCustomerId { get; private set; }
    public string? PaymentSubscriptionId { get; private set; }
    public DateTime? SubscriptionExpiresAt { get; private set; }

    public IReadOnlyCollection<UserIdentity> Identities => _identities.AsReadOnly();

    private AppUser() { }

    public static AppUser Create(string? email, string displayName)
    {
        return new AppUser
        {
            Id = Guid.NewGuid(),
            Email = email?.Trim().ToLowerInvariant(),
            DisplayName = displayName.Trim(),
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

    public void UpdatePreferences(string language, string currency)
    {
        PreferredLanguage = language.Trim().ToLowerInvariant();
        PreferredCurrency = currency.Trim().ToUpperInvariant();
    }

    public void Deactivate() => IsActive = false;

    public void SetPaymentCustomerId(string customerId)
        => PaymentCustomerId = customerId;

    public void ActivateSubscription(string subscriptionId, DateTime expiresAt)
    {
        Plan                   = SubscriptionPlan.Pro;
        PaymentSubscriptionId  = subscriptionId;
        SubscriptionExpiresAt  = expiresAt;
    }

    public void CancelSubscription()
    {
        Plan                  = SubscriptionPlan.Free;
        PaymentSubscriptionId = null;
        SubscriptionExpiresAt = null;
        // PaymentCustomerId is intentionally kept so re-subscription reuses the same customer record.
    }

    public void RenewSubscription(BillingPeriod period)
    {
        var baseDate = SubscriptionExpiresAt.HasValue && SubscriptionExpiresAt.Value > DateTime.UtcNow
            ? SubscriptionExpiresAt.Value
            : DateTime.UtcNow;

        SubscriptionExpiresAt = period == BillingPeriod.Yearly
            ? baseDate.AddYears(1)
            : baseDate.AddMonths(1);

        Plan                  = SubscriptionPlan.Pro;
        PaymentSubscriptionId = $"bank_{Guid.NewGuid()}";
    }
}
