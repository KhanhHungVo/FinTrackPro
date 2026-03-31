using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FluentAssertions;

namespace FinTrackPro.Domain.UnitTests.Trading;

public class TradeTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    // Helper: build a closed trade with minimal boilerplate
    private static Trade ClosedTrade(
        string symbol = "BTCUSDT",
        TradeDirection direction = TradeDirection.Long,
        decimal entry = 30000m,
        decimal exit = 35000m,
        decimal size = 0.1m,
        decimal fees = 5m,
        string? notes = null) =>
        Trade.Create(UserId, symbol, direction, TradeStatus.Closed, entry, exit, null, size, fees, "USD", 1.0m, notes);

    // Helper: build an open trade
    private static Trade OpenTrade(
        string symbol = "BTCUSDT",
        TradeDirection direction = TradeDirection.Long,
        decimal entry = 30000m,
        decimal? currentPrice = null,
        decimal size = 0.1m,
        decimal fees = 0m) =>
        Trade.Create(UserId, symbol, direction, TradeStatus.Open, entry, null, currentPrice, size, fees, "USD", 1.0m, null);

    [Fact]
    public void Create_ValidClosedTrade_ReturnsTrade()
    {
        var trade = ClosedTrade(notes: "test");

        trade.Id.Should().NotBeEmpty();
        trade.UserId.Should().Be(UserId);
        trade.Symbol.Should().Be("BTCUSDT");
        trade.Status.Should().Be(TradeStatus.Closed);
        trade.EntryPrice.Should().Be(30000m);
        trade.ExitPrice.Should().Be(35000m);
        trade.CurrentPrice.Should().BeNull();
        trade.Currency.Should().Be("USD");
        trade.RateToUsd.Should().Be(1.0m);
    }

    [Fact]
    public void Create_ValidOpenTrade_ReturnsTrade()
    {
        var trade = OpenTrade(currentPrice: 32000m);

        trade.Status.Should().Be(TradeStatus.Open);
        trade.ExitPrice.Should().BeNull();
        trade.CurrentPrice.Should().Be(32000m);
    }

    [Fact]
    public void Create_OpenTrade_ExitPriceStoredAsNull()
    {
        // Even if caller passes exitPrice, it is discarded for open trades
        var trade = Trade.Create(UserId, "BTCUSDT", TradeDirection.Long, TradeStatus.Open,
            30000m, 35000m /* ignored */, null, 0.1m, 0m, "USD", 1.0m, null);

        trade.ExitPrice.Should().BeNull();
    }

    [Fact]
    public void Create_ClosedTrade_CurrentPriceStoredAsNull()
    {
        var trade = Trade.Create(UserId, "BTCUSDT", TradeDirection.Long, TradeStatus.Closed,
            30000m, 35000m, 32000m /* ignored */, 0.1m, 0m, "USD", 1.0m, null);

        trade.CurrentPrice.Should().BeNull();
    }

    [Fact]
    public void Create_SymbolNormalizedToUpperCase()
    {
        var trade = Trade.Create(UserId, "  ethusdt  ", TradeDirection.Short, TradeStatus.Closed,
            2000m, 1800m, null, 1m, 1m, "USD", 1.0m, null);

        trade.Symbol.Should().Be("ETHUSDT");
    }

    [Fact]
    public void Result_CalculatesPnL_Correctly_Long()
    {
        // (35000 - 30000) * 0.1 - 5 = 495
        var trade = ClosedTrade();

        trade.Result.Should().Be(495m);
    }

    [Fact]
    public void Result_CalculatesPnL_Correctly_Short()
    {
        // Short: (entry - exit) * size - fees = (35000 - 30000) * 0.1 - 5 = 495
        var trade = Trade.Create(UserId, "BTCUSDT", TradeDirection.Short, TradeStatus.Closed,
            35000m, 30000m, null, 0.1m, 5m, "USD", 1.0m, null);

        trade.Result.Should().Be(495m);
    }

    [Fact]
    public void Result_NegativePnL_WhenExitBelowEntry_ForLong()
    {
        // (28000 - 30000) * 1 - 10 = -2010
        var trade = ClosedTrade(entry: 30000m, exit: 28000m, size: 1m, fees: 10m);

        trade.Result.Should().Be(-2010m);
    }

    [Fact]
    public void Result_IsZero_ForOpenTrade()
    {
        var trade = OpenTrade();

        trade.Result.Should().Be(0m);
    }

    [Fact]
    public void UnrealizedResult_Long_CalculatesCorrectly()
    {
        // (32000 - 30000) * 0.1 = 200
        var trade = OpenTrade(currentPrice: 32000m, size: 0.1m);

        trade.UnrealizedResult.Should().Be(200m);
    }

    [Fact]
    public void UnrealizedResult_Short_CalculatesCorrectly()
    {
        // Short: (entry - current) * size = (35000 - 30000) * 0.1 = 500
        var trade = Trade.Create(UserId, "BTCUSDT", TradeDirection.Short, TradeStatus.Open,
            35000m, null, 30000m, 0.1m, 0m, "USD", 1.0m, null);

        trade.UnrealizedResult.Should().Be(500m);
    }

    [Fact]
    public void UnrealizedResult_IsNull_WhenNoPriceSet()
    {
        var trade = OpenTrade(currentPrice: null);

        trade.UnrealizedResult.Should().BeNull();
    }

    [Fact]
    public void UnrealizedResult_IsNull_ForClosedTrade()
    {
        var trade = ClosedTrade();

        trade.UnrealizedResult.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankSymbol_ThrowsDomainException(string symbol)
    {
        var act = () => Trade.Create(UserId, symbol, TradeDirection.Long, TradeStatus.Closed,
            100m, 110m, null, 1m, 1m, "USD", 1.0m, null);

        act.Should().Throw<DomainException>().WithMessage("*Symbol*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_InvalidEntryPrice_ThrowsDomainException(decimal entry)
    {
        var act = () => Trade.Create(UserId, "BTCUSDT", TradeDirection.Long, TradeStatus.Closed,
            entry, 100m, null, 1m, 0m, "USD", 1.0m, null);

        act.Should().Throw<DomainException>().WithMessage("*Entry price*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_InvalidExitPrice_ThrowsDomainException(decimal exit)
    {
        var act = () => Trade.Create(UserId, "BTCUSDT", TradeDirection.Long, TradeStatus.Closed,
            100m, exit, null, 1m, 0m, "USD", 1.0m, null);

        act.Should().Throw<DomainException>().WithMessage("*Exit price*");
    }

    [Fact]
    public void Create_ClosedTrade_NoExitPrice_ThrowsDomainException()
    {
        var act = () => Trade.Create(UserId, "BTCUSDT", TradeDirection.Long, TradeStatus.Closed,
            100m, null, null, 1m, 0m, "USD", 1.0m, null);

        act.Should().Throw<DomainException>().WithMessage("*Exit price is required for a closed trade*");
    }

    [Fact]
    public void Create_NegativeFees_ThrowsDomainException()
    {
        var act = () => ClosedTrade(fees: -1m);

        act.Should().Throw<DomainException>().WithMessage("*Fees*");
    }

    [Fact]
    public void Create_ZeroFees_IsAllowed()
    {
        var trade = ClosedTrade(fees: 0m);

        trade.Fees.Should().Be(0m);
    }

    // ---------------------------------------------------------------
    // Update method
    // ---------------------------------------------------------------

    [Fact]
    public void Update_ValidArguments_UpdatesAllFields()
    {
        var trade = ClosedTrade();

        trade.Update("ETHUSDT", TradeDirection.Short, TradeStatus.Closed, 2000m, 2500m, null, 1m, 10m, "USD", 1.0m, "new note");

        trade.Symbol.Should().Be("ETHUSDT");
        trade.Direction.Should().Be(TradeDirection.Short);
        trade.Status.Should().Be(TradeStatus.Closed);
        trade.EntryPrice.Should().Be(2000m);
        trade.ExitPrice.Should().Be(2500m);
        trade.PositionSize.Should().Be(1m);
        trade.Fees.Should().Be(10m);
        trade.Notes.Should().Be("new note");
    }

    [Fact]
    public void Update_SymbolNormalizedToUpperCase()
    {
        var trade = ClosedTrade();

        trade.Update("  ethusdt  ", TradeDirection.Short, TradeStatus.Closed, 2000m, 2500m, null, 1m, 0m, "USD", 1.0m, null);

        trade.Symbol.Should().Be("ETHUSDT");
    }

    [Fact]
    public void Update_Result_RecomputedFromNewPrices()
    {
        var trade = ClosedTrade();

        // P&L = (2500 - 2000) * 1 - 10 = 490
        trade.Update("ETHUSDT", TradeDirection.Long, TradeStatus.Closed, 2000m, 2500m, null, 1m, 10m, "USD", 1.0m, null);

        trade.Result.Should().Be(490m);
    }

    [Fact]
    public void Update_NullNotes_Allowed()
    {
        var trade = ClosedTrade(notes: "old note");

        trade.Update("BTCUSDT", TradeDirection.Long, TradeStatus.Closed, 30000m, 35000m, null, 0.1m, 5m, "USD", 1.0m, null);

        trade.Notes.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_BlankSymbol_ThrowsDomainException(string symbol)
    {
        var trade = ClosedTrade();

        var act = () => trade.Update(symbol, TradeDirection.Long, TradeStatus.Closed, 100m, 110m, null, 1m, 0m, "USD", 1.0m, null);

        act.Should().Throw<DomainException>().WithMessage("*Symbol*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Update_InvalidEntryPrice_ThrowsDomainException(decimal entry)
    {
        var trade = ClosedTrade();

        var act = () => trade.Update("BTCUSDT", TradeDirection.Long, TradeStatus.Closed, entry, 110m, null, 1m, 0m, "USD", 1.0m, null);

        act.Should().Throw<DomainException>().WithMessage("*Entry price*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Update_InvalidExitPrice_ThrowsDomainException(decimal exit)
    {
        var trade = ClosedTrade();

        var act = () => trade.Update("BTCUSDT", TradeDirection.Long, TradeStatus.Closed, 100m, exit, null, 1m, 0m, "USD", 1.0m, null);

        act.Should().Throw<DomainException>().WithMessage("*Exit price*");
    }

    [Fact]
    public void Update_NegativeFees_ThrowsDomainException()
    {
        var trade = ClosedTrade();

        var act = () => trade.Update("BTCUSDT", TradeDirection.Long, TradeStatus.Closed, 100m, 110m, null, 1m, -1m, "USD", 1.0m, null);

        act.Should().Throw<DomainException>().WithMessage("*Fees*");
    }

    [Fact]
    public void Update_ZeroFees_IsAllowed()
    {
        var trade = ClosedTrade();

        var act = () => trade.Update("BTCUSDT", TradeDirection.Long, TradeStatus.Closed, 100m, 110m, null, 1m, 0m, "USD", 1.0m, null);

        act.Should().NotThrow();
    }

    [Fact]
    public void Update_CreatedAt_Unchanged()
    {
        var trade = ClosedTrade();
        var originalCreatedAt = trade.CreatedAt;

        trade.Update("ETHUSDT", TradeDirection.Short, TradeStatus.Closed, 2000m, 2500m, null, 1m, 0m, "USD", 1.0m, null);

        trade.CreatedAt.Should().Be(originalCreatedAt);
    }

    // ---------------------------------------------------------------
    // Close method
    // ---------------------------------------------------------------

    [Fact]
    public void Close_OpenTrade_SetsStatusAndExitPrice()
    {
        var trade = OpenTrade(currentPrice: 32000m);

        trade.Close(35000m, 5m);

        trade.Status.Should().Be(TradeStatus.Closed);
        trade.ExitPrice.Should().Be(35000m);
        trade.CurrentPrice.Should().BeNull();
        trade.Fees.Should().Be(5m);
    }

    [Fact]
    public void Close_AlreadyClosed_ThrowsConflictException()
    {
        var trade = ClosedTrade();

        var act = () => trade.Close(36000m, 5m);

        act.Should().Throw<ConflictException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Close_InvalidExitPrice_ThrowsDomainException(decimal exit)
    {
        var trade = OpenTrade();

        var act = () => trade.Close(exit, 0m);

        act.Should().Throw<DomainException>().WithMessage("*Exit price*");
    }
}
