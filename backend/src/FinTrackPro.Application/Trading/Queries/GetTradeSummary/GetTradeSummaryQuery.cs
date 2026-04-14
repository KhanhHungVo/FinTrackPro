using FinTrackPro.Application.Trading.DTOs;
using MediatR;

namespace FinTrackPro.Application.Trading.Queries.GetTradeSummary;

public record GetTradeSummaryQuery : IRequest<TradeSummaryDto>
{
    public string? Status { get; init; }
    public string? Direction { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public string? PreferredCurrency { get; init; }
    public decimal PreferredRate { get; init; } = 1m;
}
