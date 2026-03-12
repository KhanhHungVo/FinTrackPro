using MediatR;

namespace FinTrackPro.Application.Notifications.Commands.SaveNotificationPreference;

public record SaveNotificationPreferenceCommand(
    string TelegramChatId,
    bool IsEnabled
) : IRequest;
