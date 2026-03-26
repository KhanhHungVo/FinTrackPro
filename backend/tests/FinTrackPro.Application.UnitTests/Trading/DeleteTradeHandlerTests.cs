using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Trading.Commands.DeleteTrade;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Trading;

public class DeleteTradeHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly ITradeRepository _tradeRepository = Substitute.For<ITradeRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly DeleteTradeCommandHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");

    public DeleteTradeHandlerTests()
    {
        _handler = new DeleteTradeCommandHandler(_context, _tradeRepository, _currentUser, _userRepository);
        _currentUser.UserId.Returns(TestUser.Id);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    [Fact]
    public async Task Handle_OwnedTrade_DeletesSuccessfully()
    {
        var trade = Trade.Create(TestUser.Id, "BTCUSDT", TradeDirection.Long, 30000m, 35000m, 0.1m, 5m, null);

        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _tradeRepository.GetByIdAsync(trade.Id, Arg.Any<CancellationToken>())
            .Returns(trade);

        await _handler.Handle(new DeleteTradeCommand(trade.Id), CancellationToken.None);

        _tradeRepository.Received(1).Remove(trade);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TradeNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _tradeRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Trade?)null);

        var act = async () => await _handler.Handle(
            new DeleteTradeCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_TradeOwnedByOtherUser_ThrowsDomainException()
    {
        var otherUser = AppUser.Create("other@dev.com", "Other");
        var trade = Trade.Create(otherUser.Id, "BTCUSDT", TradeDirection.Long, 30000m, 35000m, 0.1m, 5m, null);

        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _tradeRepository.GetByIdAsync(trade.Id, Arg.Any<CancellationToken>())
            .Returns(trade);

        var act = async () => await _handler.Handle(
            new DeleteTradeCommand(trade.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*not authorized*");
    }
}
