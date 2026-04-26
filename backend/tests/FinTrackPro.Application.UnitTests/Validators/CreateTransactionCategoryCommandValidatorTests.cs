using FinTrackPro.Application.TransactionCategories.Commands.CreateTransactionCategory;
using FinTrackPro.Domain.Enums;
using FluentAssertions;

namespace FinTrackPro.Application.UnitTests.Validators;

public class CreateTransactionCategoryCommandValidatorTests
{
    private readonly CreateTransactionCategoryCommandValidator _validator = new();

    private static CreateTransactionCategoryCommand Valid() =>
        new(TransactionType.Expense, "pet_care", "Pet Care", "Thú cưng", "🐶");

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.Validate(Valid());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptySlug_Fails()
    {
        var result = _validator.Validate(Valid() with { Slug = "" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Slug");
    }

    [Fact]
    public void Validate_SlugWithUppercase_Fails()
    {
        var result = _validator.Validate(Valid() with { Slug = "PetCare" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Slug");
    }

    [Fact]
    public void Validate_SlugWithSpaces_Fails()
    {
        var result = _validator.Validate(Valid() with { Slug = "pet care" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Slug");
    }

    [Fact]
    public void Validate_SlugStartingWithDigit_Passes()
    {
        var result = _validator.Validate(Valid() with { Slug = "1pet_care" });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyLabelEn_Fails()
    {
        var result = _validator.Validate(Valid() with { LabelEn = "" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LabelEn");
    }

    [Fact]
    public void Validate_InvalidType_Fails()
    {
        var result = _validator.Validate(Valid() with { Type = (TransactionType)999 });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Type");
    }
}
