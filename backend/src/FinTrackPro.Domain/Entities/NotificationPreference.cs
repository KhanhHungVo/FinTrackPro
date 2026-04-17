using FinTrackPro.Domain.Common;
using FinTrackPro.Domain.Enums;

namespace FinTrackPro.Domain.Entities;

public class NotificationPreference : AuditableEntity
{
    public Guid UserId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public string? TelegramChatId { get; private set; }
    public string? Email { get; private set; }
    public bool IsEnabled { get; private set; }


    private NotificationPreference() { }

    public static NotificationPreference CreateTelegram(Guid userId, string telegramChatId)
    {
        return new NotificationPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Channel = NotificationChannel.Telegram,
            TelegramChatId = telegramChatId,
            IsEnabled = true
        };
    }

    public void Update(string? telegramChatId, bool isEnabled)
    {
        TelegramChatId = telegramChatId;
        IsEnabled = isEnabled;
    }
}
