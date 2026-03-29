using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Finance.Commands.UpdateBudget;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Finance;

public class UpdateBudgetHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly IBudgetRepository _budgetRepository = Substitute.For<IBudgetRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly UpdateBudgetCommandHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");

    public UpdateBudgetHandlerTests()
    {
        _handler = new UpdateBudgetCommandHandler(_context, _budgetRepository, _currentUser, _userRepository);
        _currentUser.UserId.Returns(TestUser.Id);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    [Fact]
    public async Task Handle_OwnedBudget_UpdatesLimitAndSaves()
    {
        var budget = Budget.Create(TestUser.Id, "Food", 500m, "USD", 1.0m, "2026-03");

        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _budgetRepository.GetByIdAsync(budget.Id, Arg.Any<CancellationToken>())
            .Returns(budget);

        await _handler.Handle(new UpdateBudgetCommand(budget.Id, 1000m), CancellationToken.None);

        budget.LimitAmount.Should().Be(1000m);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BudgetNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _budgetRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Budget?)null);

        var act = async () => await _handler.Handle(
            new UpdateBudgetCommand(Guid.NewGuid(), 1000m), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_BudgetOwnedByOtherUser_ThrowsDomainException()
    {
        var otherUser = AppUser.Create("other@dev.com", "Other");
        var budget = Budget.Create(otherUser.Id, "Food", 500m, "USD", 1.0m, "2026-03");

        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _budgetRepository.GetByIdAsync(budget.Id, Arg.Any<CancellationToken>())
            .Returns(budget);

        var act = async () => await _handler.Handle(
            new UpdateBudgetCommand(budget.Id, 1000m), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*not authorized*");
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var act = async () => await _handler.Handle(
            new UpdateBudgetCommand(Guid.NewGuid(), 1000m), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
