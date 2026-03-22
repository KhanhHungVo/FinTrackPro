using FinTrackPro.Application.Finance.Queries.GetBudgets;
using FluentAssertions;

namespace FinTrackPro.Application.UnitTests.Validators;

public class GetBudgetsQueryValidatorTests
{
    private readonly GetBudgetsQueryValidator _validator = new();

    [Theory]
    [InlineData("2026-01")]
    [InlineData("2024-12")]
    [InlineData("1999-06")]
    public void Validate_ValidMonthFormat_Passes(string month)
    {
        var result = _validator.Validate(new GetBudgetsQuery(month));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("26-03")]
    [InlineData("2026-3")]
    [InlineData("2026/03")]
    [InlineData("March 2026")]
    [InlineData("2026")]
    public void Validate_InvalidMonthFormat_Fails(string month)
    {
        var result = _validator.Validate(new GetBudgetsQuery(month));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Month must be in YYYY-MM format.");
    }

    [Fact]
    public void Validate_EmptyMonth_Fails()
    {
        var result = _validator.Validate(new GetBudgetsQuery(""));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Month");
    }
}
