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
        logger.LogInformation("Computed RSI for {Symbol}: latest RSI = {Rsi}",
            watched.Symbol, rsiResults.LastOrDefault()?.Rsi);
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

        // Volume spike detection algorithm: Rolling-Average Ratio (RAR)
        //   baseline = simple mean of the last 7 complete daily candles (klines[0..6])
        //   signal   = live 24h ticker volume (Get24HrTickerAsync) — today's open candle
        //   trigger  = signal / baseline >= 2.0×
        //
        // We fetch 8 candles and take(7) to exclude today's still-open kline from the
        // baseline — today's live volume comes from the ticker, not the incomplete candle.
        // Threshold of 2× is a simple heuristic; literature uses 1.5×–3× depending on
        // asset volatility. See: Blume, Easley & O'Hara (1994) "Market Statistics and
        // Technical Analysis" (JF) for volume-price relationship theory; Granville's OBV
        // for volume trend context; and Buff Dormeier's "Investing with Volume Analysis"
        // for modern spike detection approaches.
        var klines = (await binanceService.GetKlinesAsync(
            watched.Symbol, "1d", 8, cancellationToken)).ToList();

        if (klines.Count < 8) return;

        var avgVolume = klines.Take(7).Average(k => (double)k.Volume);
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
