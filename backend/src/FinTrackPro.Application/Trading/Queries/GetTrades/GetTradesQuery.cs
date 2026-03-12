using MediatR;

namespace FinTrackPro.Application.Trading.Queries.GetTrades;

public record GetTradesQuery : IRequest<IEnumerable<TradeDto>>;
