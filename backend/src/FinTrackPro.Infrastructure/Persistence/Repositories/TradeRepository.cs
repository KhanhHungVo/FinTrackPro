using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Infrastructure.Persistence.Repositories;

public class TradeRepository(ApplicationDbContext context) : ITradeRepository
{
    public async Task<IEnumerable<Trade>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.Trades.Where(t => t.UserId == userId).ToListAsync(cancellationToken);

    public Task<Trade?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Trades.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public void Add(Trade trade) => context.Trades.Add(trade);
    public void Remove(Trade trade) => context.Trades.Remove(trade);
}
