using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FluentAssertions;

namespace FinTrackPro.Domain.UnitTests.Finance;

public class TransactionCategoryTests
{
    [Fact]
    public void Create_ValidArguments_ReturnsCategory()
    {
        var category = TransactionCategory.Create(
            userId: null, type: TransactionType.Expense,
            slug: "food_beverage", labelEn: "Food & Beverage",
            labelVi: "Ăn uống", icon: "🍜", isSystem: true, sortOrder: 1);

        category.Id.Should().NotBeEmpty();
        category.Slug.Should().Be("food_beverage");
        category.LabelEn.Should().Be("Food & Beverage");
        category.LabelVi.Should().Be("Ăn uống");
        category.Icon.Should().Be("🍜");
        category.Type.Should().Be(TransactionType.Expense);
        category.IsSystem.Should().BeTrue();
        category.IsActive.Should().BeTrue();
        category.SortOrder.Should().Be(1);
        category.UserId.Should().BeNull();
    }

    [Fact]
    public void Create_BlankSlug_ThrowsDomainException()
    {
        var act = () => TransactionCategory.Create(null, TransactionType.Expense, "  ", "Food", "Ăn", "🍜");

        act.Should().Throw<DomainException>().WithMessage("*Slug*");
    }

    [Fact]
    public void Create_BlankLabelEn_ThrowsDomainException()
    {
        var act = () => TransactionCategory.Create(null, TransactionType.Expense, "food", "  ", "Ăn", "🍜");

        act.Should().Throw<DomainException>().WithMessage("*label*");
    }

    [Fact]
    public void Create_SlugNormalizedToLowercase()
    {
        var category = TransactionCategory.Create(null, TransactionType.Expense, "  FOOD_BEV  ", "Food", "Ăn", "🍜");

        category.Slug.Should().Be("food_bev");
    }

    [Fact]
    public void UpdateLabels_UserCategory_UpdatesFields()
    {
        var userId = Guid.NewGuid();
        var category = TransactionCategory.Create(userId, TransactionType.Expense, "pet_care", "Pet Care", "Thú cưng", "🐶");

        category.UpdateLabels("Pets", "Thú nuôi", "🐾");

        category.LabelEn.Should().Be("Pets");
        category.LabelVi.Should().Be("Thú nuôi");
        category.Icon.Should().Be("🐾");
    }

    [Fact]
    public void UpdateLabels_SystemCategory_ThrowsAuthorizationException()
    {
        var category = TransactionCategory.Create(null, TransactionType.Expense, "food", "Food", "Ăn", "🍜", isSystem: true);

        var act = () => category.UpdateLabels("New Food", "Ăn mới", "🥗");

        act.Should().Throw<AuthorizationException>();
    }

    [Fact]
    public void SoftDelete_UserCategory_SetsIsActiveFalse()
    {
        var userId = Guid.NewGuid();
        var category = TransactionCategory.Create(userId, TransactionType.Expense, "pet_care", "Pet Care", "Thú cưng", "🐶");

        category.SoftDelete();

        category.IsActive.Should().BeFalse();
    }

    [Fact]
    public void SoftDelete_SystemCategory_ThrowsAuthorizationException()
    {
        var category = TransactionCategory.Create(null, TransactionType.Expense, "food", "Food", "Ăn", "🍜", isSystem: true);

        var act = () => category.SoftDelete();

        act.Should().Throw<AuthorizationException>();
    }
}
