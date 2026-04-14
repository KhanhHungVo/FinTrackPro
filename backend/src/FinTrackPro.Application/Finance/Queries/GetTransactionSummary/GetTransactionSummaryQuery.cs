using FinTrackPro.Application.Finance.DTOs;
using MediatR;

namespace FinTrackPro.Application.Finance.Queries.GetTransactionSummary;

public record GetTransactionSummaryQuery : IRequest<TransactionSummaryDto>
{
    public string? Month { get; init; }
    public string? Type { get; init; }
    public Guid? CategoryId { get; init; }
    public string? PreferredCurrency { get; init; }
    public decimal PreferredRate { get; init; } = 1m;
}
