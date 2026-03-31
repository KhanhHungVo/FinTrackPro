using FinTrackPro.Domain.Enums;
using MediatR;

namespace FinTrackPro.Application.Trading.Commands.CreateTrade;

public record CreateTradeCommand(
    string Symbol,
    TradeDirection Direction,
    TradeStatus Status,
    decimal EntryPrice,
    decimal? ExitPrice,
    decimal? CurrentPrice,
    decimal PositionSize,
    decimal Fees,
    string Currency,
    string? Notes
) : IRequest<Guid>;
