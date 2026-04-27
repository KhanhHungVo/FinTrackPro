using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Infrastructure.Persistence.Repositories;

public class SignalRepository(ApplicationDbContext context) : ISignalRepository
{
    public async Task<IEnumerable<Signal>> GetLatestByUserAsync(
        Guid userId, int count = 20, CancellationToken cancellationToken = default) =>
        await context.Signals
            .Where(s => s.UserId == userId && s.DismissedAt == null)
            .OrderByDescending(s => s.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsRecentAsync(
        Guid userId, string symbol, SignalType signalType,
        TimeSpan within, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - within;
        return context.Signals.AnyAsync(
            s => s.UserId == userId
              && s.Symbol == symbol
              && s.SignalType == signalType
              && s.CreatedAt >= cutoff,
            cancellationToken);
    }

    public Task<Signal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Signals.FindAsync([id], cancellationToken).AsTask();

    public Task<int> DeleteOldDismissedAsync(DateTime cutoff, CancellationToken cancellationToken = default) =>
        context.Signals
            .Where(s => s.DismissedAt != null && s.DismissedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

    public void Add(Signal signal) => context.Signals.Add(signal);
}
