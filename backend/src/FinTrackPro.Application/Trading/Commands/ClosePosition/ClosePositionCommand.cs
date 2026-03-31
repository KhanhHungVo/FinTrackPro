using FinTrackPro.Application.Trading.Queries.GetTrades;
using MediatR;

namespace FinTrackPro.Application.Trading.Commands.ClosePosition;

public record ClosePositionCommand(
    Guid Id,
    decimal ExitPrice,
    decimal Fees
) : IRequest<TradeDto>;
