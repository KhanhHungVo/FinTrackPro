using FinTrackPro.Domain.Entities;

namespace FinTrackPro.Domain.Repositories;

public interface INotificationPreferenceRepository
{
    Task<NotificationPreference?> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(NotificationPreference preference);
}
