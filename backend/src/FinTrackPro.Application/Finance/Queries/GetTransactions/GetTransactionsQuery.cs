using FinTrackPro.Application.Common.Models;
using MediatR;

namespace FinTrackPro.Application.Finance.Queries.GetTransactions;

public record GetTransactionsQuery : IRequest<PagedResult<TransactionDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? Month { get; init; }
    public string? Type { get; init; }
    public Guid? CategoryId { get; init; }
    public string SortBy { get; init; } = "date";
    public string SortDir { get; init; } = "desc";
}
