using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.TransactionCategories.Queries.GetTransactionCategories;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.TransactionCategories;

public class GetTransactionCategoriesHandlerTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITransactionCategoryRepository _categoryRepository = Substitute.For<ITransactionCategoryRepository>();
    private readonly GetTransactionCategoriesQueryHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");

    public GetTransactionCategoriesHandlerTests()
    {
        _handler = new GetTransactionCategoriesQueryHandler(_currentUser, _userRepository, _categoryRepository);
        _currentUser.UserId.Returns(TestUser.Id);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsMappedDtos()
    {
        var category = TransactionCategory.Create(null, TransactionType.Expense, "food_beverage", "Food & Beverage", "Ăn uống", "🍜", isSystem: true);
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns(TestUser);
        _categoryRepository.GetByUserAsync(TestUser.Id, null, Arg.Any<CancellationToken>())
            .Returns([category]);

        var result = await _handler.Handle(new GetTransactionCategoriesQuery(), CancellationToken.None);

        var dtos = result.ToList();
        dtos.Should().HaveCount(1);
        dtos[0].Slug.Should().Be("food_beverage");
        dtos[0].LabelEn.Should().Be("Food & Beverage");
        dtos[0].IsSystem.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithTypeFilter_PassesTypeToRepository()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns(TestUser);
        _categoryRepository.GetByUserAsync(TestUser.Id, TransactionType.Expense, Arg.Any<CancellationToken>())
            .Returns([]);

        await _handler.Handle(new GetTransactionCategoriesQuery(TransactionType.Expense), CancellationToken.None);

        await _categoryRepository.Received(1).GetByUserAsync(TestUser.Id, TransactionType.Expense, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns((AppUser?)null);

        var act = async () => await _handler.Handle(new GetTransactionCategoriesQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
