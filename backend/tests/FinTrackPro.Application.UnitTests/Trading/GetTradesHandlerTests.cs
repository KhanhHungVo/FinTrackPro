using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Trading.Queries.GetTrades;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Trading;

public class GetTradesHandlerTests
{
    private readonly ITradeRepository _tradeRepository = Substitute.For<ITradeRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly GetTradesQueryHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");

    public GetTradesHandlerTests()
    {
        _handler = new GetTradesQueryHandler(_userRepository, _tradeRepository, _currentUser);
        _currentUser.UserId.Returns(TestUser.Id);
    }

    [Fact]
    public async Task Handle_ValidUser_ReturnsTrades()
    {
        var trades = new List<Trade>
        {
            Trade.Create(TestUser.Id, "BTCUSDT", TradeDirection.Long, 30000m, 35000m, 0.1m, 5m, null),
            Trade.Create(TestUser.Id, "ETHUSDT", TradeDirection.Short, 2000m, 1800m, 1m, 2m, null),
        };

        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _tradeRepository.GetByUserAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(trades);

        var result = await _handler.Handle(new GetTradesQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NoTrades_ReturnsEmpty()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _tradeRepository.GetByUserAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(new List<Trade>());

        var result = await _handler.Handle(new GetTradesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var act = async () => await _handler.Handle(new GetTradesQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
