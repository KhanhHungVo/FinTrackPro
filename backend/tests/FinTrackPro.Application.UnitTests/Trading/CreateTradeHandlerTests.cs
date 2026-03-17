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
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IBinanceService _binanceService = Substitute.For<IBinanceService>();
    private readonly CreateTradeCommandHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("kc-test", "test@dev.com", "Test", "local");

    public CreateTradeHandlerTests()
    {
        _handler = new CreateTradeCommandHandler(_context, _currentUser, _userRepository, _binanceService);
        _currentUser.KeycloakUserId.Returns("kc-test");

        var trades = Substitute.For<DbSet<Trade>>();
        _context.Trades.Returns(trades);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNewGuid()
    {
        _userRepository.GetByKeycloakIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _binanceService.IsValidSymbolAsync("BTCUSDT", Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new CreateTradeCommand("BTCUSDT", TradeDirection.Long, 30000m, 35000m, 0.1m, 5m, null);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        _context.Trades.Received(1).Add(Arg.Any<Trade>());
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByKeycloakIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var act = async () => await _handler.Handle(
            new CreateTradeCommand("BTCUSDT", TradeDirection.Long, 30000m, 35000m, 0.1m, 5m, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_InvalidSymbol_ThrowsDomainException()
    {
        _userRepository.GetByKeycloakIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _binanceService.IsValidSymbolAsync("FAKESYMBOL", Arg.Any<CancellationToken>())
            .Returns(false);

        var act = async () => await _handler.Handle(
            new CreateTradeCommand("FAKESYMBOL", TradeDirection.Long, 100m, 110m, 1m, 0m, null),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*not a valid Binance*");
    }
}
