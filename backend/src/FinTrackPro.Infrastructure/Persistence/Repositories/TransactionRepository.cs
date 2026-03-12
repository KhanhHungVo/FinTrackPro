using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Infrastructure.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly ApplicationDbContext _context;
    public TransactionRepository(ApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<Transaction>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _context.Transactions.Where(t => t.UserId == userId).ToListAsync(cancellationToken);

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Transactions.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public void Add(Transaction transaction) => _context.Transactions.Add(transaction);
    public void Remove(Transaction transaction) => _context.Transactions.Remove(transaction);
}
