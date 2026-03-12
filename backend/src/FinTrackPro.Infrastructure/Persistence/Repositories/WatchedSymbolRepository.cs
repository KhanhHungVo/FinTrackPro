using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Infrastructure.Persistence.Repositories;

public class WatchedSymbolRepository : IWatchedSymbolRepository
{
    private readonly ApplicationDbContext _context;
    public WatchedSymbolRepository(ApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<WatchedSymbol>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _context.WatchedSymbols.Where(w => w.UserId == userId).ToListAsync(cancellationToken);

    public async Task<IEnumerable<WatchedSymbol>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.WatchedSymbols.ToListAsync(cancellationToken);

    public Task<WatchedSymbol?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.WatchedSymbols.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public Task<bool> ExistsAsync(Guid userId, string symbol, CancellationToken cancellationToken = default) =>
        _context.WatchedSymbols.AnyAsync(
            w => w.UserId == userId && w.Symbol == symbol.ToUpperInvariant(), cancellationToken);

    public void Add(WatchedSymbol watchedSymbol) => _context.WatchedSymbols.Add(watchedSymbol);
    public void Remove(WatchedSymbol watchedSymbol) => _context.WatchedSymbols.Remove(watchedSymbol);
}
