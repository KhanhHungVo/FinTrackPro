using MediatR;

namespace FinTrackPro.Application.Trading.Queries.GetWatchlistAnalysis;

public record GetWatchlistAnalysisQuery : IRequest<IEnumerable<WatchlistAnalysisItemDto>>;
