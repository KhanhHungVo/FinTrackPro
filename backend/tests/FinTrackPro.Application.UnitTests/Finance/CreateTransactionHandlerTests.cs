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
    private readonly ITransactionCategoryRepository _categoryRepository = Substitute.For<ITransactionCategoryRepository>();
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly ISubscriptionLimitService _limitService = Substitute.For<ISubscriptionLimitService>();
    private readonly IExchangeRateService _exchangeRateService = Substitute.For<IExchangeRateService>();
    private readonly CreateTransactionCommandHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");
    private static readonly TransactionCategory SystemCategory =
        TransactionCategory.Create(null, TransactionType.Expense, "food_beverage", "Food & Beverage", "Ăn uống", "🍜", isSystem: true);

    public CreateTransactionHandlerTests()
    {
        _handler = new CreateTransactionCommandHandler(
            _context, _currentUser, _userRepository, _categoryRepository, _transactionRepository, _limitService, _exchangeRateService);

        _currentUser.UserId.Returns(TestUser.Id);
        _exchangeRateService.GetRateToUsdAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, decimal> { ["EUR"] = 0.92m, ["GBP"] = 0.79m });

        var transactions = Substitute.For<DbSet<Transaction>>();
        _context.Transactions.Returns(transactions);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNewGuid()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _categoryRepository.GetByIdAsync(SystemCategory.Id, Arg.Any<CancellationToken>())
            .Returns(SystemCategory);

        var command = new CreateTransactionCommand(TransactionType.Expense, 100m, "USD", SystemCategory.Id, null, "2026-03");

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

        var command = new CreateTransactionCommand(TransactionType.Expense, 100m, "USD", SystemCategory.Id, null, "2026-03");

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_CategoryNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _categoryRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((TransactionCategory?)null);

        var command = new CreateTransactionCommand(TransactionType.Expense, 100m, "USD", Guid.NewGuid(), null, "2026-03");

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_OtherUsersCategoryId_ThrowsAuthorizationException()
    {
        var otherUserId = Guid.NewGuid();
        var userCategory = TransactionCategory.Create(otherUserId, TransactionType.Expense, "custom", "Custom", "Tùy chỉnh", "📌");

        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _categoryRepository.GetByIdAsync(userCategory.Id, Arg.Any<CancellationToken>())
            .Returns(userCategory);

        var command = new CreateTransactionCommand(TransactionType.Expense, 100m, "USD", userCategory.Id, null, "2026-03");

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>();
    }

    [Fact]
    public async Task Handle_LimitExceeded_ThrowsPlanLimitExceededException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _categoryRepository.GetByIdAsync(SystemCategory.Id, Arg.Any<CancellationToken>())
            .Returns(SystemCategory);
        _context.BeginTransactionAsync(Arg.Any<System.Data.IsolationLevel>(), Arg.Any<CancellationToken>())
            .Returns(Substitute.For<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction>());
        _limitService
            .EnforceMonthlyTransactionLimitAsync(Arg.Any<AppUser>(), Arg.Any<ITransactionRepository>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new PlanLimitExceededException("transaction", "Monthly transaction limit reached.")));

        var act = async () => await _handler.Handle(
            new CreateTransactionCommand(TransactionType.Expense, 100m, "USD", SystemCategory.Id, null, "2026-03"),
            CancellationToken.None);

        await act.Should().ThrowAsync<PlanLimitExceededException>();
    }
}
