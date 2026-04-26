using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Trading.Queries.GetWatchedSymbols;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinTrackPro.Application.UnitTests.Trading;

public class GetWatchedSymbolsHandlerTests
{
    private readonly IWatchedSymbolRepository _watchedSymbolRepository = Substitute.For<IWatchedSymbolRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ISubscriptionLimitService _limitService = Substitute.For<ISubscriptionLimitService>();
    private readonly GetWatchedSymbolsQueryHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");

    public GetWatchedSymbolsHandlerTests()
    {
        _handler = new GetWatchedSymbolsQueryHandler(_userRepository, _watchedSymbolRepository, _currentUser, _limitService);
        _currentUser.UserId.Returns(TestUser.Id);
    }

    [Fact]
    public async Task Handle_ValidUser_ReturnsWatchedSymbols()
    {
        var symbols = new List<WatchedSymbol>
        {
            WatchedSymbol.Create(TestUser.Id, "BTCUSDT"),
            WatchedSymbol.Create(TestUser.Id, "ETHUSDT"),
        };

        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _watchedSymbolRepository.GetByUserAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(symbols);

        var result = await _handler.Handle(new GetWatchedSymbolsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_FreeUser_CallsEnforceWatchlistReadAccess_AndThrows()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _limitService.EnforceWatchlistReadAccessAsync(TestUser, Arg.Any<CancellationToken>())
            .ThrowsAsync(new PlanLimitExceededException("watchlist", "Pro only"));

        var act = async () => await _handler.Handle(new GetWatchedSymbolsQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<PlanLimitExceededException>()
            .Where(e => e.Feature == "watchlist");
        await _limitService.Received(1).EnforceWatchlistReadAccessAsync(TestUser, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProUser_PassesThroughToRepository()
    {
        var proUser = AppUser.Create("pro@dev.com", "Pro");
        proUser.ActivateSubscription("sub_123", DateTime.UtcNow.AddMonths(1));
        _currentUser.UserId.Returns(proUser.Id);
        _userRepository.GetByIdAsync(proUser.Id, Arg.Any<CancellationToken>())
            .Returns(proUser);
        _watchedSymbolRepository.GetByUserAsync(proUser.Id, Arg.Any<CancellationToken>())
            .Returns(new List<WatchedSymbol>());

        var result = await _handler.Handle(new GetWatchedSymbolsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
        await _limitService.Received(1).EnforceWatchlistReadAccessAsync(proUser, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyWatchlist_ReturnsEmpty()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _watchedSymbolRepository.GetByUserAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(new List<WatchedSymbol>());

        var result = await _handler.Handle(new GetWatchedSymbolsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var act = async () => await _handler.Handle(new GetWatchedSymbolsQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
