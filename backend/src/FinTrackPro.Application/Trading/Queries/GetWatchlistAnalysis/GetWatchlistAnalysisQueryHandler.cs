using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Models;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Skender.Stock.Indicators;

namespace FinTrackPro.Application.Trading.Queries.GetWatchlistAnalysis;

public class GetWatchlistAnalysisQueryHandler(
    IWatchedSymbolRepository watchedSymbolRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser,
    IBinanceService binanceService,
    HybridCache cache)
    : IRequestHandler<GetWatchlistAnalysisQuery, IEnumerable<WatchlistAnalysisItemDto>>
{
    public async Task<IEnumerable<WatchlistAnalysisItemDto>> Handle(
        GetWatchlistAnalysisQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        var watchedSymbols = await watchedSymbolRepository.GetByUserAsync(user.Id, cancellationToken);
        var symbols = watchedSymbols.Select(s => s.Symbol).ToList();

        if (symbols.Count == 0)
            return [];

        var tasks = symbols.Select(symbol =>
            cache.GetOrCreateAsync(
                $"watchlist:analysis:{symbol}",
                async ct =>
                {
                    var tickerTask = binanceService.Get24HrTickerAsync(symbol, ct);
                    var dailyTask = binanceService.GetKlinesAsync(symbol, "1d", 100, ct);
                    var weeklyTask = binanceService.GetKlinesAsync(symbol, "1w", 100, ct);
                    await Task.WhenAll(tickerTask, dailyTask, weeklyTask);
                    var ticker = tickerTask.Result;
                    var dailyKlines = dailyTask.Result;
                    var weeklyKlines = weeklyTask.Result;

                    return new WatchlistAnalysisItemDto(
                        Symbol: symbol,
                        Price: ticker?.LastPrice,
                        Change24h: ticker?.PriceChangePercent,
                        RsiDaily: ComputeRsi(dailyKlines),
                        RsiWeekly: ComputeRsi(weeklyKlines));
                },
                new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(5) },
                cancellationToken: cancellationToken)
            .AsTask());

        var results = await Task.WhenAll(tasks);
        return results.OrderBy(r => r.Symbol);
    }

    private static double? ComputeRsi(IEnumerable<KlineDto> klines)
    {
        var list = klines.ToList();
        if (list.Count < 14) return null;
        var quotes = list.Select(k => new Quote { Date = k.OpenTime, Close = k.Close });
        return quotes.GetRsi(14).LastOrDefault()?.Rsi;
    }
}
