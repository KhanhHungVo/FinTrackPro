using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Trading.Commands.RemoveWatchedSymbol;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Trading;

public class RemoveWatchedSymbolHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly IWatchedSymbolRepository _watchedSymbolRepository = Substitute.For<IWatchedSymbolRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly RemoveWatchedSymbolCommandHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("kc-test", "test@dev.com", "Test", "local");

    public RemoveWatchedSymbolHandlerTests()
    {
        _handler = new RemoveWatchedSymbolCommandHandler(
            _context, _watchedSymbolRepository, _currentUser, _userRepository);
        _currentUser.ExternalUserId.Returns("kc-test");
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    [Fact]
    public async Task Handle_OwnedSymbol_RemovesSuccessfully()
    {
        var symbol = WatchedSymbol.Create(TestUser.Id, "BTCUSDT");

        _userRepository.GetByExternalIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _watchedSymbolRepository.GetByIdAsync(symbol.Id, Arg.Any<CancellationToken>())
            .Returns(symbol);

        await _handler.Handle(new RemoveWatchedSymbolCommand(symbol.Id), CancellationToken.None);

        _watchedSymbolRepository.Received(1).Remove(symbol);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SymbolNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByExternalIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _watchedSymbolRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((WatchedSymbol?)null);

        var act = async () => await _handler.Handle(
            new RemoveWatchedSymbolCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_SymbolOwnedByOtherUser_ThrowsDomainException()
    {
        var otherUser = AppUser.Create("kc-other", "other@dev.com", "Other", "local");
        var symbol = WatchedSymbol.Create(otherUser.Id, "BTCUSDT");

        _userRepository.GetByExternalIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _watchedSymbolRepository.GetByIdAsync(symbol.Id, Arg.Any<CancellationToken>())
            .Returns(symbol);

        var act = async () => await _handler.Handle(
            new RemoveWatchedSymbolCommand(symbol.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*not authorized*");
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByExternalIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var act = async () => await _handler.Handle(
            new RemoveWatchedSymbolCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
