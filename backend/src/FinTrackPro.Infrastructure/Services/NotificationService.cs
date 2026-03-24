using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace FinTrackPro.Infrastructure.Services;

public class NotificationService(
    INotificationPreferenceRepository preferenceRepository,
    INotificationChannel telegramChannel,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task NotifyAsync(Guid userId, string title, string body, CancellationToken cancellationToken = default)
    {
        var pref = await preferenceRepository.GetByUserAsync(userId, cancellationToken);
        if (pref is null || !pref.IsEnabled || string.IsNullOrWhiteSpace(pref.TelegramChatId))
        {
            logger.LogDebug("Notification skipped for user {UserId} — no active preference.", userId);
            return;
        }

        await telegramChannel.SendAsync(pref.TelegramChatId, title, body, cancellationToken);
    }
}
