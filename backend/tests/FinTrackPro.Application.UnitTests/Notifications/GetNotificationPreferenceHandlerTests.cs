using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Notifications.Queries.GetNotificationPreference;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Notifications;

public class GetNotificationPreferenceHandlerTests
{
    private readonly INotificationPreferenceRepository _preferenceRepository = Substitute.For<INotificationPreferenceRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly GetNotificationPreferenceQueryHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("kc-test", "test@dev.com", "Test", "local");

    public GetNotificationPreferenceHandlerTests()
    {
        _handler = new GetNotificationPreferenceQueryHandler(_userRepository, _preferenceRepository, _currentUser);
        _currentUser.ExternalUserId.Returns("kc-test");
    }

    [Fact]
    public async Task Handle_ExistingPreference_ReturnsDto()
    {
        var preference = NotificationPreference.CreateTelegram(TestUser.Id, "123456789");

        _userRepository.GetByExternalIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _preferenceRepository.GetByUserAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(preference);

        var result = await _handler.Handle(new GetNotificationPreferenceQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.TelegramChatId.Should().Be("123456789");
    }

    [Fact]
    public async Task Handle_NoPreference_ReturnsNull()
    {
        _userRepository.GetByExternalIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _preferenceRepository.GetByUserAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns((NotificationPreference?)null);

        var result = await _handler.Handle(new GetNotificationPreferenceQuery(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByExternalIdAsync("kc-test", Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var act = async () => await _handler.Handle(new GetNotificationPreferenceQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
