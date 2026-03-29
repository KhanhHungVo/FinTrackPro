using FinTrackPro.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

public class ExchangeRateService(
    IExchangeRateClient client,
    IMemoryCache cache,
    ILogger<ExchangeRateService> logger)
    : IExchangeRateService
{
    private const string CacheKey = "rates_usd";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(8);

    public async Task<Dictionary<string, decimal>> GetRateToUsdAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out Dictionary<string, decimal> cached))
            return cached;

        var rates = await client.GetLatestRatesAsync(ct);

        if (rates.Count == 0)
        {
            logger.LogWarning("Failed to fetch exchange rates");
            return [];
        }

        cache.Set(CacheKey, rates, CacheTtl);

        return rates;
    }
}