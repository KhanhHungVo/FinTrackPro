using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Trading.Queries.GetTradeSummary;
using FinTrackPro.Domain.Common;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Trading;

internal class TradeSummaryTestDbContext(DbContextOptions options) : DbContext(options), IApplicationDbContext
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<UserIdentity> UserIdentities => Set<UserIdentity>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<WatchedSymbol> WatchedSymbols => Set<WatchedSymbol>();
    public DbSet<Signal> Signals => Set<Signal>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<BaseEvent>();
        base.OnModelCreating(modelBuilder);
    }
}

public class GetTradeSummaryHandlerTests : IDisposable
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly TradeSummaryTestDbContext _context;
    private readonly GetTradeSummaryQueryHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");

    public GetTradeSummaryHandlerTests()
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new TradeSummaryTestDbContext(options);
        _handler = new GetTradeSummaryQueryHandler(_userRepository, _context, _currentUser);
        _currentUser.UserId.Returns(TestUser.Id);

        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Handle_AllTradesInPreferredCurrency_SumsPnlDirectlyWithoutRoundTrip()
    {
        // Long closed: (65000 - 60000) * 0.1 - 5 = 495 USD
        // Short closed: (4000 - 2000) * 1 - 10 = 1990 USD
        // All USD; preferredCurrency = USD → short-circuit, no conversion
        _context.Trades.AddRange(
            Trade.Create(TestUser.Id, "BTCUSDT", TradeDirection.Long, TradeStatus.Closed,
                60000m, 65000m, null, 0.1m, 5m, "USD", 1m, null),
            Trade.Create(TestUser.Id, "ETH", TradeDirection.Short, TradeStatus.Closed,
                4000m, 2000m, null, 1m, 10m, "USD", 1m, null)
        );
        await _context.SaveChangesAsync(CancellationToken.None);

        var result = await _handler.Handle(
            new GetTradeSummaryQuery { PreferredCurrency = "USD", PreferredRate = 1m },
            CancellationToken.None);

        result.TotalPnl.Should().Be(2485m); // 495 + 1990
        result.TotalTrades.Should().Be(2);
        result.WinRate.Should().Be(100);
    }

    [Fact]
    public async Task Handle_MixedCurrencies_NormalizesPnlViaPreferredRate()
    {
        // USD trade: (65000 - 60000) * 0.1 - 5 = 495 USD → currency != VND → 495 / 1 * 25000 = 12,375,000 VND
        // VND trade: (1_100_000 - 1_000_000) * 1 - 5000 = 95_000 VND → currency == VND → short-circuit → 95_000 VND
        _context.Trades.AddRange(
            Trade.Create(TestUser.Id, "BTCUSDT", TradeDirection.Long, TradeStatus.Closed,
                60000m, 65000m, null, 0.1m, 5m, "USD", 1m, null),
            Trade.Create(TestUser.Id, "LOCAL", TradeDirection.Long, TradeStatus.Closed,
                1_000_000m, 1_100_000m, null, 1m, 5000m, "VND", 25000m, null)
        );
        await _context.SaveChangesAsync(CancellationToken.None);

        var result = await _handler.Handle(
            new GetTradeSummaryQuery { PreferredCurrency = "VND", PreferredRate = 25000m },
            CancellationToken.None);

        result.TotalPnl.Should().Be(12_470_000m); // 12_375_000 + 95_000
    }

    [Fact]
    public async Task Handle_UnrealizedPnl_NormalizesToPreferredCurrency()
    {
        // Open long: (2000 - 4000) * 1 = -2000 USD → * 25000 / 1 = -50,000,000 VND
        _context.Trades.Add(
            Trade.Create(TestUser.Id, "ETHUSDT", TradeDirection.Long, TradeStatus.Open,
                4000m, null, 2000m, 1m, 10m, "USD", 1m, null)
        );
        await _context.SaveChangesAsync(CancellationToken.None);

        var result = await _handler.Handle(
            new GetTradeSummaryQuery { PreferredCurrency = "VND", PreferredRate = 25000m },
            CancellationToken.None);

        result.UnrealizedPnl.Should().Be(-50_000_000m);
        result.TotalPnl.Should().Be(0m); // no closed trades
    }

    [Fact]
    public async Task Handle_PreferredRateZero_DefaultsToOne()
    {
        _context.Trades.Add(
            Trade.Create(TestUser.Id, "BTCUSDT", TradeDirection.Long, TradeStatus.Closed,
                60000m, 65000m, null, 0.1m, 5m, "USD", 1m, null)
        );
        await _context.SaveChangesAsync(CancellationToken.None);

        // preferredRate = 0 → guarded to 1; USD == preferred → short-circuit
        var result = await _handler.Handle(
            new GetTradeSummaryQuery { PreferredCurrency = "USD", PreferredRate = 0m },
            CancellationToken.None);

        result.TotalPnl.Should().Be(495m);
    }

    [Fact]
    public async Task Handle_EmptyTrades_ReturnsZeros()
    {
        var result = await _handler.Handle(
            new GetTradeSummaryQuery { PreferredCurrency = "VND", PreferredRate = 25000m },
            CancellationToken.None);

        result.TotalPnl.Should().Be(0m);
        result.UnrealizedPnl.Should().Be(0m);
        result.TotalTrades.Should().Be(0);
        result.WinRate.Should().Be(0);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var act = async () => await _handler.Handle(
            new GetTradeSummaryQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WinRate_CalculatedCorrectly()
    {
        // 2 wins, 1 loss → 67%
        _context.Trades.AddRange(
            Trade.Create(TestUser.Id, "A", TradeDirection.Long, TradeStatus.Closed, 100m, 110m, null, 1m, 0m, "USD", 1m, null),
            Trade.Create(TestUser.Id, "B", TradeDirection.Long, TradeStatus.Closed, 100m, 110m, null, 1m, 0m, "USD", 1m, null),
            Trade.Create(TestUser.Id, "C", TradeDirection.Long, TradeStatus.Closed, 100m, 90m, null, 1m, 0m, "USD", 1m, null)
        );
        await _context.SaveChangesAsync(CancellationToken.None);

        var result = await _handler.Handle(
            new GetTradeSummaryQuery { PreferredCurrency = "USD", PreferredRate = 1m },
            CancellationToken.None);

        result.WinRate.Should().Be(67);
    }
}
