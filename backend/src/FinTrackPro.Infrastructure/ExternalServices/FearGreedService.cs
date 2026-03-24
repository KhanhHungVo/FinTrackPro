using System.Net.Http.Json;
using System.Text.Json;
using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FinTrackPro.Infrastructure.ExternalServices;

public class FearGreedService(
    HttpClient httpClient,
    IMemoryCache cache,
    ILogger<FearGreedService> logger) : IFearGreedService
{
    private const string CacheKey = "fear_greed_index";

    public async Task<FearGreedDto?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out FearGreedDto? cached))
            return cached;

        try
        {
            var raw = await httpClient.GetFromJsonAsync<JsonElement>(
                "/fng/?limit=1", cancellationToken);

            var data = raw.GetProperty("data")[0];
            var value = int.Parse(data.GetProperty("value").GetString()!);
            var label = data.GetProperty("value_classification").GetString()!;
            var ts = DateTimeOffset.FromUnixTimeSeconds(
                long.Parse(data.GetProperty("timestamp").GetString()!)).UtcDateTime;

            var dto = new FearGreedDto(value, label, ts);
            cache.Set(CacheKey, dto, TimeSpan.FromHours(1));
            return dto;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch Fear & Greed index");
            return null;
        }
    }
}
