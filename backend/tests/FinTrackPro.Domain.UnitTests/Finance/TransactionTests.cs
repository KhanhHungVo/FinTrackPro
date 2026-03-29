using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FluentAssertions;

namespace FinTrackPro.Domain.UnitTests.Finance;

public class TransactionTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_ValidArguments_ReturnsTransaction()
    {
        var tx = Transaction.Create(UserId, TransactionType.Expense, 100m, "USD", 1.0m, "Food", null, "2026-03");

        tx.Id.Should().NotBeEmpty();
        tx.UserId.Should().Be(UserId);
        tx.Amount.Should().Be(100m);
        tx.Currency.Should().Be("USD");
        tx.RateToUsd.Should().Be(1.0m);
        tx.Category.Should().Be("Food");
        tx.BudgetMonth.Should().Be("2026-03");
        tx.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ZeroOrNegativeAmount_ThrowsDomainException(decimal amount)
    {
        var act = () => Transaction.Create(UserId, TransactionType.Expense, amount, "USD", 1.0m, "Food", null, "2026-03");

        act.Should().Throw<DomainException>()
            .WithMessage("*Amount*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankCategory_ThrowsDomainException(string category)
    {
        var act = () => Transaction.Create(UserId, TransactionType.Expense, 50m, "USD", 1.0m, category, null, "2026-03");

        act.Should().Throw<DomainException>()
            .WithMessage("*Category*");
    }

    [Fact]
    public void Create_WhitespaceInCategory_IsTrimmed()
    {
        var tx = Transaction.Create(UserId, TransactionType.Income, 200m, "USD", 1.0m, "  Salary  ", null, "2026-03");

        tx.Category.Should().Be("Salary");
    }

    [Fact]
    public void Create_NullNote_IsAllowed()
    {
        var tx = Transaction.Create(UserId, TransactionType.Expense, 10m, "USD", 1.0m, "Food", null, "2026-03");

        tx.Note.Should().BeNull();
    }

    [Fact]
    public void Create_CurrencyNormalizedToUpperCase()
    {
        var tx = Transaction.Create(UserId, TransactionType.Expense, 500000m, "vnd", 0.0000393m, "Food", null, "2026-03");

        tx.Currency.Should().Be("VND");
    }
}
