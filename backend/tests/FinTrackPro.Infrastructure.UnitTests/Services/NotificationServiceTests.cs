using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Repositories;
using FinTrackPro.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinTrackPro.Infrastructure.UnitTests.Services;

public class NotificationServiceTests
{
    private readonly INotificationPreferenceRepository _preferenceRepository =
        Substitute.For<INotificationPreferenceRepository>();
    private readonly INotificationChannel _channel =
        Substitute.For<INotificationChannel>();
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _service = new NotificationService(
            _preferenceRepository, _channel, NullLogger<NotificationService>.Instance);
    }

    [Fact]
    public async Task NotifyAsync_WhenCanceled_RethrowsOperationCanceledException()
    {
        var userId = Guid.NewGuid();
        var pref = NotificationPreference.CreateTelegram(userId, "123456789");
        _preferenceRepository.GetByUserAsync(userId, Arg.Any<CancellationToken>()).Returns(pref);
        _channel
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var act = async () => await _service.NotifyAsync(userId, "title", "body", CancellationToken.None);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task NotifyAsync_WhenChannelThrowsNonCancellation_LogsErrorAndSuppresses()
    {
        var userId = Guid.NewGuid();
        var pref = NotificationPreference.CreateTelegram(userId, "123456789");
        _preferenceRepository.GetByUserAsync(userId, Arg.Any<CancellationToken>()).Returns(pref);
        _channel
            .SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("network error"));

        var act = async () => await _service.NotifyAsync(userId, "title", "body", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
