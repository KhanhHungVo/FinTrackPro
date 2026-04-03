using FinTrackPro.Application.Finance.Commands.CreateTransaction;
using FinTrackPro.Domain.Enums;
using FluentAssertions;

namespace FinTrackPro.Application.UnitTests.Validators;

public class CreateTransactionCommandValidatorTests
{
    private readonly CreateTransactionCommandValidator _validator = new();

    private static CreateTransactionCommand Valid() =>
        new(TransactionType.Expense, 100m, "USD", Guid.NewGuid(), null, "2026-03");

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(Valid());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidEnumType_Fails()
    {
        var command = Valid() with { Type = (TransactionType)999 };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Type must be a valid transaction type.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveAmount_Fails(decimal amount)
    {
        var command = Valid() with { Amount = amount };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Amount must be greater than zero.");
    }

    [Fact]
    public void Validate_EmptyCurrency_Fails()
    {
        var command = Valid() with { Currency = "" };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    [Fact]
    public void Validate_EmptyGuidCategoryId_Fails()
    {
        var command = Valid() with { CategoryId = Guid.Empty };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "CategoryId is required.");
    }

    [Theory]
    [InlineData("26-03")]
    [InlineData("2026-3")]
    [InlineData("2026/03")]
    [InlineData("March 2026")]
    [InlineData("")]
    public void Validate_InvalidBudgetMonthFormat_Fails(string budgetMonth)
    {
        var command = Valid() with { BudgetMonth = budgetMonth };

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BudgetMonth");
    }
}
