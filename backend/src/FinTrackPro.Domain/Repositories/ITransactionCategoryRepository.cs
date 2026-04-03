using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;

namespace FinTrackPro.Domain.Repositories;

public interface ITransactionCategoryRepository
{
    Task<IEnumerable<TransactionCategory>> GetByUserAsync(Guid userId, TransactionType? type = null, CancellationToken cancellationToken = default);
    Task<TransactionCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsForUserAsync(Guid userId, string slug, CancellationToken cancellationToken = default);
    void Add(TransactionCategory category);
}
