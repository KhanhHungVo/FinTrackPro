namespace FinTrackPro.Application.Trading.DTOs;

public record TradeSummaryDto(
    decimal TotalPnl,
    int WinRate,
    int TotalTrades,
    decimal UnrealizedPnl);
