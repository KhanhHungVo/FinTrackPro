using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Trading.Commands.ClosePosition;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Trading;

public class ClosePositionHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITradeRepository _tradeRepository = Substitute.For<ITradeRepository>();
    private readonly ClosePositionCommandHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");

    public ClosePositionHandlerTests()
    {
        _handler = new ClosePositionCommandHandler(_context, _tradeRepository, _currentUser, _userRepository);
        _currentUser.UserId.Returns(TestUser.Id);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    private static Trade OpenTrade(Guid userId) =>
        Trade.Create(userId, "BTCUSDT", TradeDirection.Long, TradeStatus.Open,
            30000m, null, 32000m, 0.1m, 0m, "USD", 1.0m, null);

    [Fact]
    public async Task Handle_ValidCommand_ClosesTradeAndReturnsDto()
    {
        var trade = OpenTrade(TestUser.Id);
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns(TestUser);
        _tradeRepository.GetByIdAsync(trade.Id, Arg.Any<CancellationToken>()).Returns(trade);

        var command = new ClosePositionCommand(trade.Id, 35000m, 5m);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Status.Should().Be(TradeStatus.Closed);
        result.ExitPrice.Should().Be(35000m);
        result.CurrentPrice.Should().BeNull();
        result.Fees.Should().Be(5m);
        // P&L = (35000 - 30000) * 0.1 - 5 = 495
        result.Result.Should().Be(495m);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyClosed_ThrowsConflictException()
    {
        var trade = Trade.Create(TestUser.Id, "BTCUSDT", TradeDirection.Long, TradeStatus.Closed,
            30000m, 35000m, null, 0.1m, 5m, "USD", 1.0m, null);
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns(TestUser);
        _tradeRepository.GetByIdAsync(trade.Id, Arg.Any<CancellationToken>()).Returns(trade);

        var act = async () => await _handler.Handle(
            new ClosePositionCommand(trade.Id, 36000m, 5m), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_TradeNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns(TestUser);
        _tradeRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Trade?)null);

        var act = async () => await _handler.Handle(
            new ClosePositionCommand(Guid.NewGuid(), 35000m, 0m), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_TradeOwnedByOtherUser_ThrowsAuthorizationException()
    {
        var trade = OpenTrade(Guid.NewGuid());
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns(TestUser);
        _tradeRepository.GetByIdAsync(trade.Id, Arg.Any<CancellationToken>()).Returns(trade);

        var act = async () => await _handler.Handle(
            new ClosePositionCommand(trade.Id, 35000m, 0m), CancellationToken.None);

        await act.Should().ThrowAsync<AuthorizationException>();
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>()).Returns((AppUser?)null);

        var act = async () => await _handler.Handle(
            new ClosePositionCommand(Guid.NewGuid(), 35000m, 0m), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
