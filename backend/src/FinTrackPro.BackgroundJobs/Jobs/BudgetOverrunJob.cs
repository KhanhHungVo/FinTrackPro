using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinTrackPro.BackgroundJobs.Jobs;

/// <summary>
/// Runs daily. Checks each user's budgets for the current month.
/// All amounts are normalised to USD via stored RateToUsd for currency-agnostic comparison.
/// Fires a Telegram alert once per category per month on first breach,
/// tracked via BudgetAlertLog to avoid repeat notifications.
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

                var budgetInUsd = budget.LimitAmount / budget.RateToUsd;
                var spentInUsd  = transactions.Sum(t => t.Amount / t.RateToUsd);

                if (spentInUsd <= budgetInUsd) continue;

                var alreadyAlerted = await context.BudgetAlertLogs
                    .AnyAsync(l => l.UserId == budget.UserId
                               && l.Category == budget.Category
                               && l.Month == currentMonth,
                        cancellationToken);

                if (alreadyAlerted) continue;

                var spentInBudgetCurrency = spentInUsd * budget.RateToUsd;
                var overage              = spentInBudgetCurrency - budget.LimitAmount;
                var message = $"Budget overrun: '{budget.Category}' spent {spentInBudgetCurrency:F2} {budget.Currency} " +
                              $"of {budget.LimitAmount:F2} {budget.Currency} limit ({overage:F2} {budget.Currency} over).";

                await notificationService.NotifyAsync(
                    budget.UserId, $"Budget Alert: {budget.Category}", message, cancellationToken);

                context.BudgetAlertLogs.Add(
                    BudgetAlertLog.Create(budget.UserId, budget.Category, currentMonth));

                await context.SaveChangesAsync(cancellationToken);

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
