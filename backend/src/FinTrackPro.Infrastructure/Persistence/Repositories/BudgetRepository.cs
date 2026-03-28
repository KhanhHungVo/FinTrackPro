using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Infrastructure.Persistence.Repositories;

public class BudgetRepository(ApplicationDbContext context) : IBudgetRepository
{
    public async Task<IEnumerable<Budget>> GetByUserAndMonthAsync(
        Guid userId, string month, CancellationToken cancellationToken = default) =>
        await context.Budgets
            .Where(b => b.UserId == userId && b.Month == month)
            .ToListAsync(cancellationToken);

    public Task<Budget?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Budgets.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<Budget?> GetByUserCategoryMonthAsync(
        Guid userId, string category, string month, CancellationToken cancellationToken = default) =>
        context.Budgets.FirstOrDefaultAsync(
            b => b.UserId == userId && b.Category == category && b.Month == month,
            cancellationToken);

    public Task<bool> ExistsAsync(
        Guid userId, string category, string month, CancellationToken cancellationToken = default) =>
        context.Budgets.AnyAsync(
            b => b.UserId == userId && b.Category == category && b.Month == month,
            cancellationToken);

    public void Add(Budget budget) => context.Budgets.Add(budget);
    public void Remove(Budget budget) => context.Budgets.Remove(budget);
}
