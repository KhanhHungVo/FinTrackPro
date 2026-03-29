using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinTrackPro.BackgroundJobs.Jobs;

/// <summary>
/// Runs daily. Checks each user's budgets for the current month.
/// All amounts are normalised to USD via stored RateToUsd for currency-agnostic comparison.
/// Fires a Telegram alert once per category per month on first breach.
/// </summary>
public class BudgetOverrunJob(
    IApplicationDbContext context,
    INotificationService notificationService,
    ILogger<BudgetOverrunJob> logger)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");

        var budgets = await context.Budgets
            .Where(b => b.Month == currentMonth)
            .ToListAsync(cancellationToken);

        foreach (var budget in budgets)
        {
            try
            {
                var transactions = await context.Transactions
                    .Where(t => t.UserId == budget.UserId
                             && t.Type == TransactionType.Expense
                             && t.Category == budget.Category
                             && t.BudgetMonth == currentMonth)
                    .ToListAsync(cancellationToken);

                // Normalise all amounts to USD using stored rates
                var budgetInUsd = budget.LimitAmount / budget.RateToUsd;
                var spentInUsd  = transactions.Sum(t => t.Amount / t.RateToUsd);

                if (spentInUsd <= budgetInUsd) continue;

                // Check if already notified this month for this budget
                var alreadyAlerted = await context.Signals
                    .AnyAsync(s => s.UserId == budget.UserId
                               && s.Symbol == $"BUDGET:{budget.Category}"
                               && s.SignalType == SignalType.FundingRate  // reused as budget overrun marker
                               && s.CreatedAt.ToString("yyyy-MM") == currentMonth,
                        cancellationToken);

                if (alreadyAlerted) continue;

                // Format back in budget's own currency for the alert message
                var spentInBudgetCurrency = spentInUsd  * budget.RateToUsd;
                var overage              = spentInBudgetCurrency - budget.LimitAmount;
                var message = $"Budget overrun: '{budget.Category}' spent {spentInBudgetCurrency:F2} {budget.Currency} " +
                              $"of {budget.LimitAmount:F2} {budget.Currency} limit ({overage:F2} {budget.Currency} over).";

                await notificationService.NotifyAsync(
                    budget.UserId, $"Budget Alert: {budget.Category}", message, cancellationToken);

                logger.LogInformation(
                    "Budget overrun alert sent for user {UserId} / category {Category}",
                    budget.UserId, budget.Category);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking budget overrun for budget {BudgetId}", budget.Id);
            }
        }
    }
}
