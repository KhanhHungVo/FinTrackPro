using System.Net.Http.Json;
using System.Text.Json;
using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FinTrackPro.Infrastructure.ExternalServices;

public class CoinGeckoService : ICoinGeckoService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CoinGeckoService> _logger;
    private const string CacheKey = "coingecko_trending";

    public CoinGeckoService(HttpClient httpClient, IMemoryCache cache, ILogger<CoinGeckoService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<TrendingCoinDto>> GetTrendingCoinsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out IEnumerable<TrendingCoinDto>? cached) && cached is not null)
            return cached;

        try
        {
            var raw = await _httpClient.GetFromJsonAsync<JsonElement>(
                "/api/v3/search/trending", cancellationToken);

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

            _cache.Set(CacheKey, coins, TimeSpan.FromMinutes(15));
            return coins;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch CoinGecko trending coins");
            return [];
        }
    }
}
