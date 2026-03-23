using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Signals.Queries.GetSignals;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Enums;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Signals;

public class GetSignalsHandlerTests
{
    private readonly ISignalRepository _signalRepository = Substitute.For<ISignalRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly GetSignalsQueryHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("kc-test", "test@dev.com", "Test", "local");

    public GetSignalsHandlerTests()
    {
        _handler = new GetSignalsQueryHandler(_userRepository, _signalRepository, _currentUser);
        _currentUser.ExternalUserId.Returns("kc-test");
    }

    [Fact]
    public async Task Handle_ValidUser_ReturnsSignals()
    {
        var signals = new List<Signal>
        {
            Signal.Create(TestUser.Id, "BTCUSDT", SignalType.RsiOversold, "RSI 28 — oversold", 28m, "1W"),
            Signal.Create(TestUser.Id, "ETHUSDT", SignalType.VolumeSpike, "Volume spike detected", 2.5m, "1D"),
        };

        _userRepository.GetByExternalIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _signalRepository.GetLatestByUserAsync(TestUser.Id, 20, Arg.Any<CancellationToken>())
            .Returns(signals);

        var result = await _handler.Handle(new GetSignalsQuery(20), CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NoSignals_ReturnsEmpty()
    {
        _userRepository.GetByExternalIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _signalRepository.GetLatestByUserAsync(TestUser.Id, 20, Arg.Any<CancellationToken>())
            .Returns(new List<Signal>());

        var result = await _handler.Handle(new GetSignalsQuery(20), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CustomCount_PassesCountToRepository()
    {
        _userRepository.GetByExternalIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _signalRepository.GetLatestByUserAsync(TestUser.Id, 5, Arg.Any<CancellationToken>())
            .Returns(new List<Signal>());

        await _handler.Handle(new GetSignalsQuery(5), CancellationToken.None);

        await _signalRepository.Received(1).GetLatestByUserAsync(TestUser.Id, 5, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByExternalIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var act = async () => await _handler.Handle(new GetSignalsQuery(20), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
