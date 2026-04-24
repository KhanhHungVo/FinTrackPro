namespace FinTrackPro.Application.Common.Models;

public record TickerDto(
    string Symbol,
    decimal? LastPrice,
    decimal? PriceChangePercent,
    decimal Volume,
    decimal QuoteVolume
);
