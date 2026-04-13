using FinTrackPro.Application.Common.Models;
using MediatR;

namespace FinTrackPro.Application.Trading.Queries.GetTrades;

public record GetTradesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? Status = null,
    string? Direction = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    string SortBy = "date",
    string SortDir = "desc"
) : IRequest<PagedResult<TradeDto>>;
