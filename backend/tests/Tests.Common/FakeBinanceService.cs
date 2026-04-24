using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Models;

namespace Tests.Common;

/// <summary>
/// Stub that accepts all symbols as valid. Avoids real Binance HTTP calls in tests.
/// </summary>
public class FakeBinanceService : IBinanceService
{
    /// <summary>
    /// Stub: always returns <c>true</c>. Used in integration tests to bypass Binance HTTP calls.
    /// Symbol validation is now handled by FluentValidation format rules in
    /// <c>CreateTradeCommandValidator</c>, not by this service.
    /// </summary>
    public Task<bool> IsValidSymbolAsync(string symbol, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<IEnumerable<KlineDto>> GetKlinesAsync(string symbol, string interval, int limit, CancellationToken cancellationToken = default)
        => Task.FromResult(Enumerable.Empty<KlineDto>());

    public Task<TickerDto?> Get24HrTickerAsync(string symbol, CancellationToken cancellationToken = default)
        => Task.FromResult<TickerDto?>(null);

    public Task<HashSet<string>> GetValidSymbolsAsync(CancellationToken cancellationToken)
        => Task.FromResult(new HashSet<string>());

}
