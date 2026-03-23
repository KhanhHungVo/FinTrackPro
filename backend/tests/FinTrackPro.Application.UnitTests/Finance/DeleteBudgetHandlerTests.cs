using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Finance.Commands.DeleteBudget;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Finance;

public class DeleteBudgetHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly IBudgetRepository _budgetRepository = Substitute.For<IBudgetRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly DeleteBudgetCommandHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("kc-test", "test@dev.com", "Test", "local");

    public DeleteBudgetHandlerTests()
    {
        _handler = new DeleteBudgetCommandHandler(_context, _budgetRepository, _currentUser, _userRepository);
        _currentUser.ExternalUserId.Returns("kc-test");
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    [Fact]
    public async Task Handle_OwnedBudget_DeletesSuccessfully()
    {
        var budget = Budget.Create(TestUser.Id, "Food", 500m, "2026-03");

        _userRepository.GetByExternalIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _budgetRepository.GetByIdAsync(budget.Id, Arg.Any<CancellationToken>())
            .Returns(budget);

        await _handler.Handle(new DeleteBudgetCommand(budget.Id), CancellationToken.None);

        _budgetRepository.Received(1).Remove(budget);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BudgetNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByExternalIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _budgetRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Budget?)null);

        var act = async () => await _handler.Handle(
            new DeleteBudgetCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_BudgetOwnedByOtherUser_ThrowsDomainException()
    {
        var otherUser = AppUser.Create("kc-other", "other@dev.com", "Other", "local");
        var budget = Budget.Create(otherUser.Id, "Food", 500m, "2026-03");

        _userRepository.GetByExternalIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _budgetRepository.GetByIdAsync(budget.Id, Arg.Any<CancellationToken>())
            .Returns(budget);

        var act = async () => await _handler.Handle(
            new DeleteBudgetCommand(budget.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*not authorized*");
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByExternalIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var act = async () => await _handler.Handle(
            new DeleteBudgetCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
