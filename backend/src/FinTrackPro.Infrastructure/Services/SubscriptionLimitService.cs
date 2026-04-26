using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Options;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace FinTrackPro.Infrastructure.Services;

public class SubscriptionLimitService(
    ICurrentUser currentUser,
    IOptions<SubscriptionPlanOptions> options) : ISubscriptionLimitService
{
    private bool IsUnlimited() => currentUser.IsAdmin;

    private static bool IsProActive(AppUser user) =>
        user.Plan == SubscriptionPlan.Pro &&
        (!user.SubscriptionExpiresAt.HasValue || user.SubscriptionExpiresAt.Value > DateTime.UtcNow);

    private PlanLimits GetLimits(AppUser user) => user.Plan switch
    {
        SubscriptionPlan.Pro => options.Value.Pro,
        _                    => options.Value.Free,
    };

    public async Task EnforceMonthlyTransactionLimitAsync(
        AppUser user, ITransactionRepository repo, string month, CancellationToken ct = default)
    {
        if (IsUnlimited()) return;
        var limits = GetLimits(user);
        if (limits.MonthlyTransactionLimit == -1) return;

        var count = await repo.CountByUserAndMonthAsync(user.Id, month, ct);
        if (count >= limits.MonthlyTransactionLimit)
            throw new PlanLimitExceededException("transaction",
                $"Monthly transaction limit of {limits.MonthlyTransactionLimit} reached for your current plan.");
    }

    public async Task EnforceBudgetLimitAsync(
        AppUser user, IBudgetRepository repo, string month, CancellationToken ct = default)
    {
        if (IsUnlimited()) return;
        var limits = GetLimits(user);
        if (limits.ActiveBudgetLimit == -1) return;

        var count = await repo.CountByUserAndMonthAsync(user.Id, month, ct);
        if (count >= limits.ActiveBudgetLimit)
            throw new PlanLimitExceededException("budget",
                $"Budget limit of {limits.ActiveBudgetLimit} reached for your current plan.");
    }

    public async Task EnforceTradeLimitAsync(
        AppUser user, ITradeRepository repo, CancellationToken ct = default)
    {
        if (IsUnlimited()) return;
        var limits = GetLimits(user);
        if (limits.TotalTradeLimit == -1) return;

        var count = await repo.CountByUserAsync(user.Id, ct);
        if (count >= limits.TotalTradeLimit)
            throw new PlanLimitExceededException("trade",
                $"Trade limit of {limits.TotalTradeLimit} reached for your current plan.");
    }

    public async Task EnforceWatchlistLimitAsync(
        AppUser user, IWatchedSymbolRepository repo, CancellationToken ct = default)
    {
        if (IsUnlimited()) return;
        var limits = GetLimits(user);
        if (limits.WatchlistSymbolLimit == -1) return;

        var count = await repo.CountByUserAsync(user.Id, ct);
        if (count >= limits.WatchlistSymbolLimit)
            throw new PlanLimitExceededException("watchlist",
                $"Watchlist limit of {limits.WatchlistSymbolLimit} symbols reached for your current plan.");
    }

    public Task EnforceTransactionHistoryAccessAsync(AppUser user, DateTime fromDate, CancellationToken ct = default)
    {
        if (IsUnlimited()) return Task.CompletedTask;
        var limits = GetLimits(user);
        if (limits.TransactionHistoryDays == -1) return Task.CompletedTask;

        var cutoff = DateTime.UtcNow.AddDays(-limits.TransactionHistoryDays);
        if (fromDate < cutoff)
            throw new PlanLimitExceededException("transaction_history",
                $"Transaction history is limited to {limits.TransactionHistoryDays} days on your current plan.");

        return Task.CompletedTask;
    }

    public Task EnforceSignalHistoryAccessAsync(AppUser user, DateTime fromDate, CancellationToken ct = default)
    {
        if (IsUnlimited()) return Task.CompletedTask;
        var limits = GetLimits(user);
        if (limits.SignalHistoryDays == -1) return Task.CompletedTask;

        var cutoff = DateTime.UtcNow.AddDays(-limits.SignalHistoryDays);
        if (fromDate < cutoff)
            throw new PlanLimitExceededException("signal_history",
                $"Signal history is limited to {limits.SignalHistoryDays} days on your current plan.");

        return Task.CompletedTask;
    }

    public Task EnforceTelegramAsync(AppUser user, CancellationToken ct = default)
    {
        if (IsUnlimited()) return Task.CompletedTask;
        var limits = GetLimits(user);
        if (limits.TelegramNotificationsEnabled) return Task.CompletedTask;

        throw new PlanLimitExceededException("telegram",
            "Telegram notifications are not available on your current plan.");
    }

    public Task EnforceWatchlistReadAccessAsync(AppUser user, CancellationToken ct = default)
    {
        if (IsUnlimited()) return Task.CompletedTask;
        if (IsProActive(user)) return Task.CompletedTask;

        throw new PlanLimitExceededException("watchlist",
            "Watchlist and trading signals are available on the Pro plan.");
    }
}
