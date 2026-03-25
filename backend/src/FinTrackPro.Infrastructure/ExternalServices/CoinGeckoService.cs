using System.Net.Http.Json;
using System.Text.Json;
using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FinTrackPro.Infrastructure.ExternalServices;

public class CoinGeckoService(
    HttpClient httpClient,
    IMemoryCache cache,
    ILogger<CoinGeckoService> logger) : ICoinGeckoService
{
    private const string CacheKey = "coingecko_trending";

    public async Task<IEnumerable<TrendingCoinDto>> GetTrendingCoinsAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out IEnumerable<TrendingCoinDto>? cached) && cached is not null)
            return cached;

        try
        {
            var raw = await httpClient.GetFromJsonAsync<JsonElement>(
                "/api/v3/search/trending", cancellationToken);

            if (raw.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                logger.LogWarning("Unexpected empty response from CoinGecko trending");
                return [];
            }

            var coins = raw.GetProperty("coins")
                .EnumerateArray()
                .Take(7)
                .Select(c =>
                {
                    var item = c.GetProperty("item");
                    return new TrendingCoinDto(
                        Id: item.GetProperty("id").GetString()!,
                        Name: item.GetProperty("name").GetString()!,
                        Symbol: item.GetProperty("symbol").GetString()!,
                        MarketCapRank: item.TryGetProperty("market_cap_rank", out var rank)
                            ? rank.ValueKind == JsonValueKind.Number ? rank.GetInt32() : 0
                            : 0
                    );
                })
                .ToList();

            cache.Set(CacheKey, coins, TimeSpan.FromMinutes(15));
            return coins;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch CoinGecko trending coins");
            return [];
        }
    }
}
