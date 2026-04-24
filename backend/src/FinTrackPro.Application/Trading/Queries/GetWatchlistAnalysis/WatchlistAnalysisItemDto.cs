namespace FinTrackPro.Application.Trading.Queries.GetWatchlistAnalysis;

public record WatchlistAnalysisItemDto(
    string Symbol,
    decimal? Price,
    decimal? Change24h,
    double? RsiDaily,
    double? RsiWeekly);
