using FinTrackPro.Domain.Enums;
using MediatR;

namespace FinTrackPro.Application.Trading.Commands.CreateTrade;

public record CreateTradeCommand(
    string Symbol,
    TradeDirection Direction,
    decimal EntryPrice,
    decimal ExitPrice,
    decimal PositionSize,
    decimal Fees,
    string Currency,
    string? Notes
) : IRequest<Guid>;
