namespace FinTrackPro.Application.Common.Interfaces;

public interface INotificationService
{
    Task NotifyAsync(Guid userId, string title, string body, CancellationToken cancellationToken = default);
}
