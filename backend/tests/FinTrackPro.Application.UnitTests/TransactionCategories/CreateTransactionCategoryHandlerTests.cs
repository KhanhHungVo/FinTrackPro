using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.TransactionCategories.Commands.CreateTransactionCategory;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.TransactionCategories;

public class CreateTransactionCategoryHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITransactionCategoryRepository _categoryRepository = Substitute.For<ITransactionCategoryRepository>();
    private readonly CreateTransactionCategoryCommandHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");

    public CreateTransactionCategoryHandlerTests()
    {
        _handler = new CreateTransactionCategoryCommandHandler(_context, _currentUser, _userRepository, _categoryRepository);
        _currentUser.UserId.Returns(TestUser.Id);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNewGuid()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns(TestUser);
        _categoryRepository.SlugExistsForUserAsync(TestUser.Id, "pet_care", Arg.Any<CancellationToken>()).Returns(false);

        var command = new CreateTransactionCategoryCommand(TransactionType.Expense, "pet_care", "Pet Care", "Thú cưng", "🐶");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _categoryRepository.Received(1).Add(Arg.Any<TransactionCategory>());
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateSlug_ThrowsConflictException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns(TestUser);
        _categoryRepository.SlugExistsForUserAsync(TestUser.Id, "pet_care", Arg.Any<CancellationToken>()).Returns(true);

        var command = new CreateTransactionCategoryCommand(TransactionType.Expense, "pet_care", "Pet Care", "Thú cưng", "🐶");

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns((AppUser?)null);

        var command = new CreateTransactionCategoryCommand(TransactionType.Expense, "pet_care", "Pet Care", "Thú cưng", "🐶");

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
