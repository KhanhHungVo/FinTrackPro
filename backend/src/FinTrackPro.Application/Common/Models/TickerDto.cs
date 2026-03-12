namespace FinTrackPro.Application.Common.Models;

public record TickerDto(
    string Symbol,
    decimal Volume,
    decimal QuoteVolume
);
