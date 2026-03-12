namespace FinTrackPro.Application.Common.Interfaces;

public interface INotificationChannel
{
    Task SendAsync(string recipient, string title, string body, CancellationToken cancellationToken = default);
}
