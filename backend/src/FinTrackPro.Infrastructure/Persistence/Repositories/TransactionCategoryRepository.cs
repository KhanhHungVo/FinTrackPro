using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Infrastructure.Persistence.Repositories;

public class TransactionCategoryRepository(ApplicationDbContext context) : ITransactionCategoryRepository
{
    public async Task<IEnumerable<TransactionCategory>> GetByUserAsync(
        Guid userId, TransactionType? type = null, CancellationToken cancellationToken = default)
    {
        var query = context.TransactionCategories
            .Where(c => c.IsActive && (c.UserId == null || c.UserId == userId));

        if (type.HasValue)
            query = query.Where(c => c.Type == type.Value);

        return await query
            .OrderBy(c => c.IsSystem ? 0 : 1)
            .ThenBy(c => c.SortOrder)
            .ThenBy(c => c.LabelEn)
            .ToListAsync(cancellationToken);
    }

    public async Task<TransactionCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.TransactionCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<bool> SlugExistsForUserAsync(Guid userId, string slug, CancellationToken cancellationToken = default)
        => await context.TransactionCategories
            .AnyAsync(c => (c.UserId == null || c.UserId == userId) && c.Slug == slug, cancellationToken);

    public void Add(TransactionCategory category) => context.TransactionCategories.Add(category);
}
