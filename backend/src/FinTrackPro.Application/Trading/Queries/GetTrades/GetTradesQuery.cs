using FinTrackPro.Application.Common.Models;
using MediatR;

namespace FinTrackPro.Application.Trading.Queries.GetTrades;

public record GetTradesQuery : IRequest<PagedResult<TradeDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? Status { get; init; }
    public string? Direction { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public string SortBy { get; init; } = "date";
    public string SortDir { get; init; } = "desc";
}
