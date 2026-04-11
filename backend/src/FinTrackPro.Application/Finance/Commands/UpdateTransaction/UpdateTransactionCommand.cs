using FinTrackPro.Domain.Enums;
using MediatR;

namespace FinTrackPro.Application.Finance.Commands.UpdateTransaction;

public record UpdateTransactionCommand(
    Guid Id,
    TransactionType Type,
    decimal Amount,
    string Currency,
    string Category,
    string? Note,
    Guid? CategoryId
) : IRequest;
