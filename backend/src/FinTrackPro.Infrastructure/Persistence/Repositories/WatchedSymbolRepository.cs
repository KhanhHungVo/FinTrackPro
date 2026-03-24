using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Infrastructure.Persistence.Repositories;

public class WatchedSymbolRepository(ApplicationDbContext context) : IWatchedSymbolRepository
{
    public async Task<IEnumerable<WatchedSymbol>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.WatchedSymbols.Where(w => w.UserId == userId).ToListAsync(cancellationToken);

    public async Task<IEnumerable<WatchedSymbol>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await context.WatchedSymbols.ToListAsync(cancellationToken);

    public Task<WatchedSymbol?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.WatchedSymbols.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid userId, string symbol, CancellationToken cancellationToken = default) =>
        context.WatchedSymbols.AnyAsync(
            w => w.UserId == userId && w.Symbol == symbol.ToUpperInvariant(), cancellationToken);

    public void Add(WatchedSymbol watchedSymbol) => context.WatchedSymbols.Add(watchedSymbol);
    public void Remove(WatchedSymbol watchedSymbol) => context.WatchedSymbols.Remove(watchedSymbol);
}
