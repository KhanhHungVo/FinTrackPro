namespace FinTrackPro.Application.Trading.Queries.GetWatchlistAnalysis;

public record WatchlistAnalysisItemDto(
    string Symbol,
    decimal? Price,
    decimal? Change24h,
    double? Rsi1h,
    double? Rsi4h,
    double? RsiDaily,
    double? RsiWeekly);
