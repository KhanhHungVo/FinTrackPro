using FinTrackPro.Application.Common.Models;

namespace FinTrackPro.Application.Common.Interfaces;

public interface IBinanceService
{
    Task<bool> IsValidSymbolAsync(string symbol, CancellationToken cancellationToken = default);
    Task<IEnumerable<KlineDto>> GetKlinesAsync(string symbol, string interval, int limit, CancellationToken cancellationToken = default);
    Task<TickerDto?> Get24HrTickerAsync(string symbol, CancellationToken cancellationToken = default);
}
