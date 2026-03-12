using MediatR;

namespace FinTrackPro.Application.Trading.Queries.GetWatchedSymbols;

public record GetWatchedSymbolsQuery : IRequest<IEnumerable<WatchedSymbolDto>>;
