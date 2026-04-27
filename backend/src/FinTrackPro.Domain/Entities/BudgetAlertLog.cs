using FinTrackPro.Domain.Common;

namespace FinTrackPro.Domain.Entities;

/// <summary>
/// Internal dedup marker: records that a budget-overrun alert was sent for a
/// given user / category / month. Never exposed via API.
/// </summary>
public class BudgetAlertLog : CreatedEntity
{
    public Guid UserId { get; private set; }
    public string Category { get; private set; } = string.Empty;
    public string Month { get; private set; } = string.Empty;

    private BudgetAlertLog() { }

    public static BudgetAlertLog Create(Guid userId, string category, string month) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Category = category,
            Month = month,
        };
}
