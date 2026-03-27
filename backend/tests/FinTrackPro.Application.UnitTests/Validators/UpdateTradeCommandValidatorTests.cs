using FinTrackPro.Application.Trading.Commands.UpdateTrade;
using FinTrackPro.Domain.Enums;
using FluentAssertions;

namespace FinTrackPro.Application.UnitTests.Validators;

public class UpdateTradeCommandValidatorTests
{
    private readonly UpdateTradeCommandValidator _validator = new();

    private static UpdateTradeCommand Valid() =>
        new(Guid.NewGuid(), "BTCUSDT", TradeDirection.Long, 50000m, 55000m, 0.1m, 5m, null);

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(Valid());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptySymbol_Fails()
    {
        var command = Valid() with { Symbol = "" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Symbol is required.");
    }

    [Fact]
    public void Validate_InvalidDirection_Fails()
    {
        var command = Valid() with { Direction = (TradeDirection)999 };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Direction must be a valid trade direction.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveEntryPrice_Fails(decimal entryPrice)
    {
        var command = Valid() with { EntryPrice = entryPrice };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Entry price must be greater than zero.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveExitPrice_Fails(decimal exitPrice)
    {
        var command = Valid() with { ExitPrice = exitPrice };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Exit price must be greater than zero.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositivePositionSize_Fails(decimal positionSize)
    {
        var command = Valid() with { PositionSize = positionSize };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Position size must be greater than zero.");
    }

    [Fact]
    public void Validate_NegativeFees_Fails()
    {
        var command = Valid() with { Fees = -0.01m };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Fees cannot be negative.");
    }

    [Fact]
    public void Validate_ZeroFees_Passes()
    {
        var command = Valid() with { Fees = 0m };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NotesExceeding1000Characters_Fails()
    {
        var command = Valid() with { Notes = new string('a', 1001) };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Notes must not exceed 1000 characters.");
    }

    [Fact]
    public void Validate_NullNotes_Passes()
    {
        var command = Valid() with { Notes = null };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MultilineNotes_Passes()
    {
        var command = Valid() with { Notes = "Line one\nLine two\nLine three" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
