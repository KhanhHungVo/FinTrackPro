using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;

namespace FinTrackPro.Application.Admin;

public record AdminUserDto(
    Guid Id,
    string? Email,
    string DisplayName,
    SubscriptionPlan Plan,
    DateTime? SubscriptionExpiresAt,
    bool IsActive)
{
    public static explicit operator AdminUserDto(AppUser user) =>
        new(user.Id, user.Email, user.DisplayName, user.Plan, user.SubscriptionExpiresAt, user.IsActive);
}
