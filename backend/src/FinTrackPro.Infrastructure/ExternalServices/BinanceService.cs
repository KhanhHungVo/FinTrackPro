using System.Globalization;
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

    /// <inheritdoc/>
    public async Task<bool> IsValidSymbolAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var symbols = await GetValidSymbolsAsync(cancellationToken);
        return symbols.Contains(symbol.ToUpperInvariant());
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<KlineDto>> GetKlinesAsync(
        string symbol, string interval, int limit, CancellationToken cancellationToken = default)
    {
        var url = $"/api/v3/klines?symbol={symbol}&interval={interval}&limit={limit}";
        var raw = await httpClient.GetFromJsonAsync<JsonElement[][]>(url, cancellationToken);
        if (raw is null) return [];

        var result = new List<KlineDto>(raw.Length);
        foreach (var k in raw)
        {
            if (!decimal.TryParse(k[1].GetString(), CultureInfo.InvariantCulture, out var o) ||
                !decimal.TryParse(k[2].GetString(), CultureInfo.InvariantCulture, out var h) ||
                !decimal.TryParse(k[3].GetString(), CultureInfo.InvariantCulture, out var l) ||
                !decimal.TryParse(k[4].GetString(), CultureInfo.InvariantCulture, out var c) ||
                !decimal.TryParse(k[5].GetString(), CultureInfo.InvariantCulture, out var v))
            {
                logger.LogWarning("Malformed kline data from Binance for {Symbol}, skipping record", symbol);
                continue;
            }
            result.Add(new KlineDto(
                OpenTime: DateTimeOffset.FromUnixTimeMilliseconds(k[0].GetInt64()).UtcDateTime,
                Open: o, High: h, Low: l, Close: c, Volume: v));
        }
        return result;
    }

    /// <inheritdoc/>
    public async Task<TickerDto?> Get24HrTickerAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var url = $"/api/v3/ticker/24hr?symbol={symbol}";
        try
        {
            var raw = await httpClient.GetFromJsonAsync<JsonElement>(url, cancellationToken);
            if (raw.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                logger.LogWarning("Unexpected empty response from Binance 24hr ticker for {Symbol}", symbol);
                return null;
            }

            if (!decimal.TryParse(raw.GetProperty("volume").GetString(), CultureInfo.InvariantCulture, out var vol) ||
                !decimal.TryParse(raw.GetProperty("quoteVolume").GetString(), CultureInfo.InvariantCulture, out var qvol))
            {
                logger.LogWarning("Malformed numeric fields in Binance 24hr ticker for {Symbol}", symbol);
                return null;
            }

            return new TickerDto(
                Symbol: raw.GetProperty("symbol").GetString()!,
                Volume: vol,
                QuoteVolume: qvol);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to fetch 24hr ticker for {Symbol}", symbol);
            return null;
        }
    }

    /// <summary>
    /// Fetches and caches the full Binance symbol list for 24 hours.
    /// Cache key: <c>binance_exchange_info</c>. Falls back to an empty set on parse failure.
    /// Used internally by <see cref="IsValidSymbolAsync"/> — reserved for future features
    /// that need to filter or validate against Binance-listed trading pairs.
    /// </summary>
    private async Task<HashSet<string>> GetValidSymbolsAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(ExchangeInfoCacheKey, out HashSet<string>? cached) && cached is not null)
            return cached;

        var raw = await httpClient.GetFromJsonAsync<JsonElement>("/api/v3/exchangeInfo", cancellationToken);
        if (raw.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            logger.LogWarning("Unexpected empty response from Binance exchangeInfo");
            return [];
        }

        var symbols = raw.GetProperty("symbols")
            .EnumerateArray()
            .Select(s => s.GetProperty("symbol").GetString()!)
            .ToHashSet();

        cache.Set(ExchangeInfoCacheKey, symbols, TimeSpan.FromHours(24));
        return symbols;
    }
}
