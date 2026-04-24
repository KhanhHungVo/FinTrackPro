using FinTrackPro.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinTrackPro.Infrastructure.ExternalServices.ExchangeRate;

public sealed class ExchangeRateService(
    IExchangeRateClient client,
    HybridCache cache,
    IOptions<ExchangeRateOptions> options,
    ILogger<ExchangeRateService> logger)
    : IExchangeRateService
{
    private const string CacheKey = "rates_usd";

    public async Task<Dictionary<string, decimal>> GetRateToUsdAsync(CancellationToken ct = default)
    {
        return await cache.GetOrCreateAsync(
            CacheKey,
            async innerCt =>
            {
                var rates = await client.GetLatestRatesAsync(innerCt);

                if (rates.Count > 0)
                {
                    rates["USD"] = 1m;
                    return rates;
                }

                // Tier 3 — config fallback (cached for full TTL so next request also uses it)
                logger.LogWarning(
                    "Exchange rate HTTP fetch failed; returning config fallback rates. " +
                    "Amounts may be approximate until the live rate is restored.");
                return new Dictionary<string, decimal>(options.Value.FallbackRates);
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromHours(8) },
            cancellationToken: ct);
    }
}
