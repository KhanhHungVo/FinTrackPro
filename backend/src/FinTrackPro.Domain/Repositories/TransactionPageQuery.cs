namespace FinTrackPro.Domain.Repositories;

public record TransactionPageQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Month = null,
    string? Type = null,
    Guid? CategoryId = null,
    string SortBy = "date",
    string SortDir = "desc");
