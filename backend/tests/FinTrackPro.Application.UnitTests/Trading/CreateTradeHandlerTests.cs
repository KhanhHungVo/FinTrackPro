using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Trading.Commands.CreateTrade;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Trading;

public class CreateTradeHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ITradeRepository _tradeRepository = Substitute.For<ITradeRepository>();
    private readonly ISubscriptionLimitService _limitService = Substitute.For<ISubscriptionLimitService>();
    private readonly IExchangeRateService _exchangeRateService = Substitute.For<IExchangeRateService>();
    private readonly CreateTradeCommandHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");

    public CreateTradeHandlerTests()
    {
        _handler = new CreateTradeCommandHandler(_context, _currentUser, _userRepository, _tradeRepository, _limitService, _exchangeRateService);
        _currentUser.UserId.Returns(TestUser.Id);
        _exchangeRateService.GetRateToUsdAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, decimal> { ["EUR"] = 0.92m, ["GBP"] = 0.79m });

        var trades = Substitute.For<DbSet<Trade>>();
        _context.Trades.Returns(trades);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    [Fact]
    public async Task Handle_ValidClosedCommand_ReturnsNewGuid()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);

        var command = new CreateTradeCommand("BTCUSDT", TradeDirection.Long, TradeStatus.Closed,
            30000m, 35000m, null, 0.1m, 5m, "USD", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _context.Trades.Received(1).Add(Arg.Any<Trade>());
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidOpenCommand_ReturnsNewGuid()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);

        var command = new CreateTradeCommand("BTCUSDT", TradeDirection.Long, TradeStatus.Open,
            30000m, null, 32000m, 0.1m, 0m, "USD", null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var act = async () => await _handler.Handle(
            new CreateTradeCommand("BTCUSDT", TradeDirection.Long, TradeStatus.Closed,
                30000m, 35000m, null, 0.1m, 5m, "USD", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_LimitExceeded_ThrowsPlanLimitExceededException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _limitService
            .EnforceTradeLimitAsync(Arg.Any<AppUser>(), Arg.Any<ITradeRepository>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new PlanLimitExceededException("trade", "Trade limit reached.")));

        var act = async () => await _handler.Handle(
            new CreateTradeCommand("BTCUSDT", TradeDirection.Long, TradeStatus.Closed,
                30000m, 35000m, null, 0.1m, 5m, "USD", null),
            CancellationToken.None);

        await act.Should().ThrowAsync<PlanLimitExceededException>();
    }
}
