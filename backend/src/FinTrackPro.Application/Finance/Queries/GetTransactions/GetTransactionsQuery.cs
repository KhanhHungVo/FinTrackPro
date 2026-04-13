using FinTrackPro.Application.Common.Models;
using MediatR;

namespace FinTrackPro.Application.Finance.Queries.GetTransactions;

public record GetTransactionsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Month = null,
    string? Type = null,
    Guid? CategoryId = null,
    string SortBy = "date",
    string SortDir = "desc"
) : IRequest<PagedResult<TransactionDto>>;
