using FinTrackPro.Application.Common.Models;

namespace FinTrackPro.Application.Common.Interfaces;

public interface ICoinGeckoService
{
    Task<IEnumerable<TrendingCoinDto>> GetTrendingCoinsAsync(CancellationToken cancellationToken = default);
}
