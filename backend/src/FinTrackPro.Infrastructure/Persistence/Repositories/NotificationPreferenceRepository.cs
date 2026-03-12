using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Infrastructure.Persistence.Repositories;

public class NotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly ApplicationDbContext _context;
    public NotificationPreferenceRepository(ApplicationDbContext context) => _context = context;

    public Task<NotificationPreference?> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _context.NotificationPreferences.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public void Add(NotificationPreference preference) => _context.NotificationPreferences.Add(preference);
}
