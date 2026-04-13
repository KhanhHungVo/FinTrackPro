using FinTrackPro.Application.Finance.DTOs;
using MediatR;

namespace FinTrackPro.Application.Finance.Queries.GetTransactionSummary;

public record GetTransactionSummaryQuery(
    string? Month = null,
    string? Type = null,
    Guid? CategoryId = null,
    string? PreferredCurrency = null,
    decimal PreferredRate = 1m
) : IRequest<TransactionSummaryDto>;
