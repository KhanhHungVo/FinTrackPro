using FinTrackPro.Domain.Common;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;

namespace FinTrackPro.Domain.Entities;

public class Transaction : BaseEntity
{
    public Guid UserId { get; private set; }
    public TransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public decimal RateToUsd { get; private set; } = 1.0m;
    public string Category { get; private set; } = string.Empty;
    public string? Note { get; private set; }
    public string BudgetMonth { get; private set; } = string.Empty; // YYYY-MM
    public DateTime CreatedAt { get; private set; }
    public Guid? CategoryId { get; private set; }

    private Transaction() { }

    public void Update(
        TransactionType type, decimal amount, string currency,
        string category, string? note, Guid? categoryId)
    {
        if (amount <= 0)
            throw new DomainException("Amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required.");

        Type = type;
        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
        Category = category.Trim();
        Note = note?.Trim();
        CategoryId = categoryId;
    }

    public static Transaction Create(
        Guid userId, TransactionType type, decimal amount,
        string currency, decimal rateToUsd,
        string category, string? note, string budgetMonth,
        Guid? categoryId = null)
    {
        if (amount <= 0)
            throw new DomainException("Amount must be greater than zero.");
        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required.");
        if (string.IsNullOrWhiteSpace(category))
            throw new DomainException("Category is required.");

        return new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Amount = amount,
            Currency = currency.Trim().ToUpperInvariant(),
            RateToUsd = rateToUsd,
            Category = category.Trim(),
            Note = note?.Trim(),
            BudgetMonth = budgetMonth,
            CreatedAt = DateTime.UtcNow,
            CategoryId = categoryId
        };
    }
}
