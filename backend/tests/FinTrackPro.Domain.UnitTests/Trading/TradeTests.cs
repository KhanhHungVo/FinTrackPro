using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FluentAssertions;

namespace FinTrackPro.Domain.UnitTests.Trading;

public class TradeTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_ValidArguments_ReturnsTrade()
    {
        var trade = Trade.Create(UserId, "btcusdt", TradeDirection.Long, 30000m, 35000m, 0.1m, 5m, "test");

        trade.Id.Should().NotBeEmpty();
        trade.UserId.Should().Be(UserId);
        trade.Symbol.Should().Be("BTCUSDT");  // normalized to upper
        trade.EntryPrice.Should().Be(30000m);
        trade.ExitPrice.Should().Be(35000m);
    }

    [Fact]
    public void Create_SymbolNormalizedToUpperCase()
    {
        var trade = Trade.Create(UserId, "  ethusdt  ", TradeDirection.Short, 2000m, 1800m, 1m, 1m, null);

        trade.Symbol.Should().Be("ETHUSDT");
    }

    [Fact]
    public void Result_CalculatesPnL_Correctly()
    {
        // P&L = (ExitPrice - EntryPrice) * PositionSize - Fees
        // = (35000 - 30000) * 0.1 - 5 = 500 - 5 = 495
        var trade = Trade.Create(UserId, "BTCUSDT", TradeDirection.Long, 30000m, 35000m, 0.1m, 5m, null);

        trade.Result.Should().Be(495m);
    }

    [Fact]
    public void Result_NegativePnL_WhenExitBelowEntry_ForLong()
    {
        // (28000 - 30000) * 1 - 10 = -2010
        var trade = Trade.Create(UserId, "BTCUSDT", TradeDirection.Long, 30000m, 28000m, 1m, 10m, null);

        trade.Result.Should().Be(-2010m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankSymbol_ThrowsDomainException(string symbol)
    {
        var act = () => Trade.Create(UserId, symbol, TradeDirection.Long, 100m, 110m, 1m, 1m, null);

        act.Should().Throw<DomainException>().WithMessage("*Symbol*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_InvalidEntryPrice_ThrowsDomainException(decimal entry)
    {
        var act = () => Trade.Create(UserId, "BTCUSDT", TradeDirection.Long, entry, 100m, 1m, 0m, null);

        act.Should().Throw<DomainException>().WithMessage("*Entry price*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_InvalidExitPrice_ThrowsDomainException(decimal exit)
    {
        var act = () => Trade.Create(UserId, "BTCUSDT", TradeDirection.Long, 100m, exit, 1m, 0m, null);

        act.Should().Throw<DomainException>().WithMessage("*Exit price*");
    }

    [Fact]
    public void Create_NegativeFees_ThrowsDomainException()
    {
        var act = () => Trade.Create(UserId, "BTCUSDT", TradeDirection.Long, 100m, 110m, 1m, -1m, null);

        act.Should().Throw<DomainException>().WithMessage("*Fees*");
    }

    [Fact]
    public void Create_ZeroFees_IsAllowed()
    {
        var trade = Trade.Create(UserId, "BTCUSDT", TradeDirection.Long, 100m, 110m, 1m, 0m, null);

        trade.Fees.Should().Be(0m);
    }
}
