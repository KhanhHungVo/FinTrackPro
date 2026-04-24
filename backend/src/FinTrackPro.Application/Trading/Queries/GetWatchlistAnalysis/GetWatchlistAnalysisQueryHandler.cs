using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Models;
using FinTrackPro.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Skender.Stock.Indicators;

namespace FinTrackPro.Application.Trading.Queries.GetWatchlistAnalysis;

public class GetWatchlistAnalysisQueryHandler(
    IWatchedSymbolRepository watchedSymbolRepository,
    ICurrentUser currentUser,
    IBinanceService binanceService,
    HybridCache cache,
    ILogger<GetWatchlistAnalysisQueryHandler> logger)
    : IRequestHandler<GetWatchlistAnalysisQuery, IEnumerable<WatchlistAnalysisItemDto>>
{
    private const int CacheTtlSeconds = 60;
    public async Task<IEnumerable<WatchlistAnalysisItemDto>> Handle(
        GetWatchlistAnalysisQuery request, CancellationToken cancellationToken)
    {
        var watchedSymbols = await watchedSymbolRepository.GetByUserAsync(currentUser.UserId, cancellationToken);
        var symbols = watchedSymbols.Select(s => s.Symbol).ToList();

        if (symbols.Count == 0)
            return [];

        var validSymbols = await binanceService.GetValidSymbolsAsync(cancellationToken);

        symbols = [.. symbols.Intersect(validSymbols)];

        using var semaphore = new SemaphoreSlim(5);
        var tasks = symbols.Select(async symbol =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try   { return await GetAnalysisSafe(symbol, cancellationToken); }
            finally { semaphore.Release(); }
        });

        var results = await Task.WhenAll(tasks);
        return results.OfType<WatchlistAnalysisItemDto>().OrderBy(r => r.Symbol);
    }

    private async Task<WatchlistAnalysisItemDto?> GetAnalysisSafe(
        string symbol,
        CancellationToken cancellationToken)
    {
        try
        {
            return await cache.GetOrCreateAsync(
                $"watchlist:analysis:{symbol}",
                async ct =>
                {
                    var tickerTask   = binanceService.Get24HrTickerAsync(symbol, ct);
                    var dailyTask    = binanceService.GetKlinesAsync(symbol, "1d", 100, ct);
                    var weeklyTask   = binanceService.GetKlinesAsync(symbol, "1w", 100, ct);

                    await Task.WhenAll(tickerTask, dailyTask, weeklyTask);

                    var ticker       = await tickerTask;
                    var dailyKlines  = await dailyTask;
                    var weeklyKlines = await weeklyTask;

                    return new WatchlistAnalysisItemDto(
                        Symbol: symbol,
                        Price: ticker?.LastPrice,
                        Change24h: ticker?.PriceChangePercent,
                        RsiDaily: ComputeRsi(dailyKlines),
                        RsiWeekly: ComputeRsi(weeklyKlines));
                },
                new HybridCacheEntryOptions { Expiration = TimeSpan.FromSeconds(CacheTtlSeconds) },
                cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to fetch analysis for symbol {Symbol}", symbol);
            return null;
        }
    }

    private static double? ComputeRsi(IEnumerable<KlineDto> klines)
    {
        var list = klines.ToList();
        if (list.Count < 14) return null;
        var quotes = list.Select(k => new Quote { Date = k.OpenTime, Close = k.Close });
        return quotes.GetRsi(14).LastOrDefault()?.Rsi;
    }
}
