using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Infrastructure.Persistence.Repositories;

public class TradeRepository(ApplicationDbContext context) : ITradeRepository
{
    public async Task<IEnumerable<Trade>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.Trades.Where(t => t.UserId == userId).ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Trade> Items, int TotalCount)> GetPagedAsync(
        Guid userId, TradePageQuery query, CancellationToken ct = default)
    {
        var q = context.Trades.Where(t => t.UserId == userId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.ToLower();
            q = q.Where(t => t.Symbol.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            Enum.TryParse<TradeStatus>(query.Status, ignoreCase: true, out var parsedStatus))
            q = q.Where(t => t.Status == parsedStatus);

        if (!string.IsNullOrWhiteSpace(query.Direction) &&
            Enum.TryParse<TradeDirection>(query.Direction, ignoreCase: true, out var parsedDir))
            q = q.Where(t => t.Direction == parsedDir);

        if (query.DateFrom.HasValue)
        {
            var from = query.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            q = q.Where(t => t.CreatedAt >= from);
        }

        if (query.DateTo.HasValue)
        {
            var to = query.DateTo.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            q = q.Where(t => t.CreatedAt <= to);
        }

        var totalCount = await q.CountAsync(ct);

        q = (query.SortBy.ToLower(), query.SortDir.ToLower()) switch
        {
            ("pnl", "asc") => q.OrderBy(t =>
                t.Status == TradeStatus.Closed && t.ExitPrice != null
                    ? (t.Direction == TradeDirection.Long
                        ? (t.ExitPrice.Value - t.EntryPrice) * t.PositionSize - t.Fees
                        : (t.EntryPrice - t.ExitPrice.Value) * t.PositionSize - t.Fees)
                    : 0m),
            ("pnl", _) => q.OrderByDescending(t =>
                t.Status == TradeStatus.Closed && t.ExitPrice != null
                    ? (t.Direction == TradeDirection.Long
                        ? (t.ExitPrice.Value - t.EntryPrice) * t.PositionSize - t.Fees
                        : (t.EntryPrice - t.ExitPrice.Value) * t.PositionSize - t.Fees)
                    : 0m),
            ("symbol", "asc")     => q.OrderBy(t => t.Symbol),
            ("symbol", _)         => q.OrderByDescending(t => t.Symbol),
            ("entryprice", "asc") => q.OrderBy(t => t.EntryPrice),
            ("entryprice", _)     => q.OrderByDescending(t => t.EntryPrice),
            ("size", "asc")       => q.OrderBy(t => t.PositionSize),
            ("size", _)           => q.OrderByDescending(t => t.PositionSize),
            ("fees", "asc")       => q.OrderBy(t => t.Fees),
            ("fees", _)           => q.OrderByDescending(t => t.Fees),
            (_, "asc")            => q.OrderBy(t => t.CreatedAt),
            _                     => q.OrderByDescending(t => t.CreatedAt),
        };

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<Trade?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Trades.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<int> CountByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        context.Trades.CountAsync(t => t.UserId == userId, cancellationToken);

    public void Add(Trade trade) => context.Trades.Add(trade);
    public void Remove(Trade trade) => context.Trades.Remove(trade);
}
