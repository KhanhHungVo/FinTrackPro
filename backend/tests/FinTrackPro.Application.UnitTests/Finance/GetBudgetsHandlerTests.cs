using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Finance.Queries.GetBudgets;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Finance;

public class GetBudgetsHandlerTests
{
    private readonly IBudgetRepository _budgetRepository = Substitute.For<IBudgetRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly GetBudgetsQueryHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("kc-test", "test@dev.com", "Test", "local");

    public GetBudgetsHandlerTests()
    {
        _handler = new GetBudgetsQueryHandler(_userRepository, _budgetRepository, _currentUser);
        _currentUser.ExternalUserId.Returns("kc-test");
    }

    [Fact]
    public async Task Handle_ValidUser_ReturnsBudgetsForMonth()
    {
        var budgets = new List<Budget>
        {
            Budget.Create(TestUser.Id, "Food", 500m, "2026-03"),
            Budget.Create(TestUser.Id, "Transport", 200m, "2026-03"),
        };

        _userRepository.GetByExternalIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _budgetRepository.GetByUserAndMonthAsync(TestUser.Id, "2026-03", Arg.Any<CancellationToken>())
            .Returns(budgets);

        var result = await _handler.Handle(new GetBudgetsQuery("2026-03"), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NoBudgetsForMonth_ReturnsEmpty()
    {
        _userRepository.GetByExternalIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _budgetRepository.GetByUserAndMonthAsync(TestUser.Id, "2026-03", Arg.Any<CancellationToken>())
            .Returns(new List<Budget>());

        var result = await _handler.Handle(new GetBudgetsQuery("2026-03"), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByExternalIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var act = async () => await _handler.Handle(new GetBudgetsQuery("2026-03"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
