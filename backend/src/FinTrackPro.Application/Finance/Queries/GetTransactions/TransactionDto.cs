using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;

namespace FinTrackPro.Application.Finance.Queries.GetTransactions;

public record TransactionDto(
    Guid Id,
    TransactionType Type,
    decimal Amount,
    string Category,
    string? Note,
    string BudgetMonth,
    DateTime CreatedAt)
{
    public static explicit operator TransactionDto(Transaction t) => new(
        t.Id, t.Type, t.Amount, t.Category, t.Note, t.BudgetMonth, t.CreatedAt);
}
