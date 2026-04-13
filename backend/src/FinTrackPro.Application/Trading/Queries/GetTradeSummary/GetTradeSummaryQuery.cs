using FinTrackPro.Application.Trading.DTOs;
using MediatR;

namespace FinTrackPro.Application.Trading.Queries.GetTradeSummary;

public record GetTradeSummaryQuery(
    string? Status = null,
    string? Direction = null,
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    string? PreferredCurrency = null,
    decimal PreferredRate = 1m
) : IRequest<TradeSummaryDto>;
