using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Trading.Commands.AddWatchedSymbol;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Trading;

public class AddWatchedSymbolHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IWatchedSymbolRepository _watchedSymbolRepository = Substitute.For<IWatchedSymbolRepository>();
    private readonly ISubscriptionLimitService _limitService = Substitute.For<ISubscriptionLimitService>();
    private readonly AddWatchedSymbolCommandHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");

    public AddWatchedSymbolHandlerTests()
    {
        _handler = new AddWatchedSymbolCommandHandler(
            _context, _currentUser, _userRepository, _watchedSymbolRepository, _limitService);
        _currentUser.UserId.Returns(TestUser.Id);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    [Fact]
    public async Task Handle_ValidSymbol_ReturnsNewGuid()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _watchedSymbolRepository.ExistsAsync(TestUser.Id, "BTCUSDT", Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.Handle(new AddWatchedSymbolCommand("BTCUSDT"), CancellationToken.None);

        result.Should().NotBeEmpty();
        _watchedSymbolRepository.Received(1).Add(Arg.Any<WatchedSymbol>());
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateSymbol_ThrowsDomainException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _watchedSymbolRepository.ExistsAsync(TestUser.Id, "BTCUSDT", Arg.Any<CancellationToken>())
            .Returns(true);

        var act = async () => await _handler.Handle(
            new AddWatchedSymbolCommand("BTCUSDT"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*already in your watchlist*");
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var act = async () => await _handler.Handle(
            new AddWatchedSymbolCommand("BTCUSDT"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_LimitExceeded_ThrowsPlanLimitExceededException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _limitService
            .EnforceWatchlistLimitAsync(Arg.Any<AppUser>(), Arg.Any<IWatchedSymbolRepository>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new PlanLimitExceededException("watchlist", "Watchlist limit reached.")));

        var act = async () => await _handler.Handle(new AddWatchedSymbolCommand("BTCUSDT"), CancellationToken.None);

        await act.Should().ThrowAsync<PlanLimitExceededException>();
    }
}
