using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Infrastructure.Persistence.Repositories;

public class NotificationPreferenceRepository(ApplicationDbContext context) : INotificationPreferenceRepository
{
    public Task<NotificationPreference?> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public void Add(NotificationPreference preference) => context.NotificationPreferences.Add(preference);
}
