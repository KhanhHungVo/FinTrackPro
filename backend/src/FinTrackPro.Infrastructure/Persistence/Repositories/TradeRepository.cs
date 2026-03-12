using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Infrastructure.Persistence.Repositories;

public class TradeRepository : ITradeRepository
{
    private readonly ApplicationDbContext _context;
    public TradeRepository(ApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<Trade>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _context.Trades.Where(t => t.UserId == userId).ToListAsync(cancellationToken);

    public Task<Trade?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Trades.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public void Add(Trade trade) => _context.Trades.Add(trade);
    public void Remove(Trade trade) => _context.Trades.Remove(trade);
}
