using FinTrackPro.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinTrackPro.Infrastructure.Services;

public class NullNotificationChannel(ILogger<NullNotificationChannel> logger) : INotificationChannel
{
    public Task SendAsync(string recipient, string title, string body, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Notification skipped (no channel configured): {Title}", title);
        return Task.CompletedTask;
    }
}
