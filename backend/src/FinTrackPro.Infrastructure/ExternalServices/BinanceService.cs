using System.Net.Http.Json;
using System.Text.Json;
using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FinTrackPro.Infrastructure.ExternalServices;

public class BinanceService(
    HttpClient httpClient,
    IMemoryCache cache,
    ILogger<BinanceService> logger) : IBinanceService
{
    private const string ExchangeInfoCacheKey = "binance_exchange_info";

    public async Task<bool> IsValidSymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var symbols = await GetValidSymbolsAsync(cancellationToken);
        return symbols.Contains(symbol.ToUpperInvariant());
    }

    public async Task<IEnumerable<KlineDto>> GetKlinesAsync(
        string symbol, string interval, int limit, CancellationToken cancellationToken = default)
    {
        var url = $"/api/v3/klines?symbol={symbol}&interval={interval}&limit={limit}";
        var raw = await httpClient.GetFromJsonAsync<JsonElement[][]>(url, cancellationToken);
        if (raw is null) return [];

        return raw.Select(k => new KlineDto(
            OpenTime: DateTimeOffset.FromUnixTimeMilliseconds(k[0].GetInt64()).UtcDateTime,
            Open: decimal.Parse(k[1].GetString()!),
            High: decimal.Parse(k[2].GetString()!),
            Low: decimal.Parse(k[3].GetString()!),
            Close: decimal.Parse(k[4].GetString()!),
            Volume: decimal.Parse(k[5].GetString()!)
        ));
    }

    public async Task<TickerDto?> Get24HrTickerAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var url = $"/api/v3/ticker/24hr?symbol={symbol}";
        try
        {
            var raw = await httpClient.GetFromJsonAsync<JsonElement>(url, cancellationToken);
            return new TickerDto(
                Symbol: raw.GetProperty("symbol").GetString()!,
                Volume: decimal.Parse(raw.GetProperty("volume").GetString()!),
                QuoteVolume: decimal.Parse(raw.GetProperty("quoteVolume").GetString()!)
            );
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch 24hr ticker for {Symbol}", symbol);
            return null;
        }
    }

    private async Task<HashSet<string>> GetValidSymbolsAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(ExchangeInfoCacheKey, out HashSet<string>? cached) && cached is not null)
            return cached;

        var raw = await httpClient.GetFromJsonAsync<JsonElement>("/api/v3/exchangeInfo", cancellationToken);
        var symbols = raw.GetProperty("symbols")
            .EnumerateArray()
            .Select(s => s.GetProperty("symbol").GetString()!)
            .ToHashSet();

        cache.Set(ExchangeInfoCacheKey, symbols, TimeSpan.FromHours(24));
        return symbols;
    }
}
