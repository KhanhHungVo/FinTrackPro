using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Finance.Commands.UpdateTransaction;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Finance;

public class UpdateTransactionHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITransactionRepository _transactionRepository = Substitute.For<ITransactionRepository>();
    private readonly IExchangeRateService _exchangeRateService = Substitute.For<IExchangeRateService>();
    private readonly ITransactionCategoryRepository _categoryRepository = Substitute.For<ITransactionCategoryRepository>();
    private readonly UpdateTransactionCommandHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");
    private static readonly TransactionCategory SystemCategory =
        TransactionCategory.Create(null, TransactionType.Expense, "food_beverage", "Food & Beverage", "Ăn uống", "🍜", isSystem: true);

    public UpdateTransactionHandlerTests()
    {
        _handler = new UpdateTransactionCommandHandler(
            _context, _currentUser, _userRepository, _transactionRepository, _exchangeRateService, _categoryRepository);

        _currentUser.UserId.Returns(TestUser.Id);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        _exchangeRateService.GetRateToUsdAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, decimal> { ["VND"] = 26_253.7223m });
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesAndSaves()
    {
        var transaction = Transaction.Create(
            TestUser.Id, TransactionType.Expense, 100m, "USD", 1.0m,
            SystemCategory.Slug, "original note", "2026-04", SystemCategory.Id);

        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns(TestUser);
        _transactionRepository.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);
        _categoryRepository.GetByIdAsync(SystemCategory.Id, Arg.Any<CancellationToken>()).Returns(SystemCategory);

        var command = new UpdateTransactionCommand(
            transaction.Id, TransactionType.Expense, 250m, "USD",
            SystemCategory.Slug, "updated note", SystemCategory.Id);

        await _handler.Handle(command, CancellationToken.None);

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        transaction.Amount.Should().Be(250m);
        transaction.RateToUsd.Should().Be(1m);
        transaction.Note.Should().Be("updated note");
    }

    [Fact]
    public async Task Handle_CurrencyChanged_RefreshesRateToUsdAndPreservesSubmittedAmount()
    {
        var transaction = Transaction.Create(
            TestUser.Id, TransactionType.Expense, 75_000m, "VND", 26_253.7223m,
            SystemCategory.Slug, "original note", "2026-04", SystemCategory.Id);

        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns(TestUser);
        _transactionRepository.GetByIdAsync(transaction.Id, Arg.Any<CancellationToken>()).Returns(transaction);
        _categoryRepository.GetByIdAsync(SystemCategory.Id, Arg.Any<CancellationToken>()).Returns(SystemCategory);

        var command = new UpdateTransactionCommand(
            transaction.Id, TransactionType.Expense, 75_000m, "USD",
            SystemCategory.Slug, "updated note", SystemCategory.Id);

        await _handler.Handle(command, CancellationToken.None);

        transaction.Currency.Should().Be("USD");
        transaction.Amount.Should().Be(75_000m);
        transaction.RateToUsd.Should().Be(1m);
    }

    [Fact]
    public async Task Handle_TransactionNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns(TestUser);
        _transactionRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Transaction?)null);

        var command = new UpdateTransactionCommand(
            Guid.NewGuid(), TransactionType.Expense, 100m, "USD", "food", null, null);

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_OtherUsersTransaction_ThrowsAuthorizationException()
    {
        var otherTransaction = Transaction.Create(
            Guid.NewGuid(), TransactionType.Expense, 100m, "USD", 1.0m,
            "food", null, "2026-04");

        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns(TestUser);
        _transactionRepository.GetByIdAsync(otherTransaction.Id, Arg.Any<CancellationToken>()).Returns(otherTransaction);

        var command = new UpdateTransactionCommand(
            otherTransaction.Id, TransactionType.Expense, 200m, "USD", "food", null, null);

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>();
    }
}
