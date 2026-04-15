using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Finance.Queries.GetTransactionSummary;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Finance;

// Minimal in-memory DbContext — avoids a dependency on FinTrackPro.Infrastructure
internal class TestDbContext(DbContextOptions options) : DbContext(options), IApplicationDbContext
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<UserIdentity> UserIdentities => Set<UserIdentity>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<WatchedSymbol> WatchedSymbols => Set<WatchedSymbol>();
    public DbSet<Signal> Signals => Set<Signal>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(
        System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken)
        => throw new NotSupportedException("Transactions are not supported in the in-memory test context.");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<FinTrackPro.Domain.Common.BaseEvent>();
        base.OnModelCreating(modelBuilder);
    }
}

public class GetTransactionSummaryHandlerTests : IDisposable
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly TestDbContext _context;
    private readonly GetTransactionSummaryQueryHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");

    public GetTransactionSummaryHandlerTests()
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
        _handler = new GetTransactionSummaryQueryHandler(_userRepository, _context, _currentUser);
        _currentUser.UserId.Returns(TestUser.Id);

        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Handle_AllTransactionsInPreferredCurrency_SumsDirectlyWithoutRoundTrip()
    {
        // Arrange — all VND; rateToUsd = 25000; preferredCurrency = VND, preferredRate = 25000
        _context.Transactions.AddRange(
            Transaction.Create(TestUser.Id, TransactionType.Income, 500_000m, "VND", 25_000m, "Salary", null, "2026-04"),
            Transaction.Create(TestUser.Id, TransactionType.Income, 300_000m, "VND", 25_000m, "Bonus", null, "2026-04"),
            Transaction.Create(TestUser.Id, TransactionType.Expense, 200_000m, "VND", 25_000m, "Food", null, "2026-04")
        );
        await _context.SaveChangesAsync(CancellationToken.None);

        // Act
        var result = await _handler.Handle(
            new GetTransactionSummaryQuery { PreferredCurrency = "VND", PreferredRate = 25_000m },
            CancellationToken.None);

        // Assert — short-circuit path: totals match raw amounts exactly
        result.TotalIncome.Should().Be(800_000m);
        result.TotalExpense.Should().Be(200_000m);
        result.NetBalance.Should().Be(600_000m);
    }

    [Fact]
    public async Task Handle_MixedCurrencies_NormalizesViaUsdRoundTrip()
    {
        // Arrange — one VND, one USD; preferredCurrency = VND, preferredRate = 25000
        // VND tx: 500_000 VND, rateToUsd=25_000 → short-circuit → 500_000 VND
        // USD tx: 10 USD, rateToUsd=1 → 10/1*25_000 = 250_000 VND
        _context.Transactions.AddRange(
            Transaction.Create(TestUser.Id, TransactionType.Income, 500_000m, "VND", 25_000m, "Salary", null, "2026-04"),
            Transaction.Create(TestUser.Id, TransactionType.Income, 10m, "USD", 1m, "Freelance", null, "2026-04")
        );
        await _context.SaveChangesAsync(CancellationToken.None);

        var result = await _handler.Handle(
            new GetTransactionSummaryQuery { PreferredCurrency = "VND", PreferredRate = 25_000m },
            CancellationToken.None);

        result.TotalIncome.Should().Be(750_000m); // 500_000 + 250_000
        result.TotalExpense.Should().Be(0m);
        result.NetBalance.Should().Be(750_000m);
    }

    [Fact]
    public async Task Handle_PreferredRateZero_DefaultsToOne()
    {
        _context.Transactions.Add(
            Transaction.Create(TestUser.Id, TransactionType.Income, 100m, "USD", 1m, "Salary", null, "2026-04")
        );
        await _context.SaveChangesAsync(CancellationToken.None);

        // preferredRate = 0 should be treated as 1 (guard against division/zero-multiply issues)
        var result = await _handler.Handle(
            new GetTransactionSummaryQuery { PreferredCurrency = "USD", PreferredRate = 0m },
            CancellationToken.None);

        result.TotalIncome.Should().Be(100m); // USD == preferred → short-circuit fires
    }

    [Fact]
    public async Task Handle_EmptyTransactions_ReturnsZeros()
    {
        var result = await _handler.Handle(
            new GetTransactionSummaryQuery { PreferredCurrency = "VND", PreferredRate = 25_000m },
            CancellationToken.None);

        result.TotalIncome.Should().Be(0m);
        result.TotalExpense.Should().Be(0m);
        result.NetBalance.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var act = async () => await _handler.Handle(
            new GetTransactionSummaryQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WithMonthFilter_OnlySumsThatMonth()
    {
        _context.Transactions.AddRange(
            Transaction.Create(TestUser.Id, TransactionType.Income, 100_000m, "VND", 25_000m, "Salary", null, "2026-04"),
            Transaction.Create(TestUser.Id, TransactionType.Income, 200_000m, "VND", 25_000m, "Salary", null, "2026-03")
        );
        await _context.SaveChangesAsync(CancellationToken.None);

        var result = await _handler.Handle(
            new GetTransactionSummaryQuery { Month = "2026-04", PreferredCurrency = "VND", PreferredRate = 25_000m },
            CancellationToken.None);

        result.TotalIncome.Should().Be(100_000m);
    }
}
