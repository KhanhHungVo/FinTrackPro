using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Models;
using MediatR;

namespace FinTrackPro.Application.Market.Queries.GetMarketCapCoins;

public class GetMarketCapCoinsQueryHandler(ICoinGeckoService coinGeckoService)
    : IRequestHandler<GetMarketCapCoinsQuery, IEnumerable<MarketCapCoinDto>>
{
    public Task<IEnumerable<MarketCapCoinDto>> Handle(
        GetMarketCapCoinsQuery request, CancellationToken cancellationToken)
        => coinGeckoService.GetMarketCapCoinsAsync(cancellationToken);
}
