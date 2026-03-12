using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;

namespace FinTrackPro.Application.Notifications.Queries.GetNotificationPreference;

public record NotificationPreferenceDto(
    Guid Id,
    NotificationChannel Channel,
    string? TelegramChatId,
    bool IsEnabled)
{
    public static explicit operator NotificationPreferenceDto(NotificationPreference p) => new(
        p.Id, p.Channel, p.TelegramChatId, p.IsEnabled);
}
