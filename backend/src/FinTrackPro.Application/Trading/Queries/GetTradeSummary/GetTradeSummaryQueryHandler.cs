using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Trading.DTOs;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinTrackPro.Application.Trading.Queries.GetTradeSummary;

public class GetTradeSummaryQueryHandler(
    IUserRepository userRepository,
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<GetTradeSummaryQuery, TradeSummaryDto>
{
    public async Task<TradeSummaryDto> Handle(
        GetTradeSummaryQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        var q = context.Trades.Where(t => t.UserId == user.Id);

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<TradeStatus>(request.Status, ignoreCase: true, out var parsedStatus))
            q = q.Where(t => t.Status == parsedStatus);

        if (!string.IsNullOrWhiteSpace(request.Direction) &&
            Enum.TryParse<TradeDirection>(request.Direction, ignoreCase: true, out var parsedDir))
            q = q.Where(t => t.Direction == parsedDir);

        if (request.DateFrom.HasValue)
        {
            var from = request.DateFrom.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            q = q.Where(t => t.CreatedAt >= from);
        }

        if (request.DateTo.HasValue)
        {
            var to = request.DateTo.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
            q = q.Where(t => t.CreatedAt <= to);
        }

        var preferred = string.IsNullOrWhiteSpace(request.PreferredCurrency) ? "USD" : request.PreferredCurrency;
        var preferredRate = request.PreferredRate == 0 ? 1m : request.PreferredRate;

        var totalTrades = await q.CountAsync(cancellationToken);

        // Realized P&L: normalize to preferredCurrency (short-circuit when currency matches)
        var totalPnl = await q
            .Where(t => t.Status == TradeStatus.Closed && t.ExitPrice != null)
            .SumAsync(t =>
                (t.Direction == TradeDirection.Long
                    ? (t.ExitPrice!.Value - t.EntryPrice) * t.PositionSize - t.Fees
                    : (t.EntryPrice - t.ExitPrice!.Value) * t.PositionSize - t.Fees)
                * (t.Currency == preferred ? 1m : preferredRate / t.RateToUsd),
                cancellationToken);

        var closedCount = await q.CountAsync(t => t.Status == TradeStatus.Closed, cancellationToken);
        var winCount = await q
            .Where(t => t.Status == TradeStatus.Closed && t.ExitPrice != null)
            .CountAsync(t =>
                t.Direction == TradeDirection.Long
                    ? t.ExitPrice!.Value > t.EntryPrice
                    : t.ExitPrice!.Value < t.EntryPrice,
                cancellationToken);

        var winRate = closedCount > 0 ? (int)Math.Round((double)winCount / closedCount * 100) : 0;

        // Unrealized P&L: normalize to preferredCurrency (short-circuit when currency matches)
        var unrealizedPnl = await q
            .Where(t => t.Status == TradeStatus.Open && t.CurrentPrice != null)
            .SumAsync(t =>
                (t.Direction == TradeDirection.Long
                    ? (t.CurrentPrice!.Value - t.EntryPrice) * t.PositionSize
                    : (t.EntryPrice - t.CurrentPrice!.Value) * t.PositionSize)
                * (t.Currency == preferred ? 1m : preferredRate / t.RateToUsd),
                cancellationToken);

        return new TradeSummaryDto(totalPnl, winRate, totalTrades, unrealizedPnl);
    }
}
