using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FluentAssertions;

namespace FinTrackPro.Domain.UnitTests.Finance;

public class BudgetTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_ValidArguments_ReturnsBudget()
    {
        var budget = Budget.Create(UserId, "Food", 500m, "2026-03");

        budget.Id.Should().NotBeEmpty();
        budget.UserId.Should().Be(UserId);
        budget.Category.Should().Be("Food");
        budget.LimitAmount.Should().Be(500m);
        budget.Month.Should().Be("2026-03");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankCategory_ThrowsDomainException(string category)
    {
        var act = () => Budget.Create(UserId, category, 100m, "2026-03");

        act.Should().Throw<DomainException>()
            .WithMessage("*Category*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Create_ZeroOrNegativeLimit_ThrowsDomainException(decimal limit)
    {
        var act = () => Budget.Create(UserId, "Food", limit, "2026-03");

        act.Should().Throw<DomainException>()
            .WithMessage("*Limit amount*");
    }

    [Fact]
    public void UpdateLimit_ValidAmount_UpdatesLimit()
    {
        var budget = Budget.Create(UserId, "Food", 500m, "2026-03");

        budget.UpdateLimit(1000m);

        budget.LimitAmount.Should().Be(1000m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void UpdateLimit_ZeroOrNegative_ThrowsDomainException(decimal newLimit)
    {
        var budget = Budget.Create(UserId, "Food", 500m, "2026-03");

        var act = () => budget.UpdateLimit(newLimit);

        act.Should().Throw<DomainException>()
            .WithMessage("*Limit amount*");
    }
}
