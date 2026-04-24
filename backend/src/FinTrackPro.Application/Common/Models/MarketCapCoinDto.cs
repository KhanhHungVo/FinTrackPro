namespace FinTrackPro.Application.Common.Models;

public record MarketCapCoinDto(
    int Rank,
    string Id,
    string Name,
    string Symbol,
    decimal? Price,
    decimal? MarketCap,
    decimal? Change1h,
    decimal? Change24h,
    decimal? Change7d);
