using FinTrackPro.Domain.Entities;

namespace FinTrackPro.Domain.Repositories;

public interface ITransactionRepository
{
    Task<IEnumerable<Transaction>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetPagedAsync(
        Guid userId, TransactionPageQuery query, CancellationToken ct = default);
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountByUserAndMonthAsync(Guid userId, string month, CancellationToken cancellationToken = default);
    void Add(Transaction transaction);
    void Remove(Transaction transaction);
}
