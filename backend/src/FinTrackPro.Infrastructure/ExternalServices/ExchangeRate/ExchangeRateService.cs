using FinTrackPro.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinTrackPro.Infrastructure.ExternalServices.ExchangeRate;

public class ExchangeRateService(
    IExchangeRateClient client,
    IMemoryCache cache,
    IOptions<ExchangeRateOptions> options,
    ILogger<ExchangeRateService> logger)
    : IExchangeRateService
{
    private const string CacheKey = "rates_usd";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(8);

    public async Task<Dictionary<string, decimal>> GetRateToUsdAsync(CancellationToken ct = default)
    {
        // Tier 1 — memory cache
        if (cache.TryGetValue(CacheKey, out Dictionary<string, decimal>? cached) && cached is not null)
            return cached;

        // Tier 2 — HTTP client
        var rates = await client.GetLatestRatesAsync(ct);

        if (rates.Count > 0)
        {
            rates["USD"] = 1m; // USD is always 1; ensure it is present regardless of API response
            cache.Set(CacheKey, rates, CacheTtl);
            return rates;
        }

        // Tier 3 — config fallback (not cached — next request retries HTTP immediately)
        logger.LogWarning(
            "Exchange rate HTTP fetch failed; returning config fallback rates. " +
            "Amounts may be approximate until the live rate is restored.");
        return new Dictionary<string, decimal>(options.Value.FallbackRates);
    }
}
