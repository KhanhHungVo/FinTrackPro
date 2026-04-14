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
    public async Task Handle_ValidUser_ReturnsPagedTrades()
    {
        var trades = new List<Trade>
        {
            Trade.Create(TestUser.Id, "BTCUSDT", TradeDirection.Long, TradeStatus.Closed, 30000m, 35000m, null, 0.1m, 5m, "USD", 1.0m, null),
            Trade.Create(TestUser.Id, "ETHUSDT", TradeDirection.Short, TradeStatus.Closed, 2000m, 1800m, null, 1m, 2m, "USD", 1.0m, null),
        };

        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _tradeRepository
            .GetPagedAsync(TestUser.Id, Arg.Any<TradePageQuery>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Trade>)trades, trades.Count));

        var result = await _handler.Handle(new GetTradesQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_NoTrades_ReturnsEmptyPagedResult()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _tradeRepository
            .GetPagedAsync(TestUser.Id, Arg.Any<TradePageQuery>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Trade>)[], 0));

        var result = await _handler.Handle(new GetTradesQuery(), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_PassesStatusToRepository()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _tradeRepository
            .GetPagedAsync(TestUser.Id, Arg.Any<TradePageQuery>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Trade>)[], 0));

        await _handler.Handle(new GetTradesQuery { Status = "Open" }, CancellationToken.None);

        await _tradeRepository.Received(1).GetPagedAsync(
            TestUser.Id,
            Arg.Is<TradePageQuery>(q => q.Status == "Open"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PageSizeClampsAt100()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _tradeRepository
            .GetPagedAsync(TestUser.Id, Arg.Any<TradePageQuery>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<Trade>)[], 0));

        var result = await _handler.Handle(new GetTradesQuery { PageSize = 500 }, CancellationToken.None);

        result.PageSize.Should().Be(100);
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
