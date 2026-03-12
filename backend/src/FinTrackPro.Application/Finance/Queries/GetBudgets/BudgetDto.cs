using FinTrackPro.Domain.Entities;

namespace FinTrackPro.Application.Finance.Queries.GetBudgets;

public record BudgetDto(
    Guid Id,
    string Category,
    decimal LimitAmount,
    string Month,
    DateTime CreatedAt)
{
    public static explicit operator BudgetDto(Budget b) => new(
        b.Id, b.Category, b.LimitAmount, b.Month, b.CreatedAt);
}
