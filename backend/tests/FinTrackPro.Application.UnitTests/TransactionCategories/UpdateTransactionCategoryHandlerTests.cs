using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.TransactionCategories.Commands.UpdateTransactionCategory;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.TransactionCategories;

public class UpdateTransactionCategoryHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ITransactionCategoryRepository _categoryRepository = Substitute.For<ITransactionCategoryRepository>();
    private readonly UpdateTransactionCategoryCommandHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");

    public UpdateTransactionCategoryHandlerTests()
    {
        _handler = new UpdateTransactionCategoryCommandHandler(_context, _currentUser, _categoryRepository);
        _currentUser.UserId.Returns(TestUser.Id);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    [Fact]
    public async Task Handle_UserCategory_UpdatesAndSaves()
    {
        var category = TransactionCategory.Create(TestUser.Id, TransactionType.Expense, "pet_care", "Pet Care", "Thú cưng", "🐶");
        _categoryRepository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);

        await _handler.Handle(new UpdateTransactionCategoryCommand(category.Id, "Pets", "Thú nuôi", "🐾"), CancellationToken.None);

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        category.LabelEn.Should().Be("Pets");
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
    {
        _categoryRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((TransactionCategory?)null);

        var act = async () => await _handler.Handle(new UpdateTransactionCategoryCommand(Guid.NewGuid(), "Pets", "Thú nuôi", "🐾"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_OtherUsersCategory_ThrowsAuthorizationException()
    {
        var otherCategory = TransactionCategory.Create(Guid.NewGuid(), TransactionType.Expense, "pet_care", "Pet Care", "Thú cưng", "🐶");
        _categoryRepository.GetByIdAsync(otherCategory.Id, Arg.Any<CancellationToken>()).Returns(otherCategory);

        var act = async () => await _handler.Handle(new UpdateTransactionCategoryCommand(otherCategory.Id, "Pets", "Thú nuôi", "🐾"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>();
    }

    [Fact]
    public async Task Handle_SystemCategory_ThrowsAuthorizationException()
    {
        var systemCategory = TransactionCategory.Create(null, TransactionType.Expense, "food", "Food", "Ăn", "🍜", isSystem: true);
        // UserId is null → ownership check passes, but UpdateLabels() throws
        _currentUser.UserId.Returns((Guid?)null ?? Guid.Empty);
        _categoryRepository.GetByIdAsync(systemCategory.Id, Arg.Any<CancellationToken>()).Returns(systemCategory);

        var act = async () => await _handler.Handle(new UpdateTransactionCategoryCommand(systemCategory.Id, "Food2", "Ăn2", "🥗"), CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>();
    }
}
