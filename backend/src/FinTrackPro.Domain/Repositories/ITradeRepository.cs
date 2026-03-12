using FinTrackPro.Domain.Entities;

namespace FinTrackPro.Domain.Repositories;

public interface ITradeRepository
{
    Task<IEnumerable<Trade>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Trade?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    void Add(Trade trade);
    void Remove(Trade trade);
}
