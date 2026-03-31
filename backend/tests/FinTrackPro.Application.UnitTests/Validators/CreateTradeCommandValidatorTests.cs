using FinTrackPro.Application.Trading.Commands.CreateTrade;
using FinTrackPro.Domain.Enums;
using FluentAssertions;

namespace FinTrackPro.Application.UnitTests.Validators;

public class CreateTradeCommandValidatorTests
{
    private readonly CreateTradeCommandValidator _validator = new();

    private static CreateTradeCommand ValidClosed() =>
        new("BTCUSDT", TradeDirection.Long, TradeStatus.Closed, 50000m, 55000m, null, 0.1m, 5m, "USD", null);

    private static CreateTradeCommand ValidOpen() =>
        new("BTCUSDT", TradeDirection.Long, TradeStatus.Open, 50000m, null, 52000m, 0.1m, 0m, "USD", null);

    [Fact]
    public void Validate_ValidClosedCommand_Passes()
    {
        var result = _validator.Validate(ValidClosed());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValidOpenCommand_Passes()
    {
        var result = _validator.Validate(ValidOpen());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_OpenCommandWithoutCurrentPrice_Passes()
    {
        var command = ValidOpen() with { CurrentPrice = null };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ClosedCommandWithoutExitPrice_Fails()
    {
        var command = ValidClosed() with { ExitPrice = null };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Exit price is required for a closed trade.");
    }

    [Fact]
    public void Validate_OpenCommandWithNoExitPrice_Passes()
    {
        var command = ValidOpen() with { ExitPrice = null };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptySymbol_Fails()
    {
        var command = ValidClosed() with { Symbol = "" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Symbol is required.");
    }

    [Theory]
    [InlineData("BTCUSDT")]
    [InlineData("VIC")]
    [InlineData("VN30")]
    [InlineData("AAPL")]
    [InlineData("EUR/USD")]
    [InlineData("GBP-VND")]
    public void Validate_ValidSymbolFormats_Pass(string symbol)
    {
        var command = ValidClosed() with { Symbol = symbol };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("btcusdt")]
    [InlineData("BTC USDT")]
    [InlineData("BTC@USDT")]
    [InlineData("AVERYLONGSYMBOLNAME12345")]
    public void Validate_InvalidSymbolFormats_Fail(string symbol)
    {
        var command = ValidClosed() with { Symbol = symbol };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_InvalidDirection_Fails()
    {
        var command = ValidClosed() with { Direction = (TradeDirection)999 };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Direction must be a valid trade direction.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveEntryPrice_Fails(decimal entryPrice)
    {
        var command = ValidClosed() with { EntryPrice = entryPrice };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Entry price must be greater than zero.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveExitPrice_Fails(decimal exitPrice)
    {
        var command = ValidClosed() with { ExitPrice = exitPrice };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Exit price must be greater than zero.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositivePositionSize_Fails(decimal positionSize)
    {
        var command = ValidClosed() with { PositionSize = positionSize };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Position size must be greater than zero.");
    }

    [Fact]
    public void Validate_NegativeFees_Fails()
    {
        var command = ValidClosed() with { Fees = -0.01m };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Fees cannot be negative.");
    }

    [Fact]
    public void Validate_ZeroFees_Passes()
    {
        var command = ValidClosed() with { Fees = 0m };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
