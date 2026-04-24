using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Models;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinTrackPro.Infrastructure.ExternalServices;

public class CoinGeckoService(
    HttpClient httpClient,
    HybridCache cache,
    IOptions<CoinGeckoOptions> options,
    ILogger<CoinGeckoService> logger) : ICoinGeckoService
{
    public async Task<IEnumerable<TrendingCoinDto>> GetTrendingCoinsAsync(CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync(
            "market:trending",
            async ct =>
            {
                try
                {
                    // Step 1: fetch trending IDs + rank
                    var trendingRaw = await httpClient.GetFromJsonAsync<JsonElement>(
                        "/api/v3/search/trending", ct);

                    if (trendingRaw.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
                    {
                        logger.LogWarning("Unexpected empty response from CoinGecko trending");
                        return [];
                    }

                    var trendingItems = trendingRaw.GetProperty("coins")
                        .EnumerateArray()
                        .Take(10)
                        .Select(c => c.GetProperty("item"))
                        .Select(item => new
                        {
                            Id = item.GetProperty("id").GetString()!,
                            Name = item.GetProperty("name").GetString()!,
                            Symbol = item.GetProperty("symbol").GetString()!,
                            MarketCapRank = item.TryGetProperty("market_cap_rank", out var rank)
                                ? rank.ValueKind == JsonValueKind.Number ? rank.GetInt32() : 0
                                : 0
                        })
                        .ToList();

                    if (trendingItems.Count == 0)
                        return [];

                    // Step 2: batch-enrich with price + % change
                    var ids = string.Join(",", trendingItems.Select(x => x.Id));
                    var marketsUrl = $"/api/v3/coins/markets?ids={ids}&vs_currency=usd&price_change_percentage=1h,24h,7d";
                    var marketsRaw = await httpClient.GetFromJsonAsync<JsonElement[]>(marketsUrl, ct);

                    var priceMap = new Dictionary<string, (decimal? Price, decimal? C1h, decimal? C24h, decimal? C7d)>();
                    if (marketsRaw is not null)
                    {
                        foreach (var m in marketsRaw)
                        {
                            var id = m.GetProperty("id").GetString()!;
                            priceMap[id] = (
                                ParseNullableDecimal(m, "current_price"),
                                ParseNullableDecimal(m, "price_change_percentage_1h_in_currency"),
                                ParseNullableDecimal(m, "price_change_percentage_24h_in_currency"),
                                ParseNullableDecimal(m, "price_change_percentage_7d_in_currency")
                            );
                        }
                    }

                    return trendingItems.Select((item, idx) =>
                    {
                        var prices = priceMap.GetValueOrDefault(item.Id);
                        return new TrendingCoinDto(
                            Id: item.Id,
                            Name: item.Name,
                            Symbol: item.Symbol,
                            MarketCapRank: item.MarketCapRank,
                            Price: prices.Price,
                            Change1h: prices.C1h,
                            Change24h: prices.C24h,
                            Change7d: prices.C7d);
                    }).ToList();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to fetch CoinGecko trending coins");
                    return [];
                }
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromSeconds(options.Value.TrendingCacheTtlSeconds) },
            tags: ["market"],
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<MarketCapCoinDto>> GetMarketCapCoinsAsync(CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync(
            "market:marketcap",
            async ct =>
            {
                try
                {
                    var url = "/api/v3/coins/markets?vs_currency=usd&order=market_cap_desc&per_page=10&price_change_percentage=1h,24h,7d";
                    var raw = await httpClient.GetFromJsonAsync<JsonElement[]>(url, ct);

                    if (raw is null || raw.Length == 0)
                        return [];

                    return raw.Select((m, idx) => new MarketCapCoinDto(
                        Rank: idx + 1,
                        Id: m.GetProperty("id").GetString()!,
                        Name: m.GetProperty("name").GetString()!,
                        Symbol: m.GetProperty("symbol").GetString()!,
                        Price: ParseNullableDecimal(m, "current_price"),
                        MarketCap: ParseNullableDecimal(m, "market_cap"),
                        Change1h: ParseNullableDecimal(m, "price_change_percentage_1h_in_currency"),
                        Change24h: ParseNullableDecimal(m, "price_change_percentage_24h_in_currency"),
                        Change7d: ParseNullableDecimal(m, "price_change_percentage_7d_in_currency")
                    )).ToList();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to fetch CoinGecko market cap coins");
                    return [];
                }
            },
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromSeconds(options.Value.MarketCapCacheTtlSeconds) },
            tags: ["market"],
            cancellationToken: cancellationToken);
    }

    private static decimal? ParseNullableDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop))
            return null;
        if (prop.ValueKind == JsonValueKind.Null)
            return null;
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var d))
            return d;
        if (prop.ValueKind == JsonValueKind.String &&
            decimal.TryParse(prop.GetString(), CultureInfo.InvariantCulture, out var ds))
            return ds;
        return null;
    }
}
