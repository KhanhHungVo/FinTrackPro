using FinTrackPro.Application.Common.Models;

namespace FinTrackPro.Application.Common.Interfaces;

public interface IBinanceService
{
    /// <summary>
    /// Validates whether <paramref name="symbol"/> exists in Binance's exchange info.
    /// NOTE: Not used in the trade-creation flow (removed to avoid geo-blocking on Render).
    /// Reserved for future use: e.g. market-data widgets that filter to Binance-listed pairs only.
    /// </summary>
    Task<bool> IsValidSymbolAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns OHLCV candlestick data for <paramref name="symbol"/>.
    /// Used by: <c>MarketSignalJob</c>, <c>/market/signals</c> endpoint.
    /// </summary>
    Task<IEnumerable<KlineDto>> GetKlinesAsync(string symbol, string interval, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns 24-hour ticker stats for <paramref name="symbol"/>. Returns <c>null</c> if the symbol
    /// is unavailable or Binance cannot be reached.
    /// Used by: market data endpoints.
    /// </summary>
    Task<TickerDto?> Get24HrTickerAsync(string symbol, CancellationToken cancellationToken = default);
}
