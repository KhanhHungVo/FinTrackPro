using FinTrackPro.Application.Trading.Queries.GetTrades;
using FinTrackPro.Domain.Enums;
using MediatR;

namespace FinTrackPro.Application.Trading.Commands.UpdateTrade;

public record UpdateTradeCommand(
    Guid Id,
    string Symbol,
    TradeDirection Direction,
    decimal EntryPrice,
    decimal ExitPrice,
    decimal PositionSize,
    decimal Fees,
    string Currency,
    string? Notes
) : IRequest<TradeDto>;
