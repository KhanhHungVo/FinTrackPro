using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;

namespace FinTrackPro.Application.Subscription.Queries.GetSubscriptionStatus;

public record SubscriptionStatusDto(SubscriptionPlan Plan, bool IsActive, DateTime? ExpiresAt)
{
    public static explicit operator SubscriptionStatusDto(AppUser user) =>
        new(
            user.Plan,
            user.Plan == SubscriptionPlan.Pro && (user.SubscriptionExpiresAt is null || user.SubscriptionExpiresAt > DateTime.UtcNow),
            user.SubscriptionExpiresAt);
}
