using FinTrackPro.Domain.Entities;

namespace FinTrackPro.Domain.Repositories;

public interface IBudgetRepository
{
    Task<IEnumerable<Budget>> GetByUserAndMonthAsync(Guid userId, string month, CancellationToken cancellationToken = default);
    Task<Budget?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Budget?> GetByUserCategoryMonthAsync(Guid userId, string category, string month, CancellationToken cancellationToken = default);
    void Add(Budget budget);
    void Remove(Budget budget);
}
