using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Infrastructure.Persistence.Repositories;

public class SignalRepository : ISignalRepository
{
    private readonly ApplicationDbContext _context;
    public SignalRepository(ApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<Signal>> GetLatestByUserAsync(
        Guid userId, int count = 20, CancellationToken cancellationToken = default) =>
        await _context.Signals
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsRecentAsync(
        Guid userId, string symbol, SignalType signalType,
        TimeSpan within, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - within;
        return _context.Signals.AnyAsync(
            s => s.UserId == userId
              && s.Symbol == symbol
              && s.SignalType == signalType
              && s.CreatedAt >= cutoff,
            cancellationToken);
    }

    public void Add(Signal signal) => _context.Signals.Add(signal);
}
