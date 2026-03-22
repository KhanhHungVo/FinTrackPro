using FinTrackPro.Application.Signals.Queries.GetSignals;
using FluentAssertions;

namespace FinTrackPro.Application.UnitTests.Validators;

public class GetSignalsQueryValidatorTests
{
    private readonly GetSignalsQueryValidator _validator = new();

    [Theory]
    [InlineData(1)]
    [InlineData(20)]
    [InlineData(1000)]
    public void Validate_ValidCount_Passes(int count)
    {
        var result = _validator.Validate(new GetSignalsQuery(count));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCount_Fails(int count)
    {
        var result = _validator.Validate(new GetSignalsQuery(count));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Count must be greater than zero.");
    }

    [Fact]
    public void Validate_CountExceedsMaximum_Fails()
    {
        var result = _validator.Validate(new GetSignalsQuery(1001));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Count must not exceed 1000.");
    }
}
