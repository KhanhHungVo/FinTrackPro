using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Common.Options;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FinTrackPro.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FinTrackPro.Infrastructure.UnitTests.Services;

public class SubscriptionLimitServiceTests
{
    // -------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------

    private static SubscriptionLimitService BuildService(
        bool isAdmin,
        PlanLimits? freeLimits = null,
        PlanLimits? proLimits  = null)
    {
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.IsAdmin.Returns(isAdmin);

        var opts = Options.Create(new SubscriptionPlanOptions
        {
            Free = freeLimits ?? DefaultFreeLimits(),
            Pro  = proLimits  ?? UnlimitedProLimits(),
        });

        return new SubscriptionLimitService(currentUser, opts);
    }

    private static PlanLimits DefaultFreeLimits() => new()
    {
        MonthlyTransactionLimit = 20,
        ActiveBudgetLimit       = 3,
        TotalTradeLimit         = 10,
        WatchlistSymbolLimit    = 5,
        TransactionHistoryDays  = 90,
        SignalHistoryDays       = 30,
        TelegramNotificationsEnabled = false,
    };

    private static PlanLimits UnlimitedProLimits() => new()
    {
        MonthlyTransactionLimit = -1,
        ActiveBudgetLimit       = -1,
        TotalTradeLimit         = -1,
        WatchlistSymbolLimit    = -1,
        TransactionHistoryDays  = -1,
        SignalHistoryDays       = -1,
        TelegramNotificationsEnabled = true,
    };

    private static AppUser FreeUser()
    {
        var u = AppUser.Create("free@dev.com", "Free");
        // Plan defaults to Free — no extra call needed.
        return u;
    }

    private static AppUser ProUser()
    {
        var u = AppUser.Create("pro@dev.com", "Pro");
        u.ActivateSubscription("sub_test_123", DateTime.UtcNow.AddMonths(1));
        return u;
    }

    // -------------------------------------------------------------------
    // EnforceMonthlyTransactionLimitAsync
    // -------------------------------------------------------------------

