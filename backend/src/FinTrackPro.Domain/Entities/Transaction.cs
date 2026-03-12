using FinTrackPro.Domain.Common;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;

namespace FinTrackPro.Domain.Entities;

public class Transaction : BaseEntity
{
    public Guid UserId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string? Note { get; private set; }
    public string BudgetMonth { get; private set; } = string.Empty; // YYYY-MM
    public DateTime CreatedAt { get; private set; }

    private Transaction() { }

    public static Transaction Create(
        Guid userId, TransactionType type, decimal amount,
        string category, string? note, string budgetMonth)
    {
        if (amount <= 0)
            throw new DomainException("Amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(category))
            throw new DomainException("Category is required.");

        return new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Amount = amount,
            Category = category.Trim(),
            Note = note?.Trim(),
            BudgetMonth = budgetMonth,
            CreatedAt = DateTime.UtcNow
        };
    }
}
