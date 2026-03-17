using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FluentAssertions;

namespace FinTrackPro.Domain.UnitTests.Trading;

public class WatchedSymbolTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_ValidSymbol_ReturnsWatchedSymbol()
    {
        var ws = WatchedSymbol.Create(UserId, "btcusdt");

        ws.Id.Should().NotBeEmpty();
        ws.UserId.Should().Be(UserId);
        ws.Symbol.Should().Be("BTCUSDT");
    }

    [Fact]
    public void Create_SymbolIsNormalizedToUpperCase()
    {
        var ws = WatchedSymbol.Create(UserId, "  ethusdt  ");

        ws.Symbol.Should().Be("ETHUSDT");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankSymbol_ThrowsDomainException(string symbol)
    {
        var act = () => WatchedSymbol.Create(UserId, symbol);

        act.Should().Throw<DomainException>().WithMessage("*Symbol*");
    }
}
