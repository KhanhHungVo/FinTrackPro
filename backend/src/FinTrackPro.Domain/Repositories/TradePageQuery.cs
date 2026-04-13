namespace FinTrackPro.Domain.Repositories;

public record TradePageQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Status = null,
    string? Direction = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    string SortBy = "date",
    string SortDir = "desc");
