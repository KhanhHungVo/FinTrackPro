using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;

namespace FinTrackPro.Application.Trading.Queries.GetTrades;

public record TradeDto(
    Guid Id,
    string Symbol,
    TradeDirection Direction,
    decimal EntryPrice,
    decimal ExitPrice,
    decimal PositionSize,
    decimal Fees,
    string Currency,
    decimal RateToUsd,
    decimal Result,
    string? Notes,
    DateTime CreatedAt)
{
    public static explicit operator TradeDto(Trade t) => new(
        t.Id, t.Symbol, t.Direction,
        t.EntryPrice, t.ExitPrice,
        t.PositionSize, t.Fees,
        t.Currency, t.RateToUsd, t.Result,
        t.Notes, t.CreatedAt);
}
