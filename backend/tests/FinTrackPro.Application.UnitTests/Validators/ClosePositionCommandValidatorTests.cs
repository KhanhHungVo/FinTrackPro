using FinTrackPro.Application.Trading.Commands.ClosePosition;
using FluentAssertions;

namespace FinTrackPro.Application.UnitTests.Validators;

public class ClosePositionCommandValidatorTests
{
    private readonly ClosePositionCommandValidator _validator = new();

    private static ClosePositionCommand Valid() => new(Guid.NewGuid(), 35000m, 5m);

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(Valid());

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveExitPrice_Fails(decimal exitPrice)
    {
        var command = Valid() with { ExitPrice = exitPrice };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Exit price is required to close a position.");
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
}
