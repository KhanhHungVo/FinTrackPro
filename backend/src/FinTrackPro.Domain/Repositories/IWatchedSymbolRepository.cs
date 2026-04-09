using FinTrackPro.Domain.Entities;

namespace FinTrackPro.Domain.Repositories;

public interface IWatchedSymbolRepository
{
    Task<IEnumerable<WatchedSymbol>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<WatchedSymbol>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<WatchedSymbol?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, string symbol, CancellationToken cancellationToken = default);
    Task<int> CountByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(WatchedSymbol watchedSymbol);
    void Remove(WatchedSymbol watchedSymbol);
}
