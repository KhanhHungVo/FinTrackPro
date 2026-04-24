using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Models;
using MediatR;

namespace FinTrackPro.Application.Market.Queries.GetTrendingCoins;

public class GetTrendingCoinsQueryHandler(ICoinGeckoService coinGeckoService)
    : IRequestHandler<GetTrendingCoinsQuery, IEnumerable<TrendingCoinDto>>
{
    public Task<IEnumerable<TrendingCoinDto>> Handle(
        GetTrendingCoinsQuery request, CancellationToken cancellationToken)
        => coinGeckoService.GetTrendingCoinsAsync(cancellationToken);
}
