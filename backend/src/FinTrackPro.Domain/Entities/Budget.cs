using FinTrackPro.Domain.Common;
using FinTrackPro.Domain.Exceptions;

namespace FinTrackPro.Domain.Entities;

public class Budget : BaseEntity
{
    public Guid UserId { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public decimal LimitAmount { get; private set; }
    public string Month { get; private set; } = string.Empty; // YYYY-MM
    public DateTime CreatedAt { get; private set; }

    private Budget() { }

    public static Budget Create(Guid userId, string category, decimal limitAmount, string month)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new DomainException("Category is required.");
        if (limitAmount <= 0)
            throw new DomainException("Limit amount must be greater than zero.");

        return new Budget
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Category = category.Trim(),
            LimitAmount = limitAmount,
            Month = month,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateLimit(decimal newLimit)
    {
        if (newLimit <= 0)
            throw new DomainException("Limit amount must be greater than zero.");
        LimitAmount = newLimit;
    }
}
