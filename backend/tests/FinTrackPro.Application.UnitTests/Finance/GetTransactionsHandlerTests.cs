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
    private readonly ISubscriptionLimitService _limitService = Substitute.For<ISubscriptionLimitService>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly GetTransactionsQueryHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");

    public GetTransactionsHandlerTests()
    {
        _handler = new GetTransactionsQueryHandler(_userRepository, _transactionRepository, _limitService, _currentUser);
        _currentUser.UserId.Returns(TestUser.Id);
    }

    [Fact]
    public async Task Handle_NoFilters_ReturnsPagedResult()
    {
        var tx = Transaction.Create(TestUser.Id, TransactionType.Income, 200m, "USD", 1.0m, "Salary", null, "2026-03");

        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _transactionRepository
            .GetPagedAsync(TestUser.Id, Arg.Any<TransactionPageQuery>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Transaction>)[tx], 1));

        var result = await _handler.Handle(new GetTransactionsQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_WithMonthFilter_PassesMonthToRepository()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _transactionRepository
            .GetPagedAsync(TestUser.Id, Arg.Any<TransactionPageQuery>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Transaction>)[], 0));

        await _handler.Handle(new GetTransactionsQuery(Month: "2026-03"), CancellationToken.None);

        await _transactionRepository.Received(1).GetPagedAsync(
            TestUser.Id,
            Arg.Is<TransactionPageQuery>(q => q.Month == "2026-03"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PageSizeClampsAt100()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _transactionRepository
            .GetPagedAsync(TestUser.Id, Arg.Any<TransactionPageQuery>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Transaction>)[], 0));

        var result = await _handler.Handle(new GetTransactionsQuery(PageSize: 500), CancellationToken.None);

        result.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var act = async () => await _handler.Handle(new GetTransactionsQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_HistoryLimitExceeded_ThrowsPlanLimitExceededException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _limitService
            .EnforceTransactionHistoryAccessAsync(Arg.Any<AppUser>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new PlanLimitExceededException("transaction_history", "History access denied.")));

        var act = async () => await _handler.Handle(new GetTransactionsQuery(Month: "2025-01"), CancellationToken.None);

        await act.Should().ThrowAsync<PlanLimitExceededException>();
    }
}
