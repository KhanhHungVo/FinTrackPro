using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Infrastructure.Persistence.Repositories;

public class TransactionRepository(ApplicationDbContext context) : ITransactionRepository
{
    public async Task<IEnumerable<Transaction>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.Transactions.Where(t => t.UserId == userId).ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetPagedAsync(
        Guid userId, TransactionPageQuery query, CancellationToken ct = default)
    {
        var q = context.Transactions.Where(t => t.UserId == userId);

        if (!string.IsNullOrWhiteSpace(query.Month))
            q = q.Where(t => t.BudgetMonth == query.Month);

        if (!string.IsNullOrWhiteSpace(query.Type) &&
            Enum.TryParse<TransactionType>(query.Type, ignoreCase: true, out var parsedType))
            q = q.Where(t => t.Type == parsedType);

        if (query.CategoryId.HasValue)
            q = q.Where(t => t.CategoryId == query.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.ToLower();
            q = q.Where(t =>
                (t.Note != null && t.Note.ToLower().Contains(term)) ||
                t.Category.ToLower().Contains(term));
        }

        var totalCount = await q.CountAsync(ct);

        q = (query.SortBy.ToLower(), query.SortDir.ToLower()) switch
        {
            ("amount", "asc")  => q.OrderBy(t => t.Amount),
            ("amount", _)      => q.OrderByDescending(t => t.Amount),
            ("category", "asc")  => q.OrderBy(t => t.Category),
            ("category", _)      => q.OrderByDescending(t => t.Category),
            (_, "asc")         => q.OrderBy(t => t.CreatedAt),
            _                  => q.OrderByDescending(t => t.CreatedAt),
        };

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<int> CountByUserAndMonthAsync(Guid userId, string month, CancellationToken cancellationToken = default) =>
        context.Transactions.CountAsync(t => t.UserId == userId && t.BudgetMonth == month, cancellationToken);

    public void Add(Transaction transaction) => context.Transactions.Add(transaction);
    public void Remove(Transaction transaction) => context.Transactions.Remove(transaction);
}
