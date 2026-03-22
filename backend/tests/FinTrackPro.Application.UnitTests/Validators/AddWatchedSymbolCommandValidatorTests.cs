using FinTrackPro.Application.Trading.Commands.AddWatchedSymbol;
using FluentAssertions;

namespace FinTrackPro.Application.UnitTests.Validators;

public class AddWatchedSymbolCommandValidatorTests
{
    private readonly AddWatchedSymbolCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidSymbol_Passes()
    {
        var result = _validator.Validate(new AddWatchedSymbolCommand("BTCUSDT"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptySymbol_Fails(string symbol)
    {
        var result = _validator.Validate(new AddWatchedSymbolCommand(symbol));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Symbol is required.");
    }
}
