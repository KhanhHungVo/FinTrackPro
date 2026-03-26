using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Finance.Commands.CreateTransaction;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Finance;

public class CreateTransactionHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly CreateTransactionCommandHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");

    public CreateTransactionHandlerTests()
    {
        _handler = new CreateTransactionCommandHandler(_context, _currentUser, _userRepository);

        _currentUser.UserId.Returns(TestUser.Id);

        var transactions = Substitute.For<DbSet<Transaction>>();
        _context.Transactions.Returns(transactions);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNewGuid()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);

        var command = new CreateTransactionCommand(TransactionType.Expense, 100m, "Food", null, "2026-03");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _context.Transactions.Received(1).Add(Arg.Any<Transaction>());
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var command = new CreateTransactionCommand(TransactionType.Expense, 100m, "Food", null, "2026-03");

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