    [Fact]
    public async Task EnforceMonthlyTransaction_FreeUserBelowLimit_DoesNotThrow()
    {
        var svc  = BuildService(isAdmin: false);
        var repo = Substitute.For<ITransactionRepository>();
        repo.CountByUserAndMonthAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(5);

        var act = () => svc.EnforceMonthlyTransactionLimitAsync(FreeUser(), repo, "2026-04");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnforceMonthlyTransaction_FreeUserAtLimit_ThrowsPlanLimitExceededException()
    {
        var svc  = BuildService(isAdmin: false);
        var repo = Substitute.For<ITransactionRepository>();
        repo.CountByUserAndMonthAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(20); // equals limit

        var act = () => svc.EnforceMonthlyTransactionLimitAsync(FreeUser(), repo, "2026-04");

        await act.Should().ThrowAsync<PlanLimitExceededException>()
            .Where(e => e.Feature == "transaction");
    }

    [Fact]
    public async Task EnforceMonthlyTransaction_AdminAtLimit_DoesNotThrow()
    {
        var svc  = BuildService(isAdmin: true);
        var repo = Substitute.For<ITransactionRepository>();
        repo.CountByUserAndMonthAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(9999);

        var act = () => svc.EnforceMonthlyTransactionLimitAsync(FreeUser(), repo, "2026-04");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnforceMonthlyTransaction_ProUserWithUnlimitedPlan_DoesNotThrow()
    {
        var svc  = BuildService(isAdmin: false);
        var repo = Substitute.For<ITransactionRepository>();
        repo.CountByUserAndMonthAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(9999);

        var act = () => svc.EnforceMonthlyTransactionLimitAsync(ProUser(), repo, "2026-04");

        await act.Should().NotThrowAsync();
    }

    // -------------------------------------------------------------------
    // EnforceBudgetLimitAsync
    // -------------------------------------------------------------------

    [Fact]
    public async Task EnforceBudget_FreeUserBelowLimit_DoesNotThrow()
    {
        var svc  = BuildService(isAdmin: false);
        var repo = Substitute.For<IBudgetRepository>();
        repo.CountByUserAndMonthAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(2);

        var act = () => svc.EnforceBudgetLimitAsync(FreeUser(), repo, "2026-04");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnforceBudget_FreeUserAtLimit_ThrowsPlanLimitExceededException()
    {
        var svc  = BuildService(isAdmin: false);
        var repo = Substitute.For<IBudgetRepository>();
        repo.CountByUserAndMonthAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(3); // equals limit

        var act = () => svc.EnforceBudgetLimitAsync(FreeUser(), repo, "2026-04");

        await act.Should().ThrowAsync<PlanLimitExceededException>()
            .Where(e => e.Feature == "budget");
    }

    [Fact]
    public async Task EnforceBudget_AdminAtLimit_DoesNotThrow()
    {
        var svc  = BuildService(isAdmin: true);
        var repo = Substitute.For<IBudgetRepository>();
        repo.CountByUserAndMonthAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(9999);

        var act = () => svc.EnforceBudgetLimitAsync(FreeUser(), repo, "2026-04");

        await act.Should().NotThrowAsync();
    }

    // -------------------------------------------------------------------
    // EnforceTradeLimitAsync
    // -------------------------------------------------------------------

    [Fact]
    public async Task EnforceTradeLimit_FreeUserBelowLimit_DoesNotThrow()
    {
        var svc  = BuildService(isAdmin: false);
        var repo = Substitute.For<ITradeRepository>();
        repo.CountByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(5);

        var act = () => svc.EnforceTradeLimitAsync(FreeUser(), repo);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnforceTradeLimit_FreeUserAtLimit_ThrowsPlanLimitExceededException()
    {
        var svc  = BuildService(isAdmin: false);
        var repo = Substitute.For<ITradeRepository>();
        repo.CountByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(10); // equals limit

        var act = () => svc.EnforceTradeLimitAsync(FreeUser(), repo);

        await act.Should().ThrowAsync<PlanLimitExceededException>()
            .Where(e => e.Feature == "trade");
    }

    [Fact]
    public async Task EnforceTradeLimit_AdminAtLimit_DoesNotThrow()
    {
        var svc  = BuildService(isAdmin: true);
        var repo = Substitute.For<ITradeRepository>();
        repo.CountByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(9999);

        var act = () => svc.EnforceTradeLimitAsync(FreeUser(), repo);

        await act.Should().NotThrowAsync();
    }

    // -------------------------------------------------------------------
    // EnforceWatchlistLimitAsync
    // -------------------------------------------------------------------

    [Fact]
    public async Task EnforceWatchlist_FreeUserBelowLimit_DoesNotThrow()
    {
        var svc  = BuildService(isAdmin: false);
        var repo = Substitute.For<IWatchedSymbolRepository>();
        repo.CountByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(3);

        var act = () => svc.EnforceWatchlistLimitAsync(FreeUser(), repo);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnforceWatchlist_FreeUserAtLimit_ThrowsPlanLimitExceededException()
    {
        var svc  = BuildService(isAdmin: false);
        var repo = Substitute.For<IWatchedSymbolRepository>();
        repo.CountByUserAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(5); // equals limit

        var act = () => svc.EnforceWatchlistLimitAsync(FreeUser(), repo);

        await act.Should().ThrowAsync<PlanLimitExceededException>()
            .Where(e => e.Feature == "watchlist");
    }

    // -------------------------------------------------------------------
    // EnforceTransactionHistoryAccessAsync
    // -------------------------------------------------------------------

    [Fact]
    public async Task EnforceTransactionHistory_WithinLimit_DoesNotThrow()
    {
        var svc     = BuildService(isAdmin: false);
        var fromDate = DateTime.UtcNow.AddDays(-30);

        var act = () => svc.EnforceTransactionHistoryAccessAsync(FreeUser(), fromDate);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnforceTransactionHistory_BeyondLimit_ThrowsPlanLimitExceededException()
    {
        var svc     = BuildService(isAdmin: false);
        var fromDate = DateTime.UtcNow.AddDays(-91); // exceeds 90-day Free limit

        var act = () => svc.EnforceTransactionHistoryAccessAsync(FreeUser(), fromDate);

        await act.Should().ThrowAsync<PlanLimitExceededException>()
            .Where(e => e.Feature == "transaction_history");
    }

    [Fact]
    public async Task EnforceTransactionHistory_AdminBeyondLimit_DoesNotThrow()
    {
        var svc     = BuildService(isAdmin: true);
        var fromDate = DateTime.UtcNow.AddYears(-5);

        var act = () => svc.EnforceTransactionHistoryAccessAsync(FreeUser(), fromDate);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnforceTransactionHistory_ProUserUnlimited_DoesNotThrow()
    {
        var svc     = BuildService(isAdmin: false);
        var fromDate = DateTime.UtcNow.AddYears(-5);

        var act = () => svc.EnforceTransactionHistoryAccessAsync(ProUser(), fromDate);

        await act.Should().NotThrowAsync();
    }

    // -------------------------------------------------------------------
    // EnforceTelegramAsync
    // -------------------------------------------------------------------

    [Fact]
    public async Task EnforceTelegram_FreeUser_ThrowsPlanLimitExceededException()
    {
        var svc = BuildService(isAdmin: false);

        var act = () => svc.EnforceTelegramAsync(FreeUser());

        await act.Should().ThrowAsync<PlanLimitExceededException>()
            .Where(e => e.Feature == "telegram");
    }

    [Fact]
    public async Task EnforceTelegram_ProUser_DoesNotThrow()
    {
        var svc = BuildService(isAdmin: false);

        var act = () => svc.EnforceTelegramAsync(ProUser());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnforceTelegram_Admin_DoesNotThrow()
    {
        var svc = BuildService(isAdmin: true);

        var act = () => svc.EnforceTelegramAsync(FreeUser());

        await act.Should().NotThrowAsync();
    }
}
