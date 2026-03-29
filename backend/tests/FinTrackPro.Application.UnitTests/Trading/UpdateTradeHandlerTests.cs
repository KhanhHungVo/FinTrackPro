using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Trading.Commands.UpdateTrade;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Trading;

public class UpdateTradeHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITradeRepository _tradeRepository = Substitute.For<ITradeRepository>();
    private readonly IExchangeRateService _exchangeRateService = Substitute.For<IExchangeRateService>();
    private readonly UpdateTradeCommandHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");

    private static Trade CreateTestTrade(Guid userId) =>
        Trade.Create(userId, "BTCUSDT", TradeDirection.Long, 30000m, 35000m, 0.1m, 5m, "USD", 1.0m, null);

    public UpdateTradeHandlerTests()
    {
        _handler = new UpdateTradeCommandHandler(_context, _tradeRepository, _currentUser, _userRepository, _exchangeRateService);
        _currentUser.UserId.Returns(TestUser.Id);
        _exchangeRateService.GetRateToUsdAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, decimal> { ["EUR"] = 0.92m, ["GBP"] = 0.79m });
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesTradeAndReturnsDto()
    {
        var trade = CreateTestTrade(TestUser.Id);
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns(TestUser);
        _tradeRepository.GetByIdAsync(trade.Id, Arg.Any<CancellationToken>()).Returns(trade);

        var command = new UpdateTradeCommand(
            trade.Id, "ETHUSDT", TradeDirection.Short,
            2000m, 2500m, 1m, 10m, "USD", "Updated note\nSecond line");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Symbol.Should().Be("ETHUSDT");
        result.Direction.Should().Be(TradeDirection.Short);
        result.EntryPrice.Should().Be(2000m);
        result.ExitPrice.Should().Be(2500m);
        result.PositionSize.Should().Be(1m);
        result.Fees.Should().Be(10m);
        result.Notes.Should().Be("Updated note\nSecond line");
        // P&L = (2500 - 2000) * 1 - 10 = 490
        result.Result.Should().Be(490m);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns((AppUser?)null);

        var act = async () => await _handler.Handle(
            new UpdateTradeCommand(Guid.NewGuid(), "BTCUSDT", TradeDirection.Long, 30000m, 35000m, 0.1m, 5m, "USD", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_TradeNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns(TestUser);
        _tradeRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Trade?)null);

        var act = async () => await _handler.Handle(
            new UpdateTradeCommand(Guid.NewGuid(), "BTCUSDT", TradeDirection.Long, 30000m, 35000m, 0.1m, 5m, "USD", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_TradeOwnedByAnotherUser_ThrowsAuthorizationException()
    {
        var otherUserId = Guid.NewGuid();
        var trade = CreateTestTrade(otherUserId);
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns(TestUser);
        _tradeRepository.GetByIdAsync(trade.Id, Arg.Any<CancellationToken>()).Returns(trade);

        var act = async () => await _handler.Handle(
            new UpdateTradeCommand(trade.Id, "BTCUSDT", TradeDirection.Long, 30000m, 35000m, 0.1m, 5m, "USD", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>();
    }

    [Fact]
    public async Task Handle_DomainRuleViolation_ThrowsDomainException()
    {
        var trade = CreateTestTrade(TestUser.Id);
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns(TestUser);
        _tradeRepository.GetByIdAsync(trade.Id, Arg.Any<CancellationToken>()).Returns(trade);

        var act = async () => await _handler.Handle(
            new UpdateTradeCommand(trade.Id, "BTCUSDT", TradeDirection.Long, 30000m, 0m, 0.1m, 5m, "USD", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Exit price must be greater than zero.");
    }
}
