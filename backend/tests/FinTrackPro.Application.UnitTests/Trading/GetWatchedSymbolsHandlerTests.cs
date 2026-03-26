using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Trading.Queries.GetWatchedSymbols;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Trading;

public class GetWatchedSymbolsHandlerTests
{
    private readonly IWatchedSymbolRepository _watchedSymbolRepository = Substitute.For<IWatchedSymbolRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly GetWatchedSymbolsQueryHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");

    public GetWatchedSymbolsHandlerTests()
    {
        _handler = new GetWatchedSymbolsQueryHandler(_userRepository, _watchedSymbolRepository, _currentUser);
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
