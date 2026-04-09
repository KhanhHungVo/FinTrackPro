using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;

namespace FinTrackPro.Application.Common.Interfaces;

/// <summary>
/// Enforces per-plan feature limits. All methods throw
/// <see cref="Domain.Exceptions.PlanLimitExceededException"/> (→ HTTP 402) when a limit is exceeded.
/// Admin users and plans with limit set to -1 are always allowed through.
/// </summary>
public interface ISubscriptionLimitService
{
    Task EnforceMonthlyTransactionLimitAsync(AppUser user, ITransactionRepository repo, string month, CancellationToken ct = default);
    Task EnforceBudgetLimitAsync(AppUser user, IBudgetRepository repo, string month, CancellationToken ct = default);
    Task EnforceTradeLimitAsync(AppUser user, ITradeRepository repo, CancellationToken ct = default);
    Task EnforceWatchlistLimitAsync(AppUser user, IWatchedSymbolRepository repo, CancellationToken ct = default);
    Task EnforceTransactionHistoryAccessAsync(AppUser user, DateTime fromDate, CancellationToken ct = default);
    Task EnforceSignalHistoryAccessAsync(AppUser user, DateTime fromDate, CancellationToken ct = default);
    Task EnforceTelegramAsync(AppUser user, CancellationToken ct = default);
}
