using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;

namespace FinTrackPro.Domain.Repositories;

public interface ISignalRepository
{
    Task<IEnumerable<Signal>> GetLatestByUserAsync(Guid userId, int count = 20, CancellationToken cancellationToken = default);
    Task<bool> ExistsRecentAsync(Guid userId, string symbol, SignalType signalType, TimeSpan within, CancellationToken cancellationToken = default);
    Task<Signal?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> DeleteOldDismissedAsync(DateTime cutoff, CancellationToken cancellationToken = default);
    void Add(Signal signal);
}
