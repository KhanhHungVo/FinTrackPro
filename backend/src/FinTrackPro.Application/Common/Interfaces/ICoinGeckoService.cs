using FinTrackPro.Application.Common.Models;

namespace FinTrackPro.Application.Common.Interfaces;

public interface ICoinGeckoService
{
    Task<IEnumerable<TrendingCoinDto>> GetTrendingCoinsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<MarketCapCoinDto>> GetMarketCapCoinsAsync(CancellationToken cancellationToken = default);
}
