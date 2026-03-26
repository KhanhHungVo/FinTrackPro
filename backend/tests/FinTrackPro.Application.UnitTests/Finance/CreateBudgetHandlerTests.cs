using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Finance.Commands.CreateBudget;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Finance;

public class CreateBudgetHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly CreateBudgetCommandHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");

    public CreateBudgetHandlerTests()
    {
        _handler = new CreateBudgetCommandHandler(_context, _currentUser, _userRepository);
        _currentUser.UserId.Returns(TestUser.Id);

        var budgets = Substitute.For<DbSet<Budget>>();
        _context.Budgets.Returns(budgets);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNewGuid()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);

        var command = new CreateBudgetCommand("Food", 500m, "2026-03");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _context.Budgets.Received(1).Add(Arg.Any<Budget>());
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var act = async () => await _handler.Handle(new CreateBudgetCommand("Food", 500m, "2026-03"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
