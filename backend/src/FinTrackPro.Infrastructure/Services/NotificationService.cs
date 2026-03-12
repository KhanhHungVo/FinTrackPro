using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace FinTrackPro.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly INotificationChannel _telegramChannel;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        INotificationPreferenceRepository preferenceRepository,
        INotificationChannel telegramChannel,
        ILogger<NotificationService> logger)
    {
        _preferenceRepository = preferenceRepository;
        _telegramChannel = telegramChannel;
        _logger = logger;
    }

    public async Task NotifyAsync(Guid userId, string title, string body, CancellationToken cancellationToken = default)
    {
        var pref = await _preferenceRepository.GetByUserAsync(userId, cancellationToken);
        if (pref is null || !pref.IsEnabled || string.IsNullOrWhiteSpace(pref.TelegramChatId))
        {
            _logger.LogDebug("Notification skipped for user {UserId} — no active preference.", userId);
            return;
        }

        await _telegramChannel.SendAsync(pref.TelegramChatId, title, body, cancellationToken);
    }
}
