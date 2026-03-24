using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Infrastructure.Persistence.Repositories;

public class TransactionRepository(ApplicationDbContext context) : ITransactionRepository
{
    public async Task<IEnumerable<Transaction>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.Transactions.Where(t => t.UserId == userId).ToListAsync(cancellationToken);

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public void Add(Transaction transaction) => context.Transactions.Add(transaction);
    public void Remove(Transaction transaction) => context.Transactions.Remove(transaction);
}
