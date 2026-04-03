using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.TransactionCategories.Commands.DeleteTransactionCategory;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.TransactionCategories;

public class DeleteTransactionCategoryHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ITransactionCategoryRepository _categoryRepository = Substitute.For<ITransactionCategoryRepository>();
    private readonly DeleteTransactionCategoryCommandHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");

    public DeleteTransactionCategoryHandlerTests()
    {
        _handler = new DeleteTransactionCategoryCommandHandler(_context, _currentUser, _categoryRepository);
        _currentUser.UserId.Returns(TestUser.Id);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    [Fact]
    public async Task Handle_UserCategory_SetsIsActiveFalseAndSaves()
    {
        var category = TransactionCategory.Create(TestUser.Id, TransactionType.Expense, "pet_care", "Pet Care", "Thú cưng", "🐶");
        _categoryRepository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);

        await _handler.Handle(new DeleteTransactionCategoryCommand(category.Id), CancellationToken.None);

        category.IsActive.Should().BeFalse();
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SystemCategory_ThrowsAuthorizationException()
    {
        var systemCategory = TransactionCategory.Create(null, TransactionType.Expense, "food", "Food", "Ăn", "🍜", isSystem: true);
        _currentUser.UserId.Returns(Guid.Empty);
        _categoryRepository.GetByIdAsync(systemCategory.Id, Arg.Any<CancellationToken>()).Returns(systemCategory);

        var act = async () => await _handler.Handle(new DeleteTransactionCategoryCommand(systemCategory.Id), CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>();
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
    {
        _categoryRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((TransactionCategory?)null);

        var act = async () => await _handler.Handle(new DeleteTransactionCategoryCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
