using FinTrackPro.Domain.Enums;
using MediatR;

namespace FinTrackPro.Application.Finance.Commands.CreateTransaction;

public record CreateTransactionCommand(
    TransactionType Type,
    decimal Amount,
    string Currency,
    Guid CategoryId,
    string? Note,
    string BudgetMonth  // YYYY-MM
) : IRequest<Guid>;
