using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Skender.Stock.Indicators;

namespace FinTrackPro.BackgroundJobs.Jobs;

/// <summary>
/// Runs every 4 hours. Fetches klines for all watched symbols,
/// computes RSI and volume spike signals, stores them, and sends Telegram notifications.
/// </summary>
public class MarketSignalJob(
    IWatchedSymbolRepository watchedSymbols,
    ISignalRepository signalRepository,
    IApplicationDbContext context,
    IBinanceService binanceService,
    INotificationService notificationService,
    ILogger<MarketSignalJob> logger)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var allWatched = await watchedSymbols.GetAllAsync(cancellationToken);

        foreach (var watched in allWatched)
        {
            try
            {
                await ProcessSymbolAsync(watched, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing signal for {Symbol} / user {UserId}",
                    watched.Symbol, watched.UserId);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessSymbolAsync(WatchedSymbol watched, CancellationToken cancellationToken)
    {
        var klines = (await binanceService.GetKlinesAsync(
            watched.Symbol, "1w", 14, cancellationToken)).ToList();

        if (klines.Count < 14) return;

        // RSI computation via Skender
        var quotes = klines.Select(k => new Quote
        {
            Date = k.OpenTime,
            Close = k.Close
        });

        var rsiResults = quotes.GetRsi(14).ToList();
        var latestRsi = rsiResults.LastOrDefault()?.Rsi;

        if (latestRsi.HasValue)
        {
            await TryCreateSignalAsync(
                watched, latestRsi.Value,
                (decimal)latestRsi.Value, "1W", cancellationToken);
        }

        // Volume spike detection
        await CheckVolumeSpikeAsync(watched, cancellationToken);
    }

    private async Task TryCreateSignalAsync(
        WatchedSymbol watched, double rsi,
        decimal value, string timeframe, CancellationToken cancellationToken)
    {
        SignalType? signalType = rsi < 30 ? SignalType.RsiOversold
                               : rsi > 70 ? SignalType.RsiOverbought
                               : null;

        if (signalType is null) return;

        var alreadyNotified = await signalRepository.ExistsRecentAsync(
            watched.UserId, watched.Symbol, signalType.Value,
            TimeSpan.FromHours(24), cancellationToken);

        if (alreadyNotified) return;

        var message = signalType == SignalType.RsiOversold
            ? $"RSI {rsi:F1} — {watched.Symbol} is oversold. Consider DCA."
            : $"RSI {rsi:F1} — {watched.Symbol} is overbought. Consider taking profit.";

        var signal = Signal.Create(watched.UserId, watched.Symbol, signalType.Value, message, value, timeframe);
        signal.MarkNotified();
        signalRepository.Add(signal);

        await notificationService.NotifyAsync(
            watched.UserId, $"RSI Alert: {watched.Symbol}", message, cancellationToken);
    }

    private async Task CheckVolumeSpikeAsync(WatchedSymbol watched, CancellationToken cancellationToken)
    {
        var ticker = await binanceService.Get24HrTickerAsync(watched.Symbol, cancellationToken);
        if (ticker is null) return;

        // Volume spike: compare current volume to an estimated baseline from klines
        var klines = (await binanceService.GetKlinesAsync(
            watched.Symbol, "1d", 7, cancellationToken)).ToList();

        if (klines.Count < 7) return;

        var avgVolume = klines.Take(6).Average(k => (double)k.Volume);
        if (avgVolume == 0) return;

        var spikeRatio = (double)ticker.Volume / avgVolume;
        if (spikeRatio < 2.0) return;

        var alreadyNotified = await signalRepository.ExistsRecentAsync(
            watched.UserId, watched.Symbol, SignalType.VolumeSpike,
            TimeSpan.FromHours(24), cancellationToken);

        if (alreadyNotified) return;

        var message = $"Volume spike detected on {watched.Symbol}: {spikeRatio:F1}x above 7-day average.";
        var signal = Signal.Create(watched.UserId, watched.Symbol, SignalType.VolumeSpike,
            message, (decimal)spikeRatio, "1D");
        signal.MarkNotified();
        signalRepository.Add(signal);

        await notificationService.NotifyAsync(
            watched.UserId, $"Volume Spike: {watched.Symbol}", message, cancellationToken);
    }
}
