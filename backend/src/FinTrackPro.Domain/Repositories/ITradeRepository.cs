using FinTrackPro.Domain.Entities;

namespace FinTrackPro.Domain.Repositories;

public interface ITradeRepository
{
    Task<IEnumerable<Trade>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Trade> Items, int TotalCount)> GetPagedAsync(
        Guid userId, TradePageQuery query, CancellationToken ct = default);
    Task<Trade?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(Trade trade);
    void Remove(Trade trade);
}
