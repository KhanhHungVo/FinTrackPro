using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Finance.Queries.GetTransactions;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Finance;

public class GetTransactionsHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly GetTransactionsQueryHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("kc-test", "test@dev.com", "Test", "local");

    public GetTransactionsHandlerTests()
    {
        _handler = new GetTransactionsQueryHandler(_userRepository, _transactionRepository, _currentUser);
        _currentUser.KeycloakUserId.Returns("kc-test");
    }

    [Fact]
    public async Task Handle_NoMonthFilter_ReturnsAllTransactionsOrderedByDateDesc()
    {
        var older = Transaction.Create(TestUser.Id, TransactionType.Expense, 10m, "Food", null, "2026-02");
        var newer = Transaction.Create(TestUser.Id, TransactionType.Income, 200m, "Salary", null, "2026-03");

        _userRepository.GetByKeycloakIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _transactionRepository.GetByUserAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { older, newer });

        var result = (await _handler.Handle(new GetTransactionsQuery(), CancellationToken.None)).ToList();

        result.Should().HaveCount(2);
        // Ordered by CreatedAt descending — newer was created after older
        result.First().Amount.Should().Be(200m);
    }

    [Fact]
    public async Task Handle_WithMonthFilter_ReturnsOnlyMatchingMonth()
    {
        var march = Transaction.Create(TestUser.Id, TransactionType.Expense, 50m, "Food", null, "2026-03");
        var feb = Transaction.Create(TestUser.Id, TransactionType.Expense, 30m, "Transport", null, "2026-02");

        _userRepository.GetByKeycloakIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _transactionRepository.GetByUserAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(new[] { march, feb });

        var result = (await _handler.Handle(new GetTransactionsQuery("2026-03"), CancellationToken.None)).ToList();

        result.Should().HaveCount(1);
        result.Single().BudgetMonth.Should().Be("2026-03");
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByKeycloakIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var act = async () => await _handler.Handle(new GetTransactionsQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
