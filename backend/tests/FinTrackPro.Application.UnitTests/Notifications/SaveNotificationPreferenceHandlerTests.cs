using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Application.Notifications.Commands.SaveNotificationPreference;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using FluentAssertions;
using NSubstitute;

namespace FinTrackPro.Application.UnitTests.Notifications;

public class SaveNotificationPreferenceHandlerTests
{
    private readonly IApplicationDbContext _context = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly INotificationPreferenceRepository _preferenceRepository = Substitute.For<INotificationPreferenceRepository>();
    private readonly ISubscriptionLimitService _limitService = Substitute.For<ISubscriptionLimitService>();
    private readonly SaveNotificationPreferenceCommandHandler _handler;

    private static readonly AppUser TestUser = AppUser.Create("test@dev.com", "Test");

    public SaveNotificationPreferenceHandlerTests()
    {
        _handler = new SaveNotificationPreferenceCommandHandler(
            _context, _currentUser, _userRepository, _preferenceRepository, _limitService);
        _currentUser.UserId.Returns(TestUser.Id);
        _context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    [Fact]
    public async Task Handle_NoExistingPreference_CreatesNew()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _preferenceRepository.GetByUserAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns((NotificationPreference?)null);

        await _handler.Handle(
            new SaveNotificationPreferenceCommand("123456789", true), CancellationToken.None);

        _preferenceRepository.Received(1).Add(Arg.Any<NotificationPreference>());
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingPreference_UpdatesInPlace()
    {
        var existing = NotificationPreference.CreateTelegram(TestUser.Id, "111111111");

        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _preferenceRepository.GetByUserAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(existing);

        await _handler.Handle(
            new SaveNotificationPreferenceCommand("999999999", false), CancellationToken.None);

        existing.TelegramChatId.Should().Be("999999999");
        existing.IsEnabled.Should().BeFalse();
        _preferenceRepository.DidNotReceive().Add(Arg.Any<NotificationPreference>());
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns((AppUser?)null);

        var act = async () => await _handler.Handle(
            new SaveNotificationPreferenceCommand("123456789", true), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_IsEnabledTrue_LimitExceeded_ThrowsPlanLimitExceededException()
    {
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _limitService
            .EnforceTelegramAsync(Arg.Any<AppUser>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new PlanLimitExceededException("telegram", "Telegram not available on Free plan.")));

        var act = async () => await _handler.Handle(
            new SaveNotificationPreferenceCommand("123456789", true), CancellationToken.None);

        await act.Should().ThrowAsync<PlanLimitExceededException>();
    }

    [Fact]
    public async Task Handle_IsEnabledFalse_SkipsTelegramLimitCheck()
    {
        var existing = NotificationPreference.CreateTelegram(TestUser.Id, "111111111");
        _userRepository.GetByIdAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(TestUser);
        _preferenceRepository.GetByUserAsync(TestUser.Id, Arg.Any<CancellationToken>())
            .Returns(existing);

        await _handler.Handle(new SaveNotificationPreferenceCommand("111111111", false), CancellationToken.None);

        await _limitService.DidNotReceive().EnforceTelegramAsync(Arg.Any<AppUser>(), Arg.Any<CancellationToken>());
    }
}
