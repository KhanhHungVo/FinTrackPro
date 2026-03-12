using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Infrastructure.Persistence.Repositories;

public class BudgetRepository : IBudgetRepository
{
    private readonly ApplicationDbContext _context;
    public BudgetRepository(ApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<Budget>> GetByUserAndMonthAsync(
        Guid userId, string month, CancellationToken cancellationToken = default) =>
        await _context.Budgets
            .Where(b => b.UserId == userId && b.Month == month)
            .ToListAsync(cancellationToken);

    public Task<Budget?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Budgets.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public Task<Budget?> GetByUserCategoryMonthAsync(
        Guid userId, string category, string month, CancellationToken cancellationToken = default) =>
        _context.Budgets.FirstOrDefaultAsync(
            b => b.UserId == userId && b.Category == category && b.Month == month,
            cancellationToken);

    public void Add(Budget budget) => _context.Budgets.Add(budget);
    public void Remove(Budget budget) => _context.Budgets.Remove(budget);
}
