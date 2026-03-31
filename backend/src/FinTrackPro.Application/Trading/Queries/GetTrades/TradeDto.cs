using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;

namespace FinTrackPro.Application.Trading.Queries.GetTrades;

public record TradeDto(
    Guid Id,
    string Symbol,
    TradeDirection Direction,
    TradeStatus Status,
    decimal EntryPrice,
    decimal? ExitPrice,
    decimal? CurrentPrice,
    decimal PositionSize,
    decimal Fees,
    string Currency,
    decimal RateToUsd,
    decimal Result,
    decimal? UnrealizedResult,
    string? Notes,
    DateTime CreatedAt)
{
    public static explicit operator TradeDto(Trade t) => new(
        t.Id, t.Symbol, t.Direction, t.Status,
        t.EntryPrice, t.ExitPrice, t.CurrentPrice,
        t.PositionSize, t.Fees,
        t.Currency, t.RateToUsd,
        t.Result, t.UnrealizedResult,
        t.Notes, t.CreatedAt);
}
