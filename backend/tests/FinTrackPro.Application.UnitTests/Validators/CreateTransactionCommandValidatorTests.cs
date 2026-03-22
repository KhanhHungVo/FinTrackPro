using FinTrackPro.Application.Finance.Commands.CreateTransaction;
using FinTrackPro.Domain.Enums;
using FluentAssertions;

namespace FinTrackPro.Application.UnitTests.Validators;

public class CreateTransactionCommandValidatorTests
{
    private readonly CreateTransactionCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var command = new CreateTransactionCommand(TransactionType.Expense, 100m, "Food", null, "2026-03");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidEnumType_Fails()
    {
        var command = new CreateTransactionCommand((TransactionType)999, 100m, "Food", null, "2026-03");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Type must be a valid transaction type.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveAmount_Fails(decimal amount)
    {
        var command = new CreateTransactionCommand(TransactionType.Expense, amount, "Food", null, "2026-03");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Amount must be greater than zero.");
    }

    [Fact]
    public void Validate_EmptyCategory_Fails()
    {
        var command = new CreateTransactionCommand(TransactionType.Expense, 100m, "", null, "2026-03");

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.ErrorMessage == "Category is required.");
    }

    [Theory]
    [InlineData("26-03")]
    [InlineData("2026-3")]
    [InlineData("2026/03")]
    [InlineData("March 2026")]
    [InlineData("")]
    public void Validate_InvalidBudgetMonthFormat_Fails(string budgetMonth)
    {
        var command = new CreateTransactionCommand(TransactionType.Expense, 100m, "Food", null, budgetMonth);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "BudgetMonth");
    }
}
