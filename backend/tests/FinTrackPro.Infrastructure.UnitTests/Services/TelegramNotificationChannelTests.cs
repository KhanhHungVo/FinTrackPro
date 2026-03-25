using FinTrackPro.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Telegram.Bot;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;

namespace FinTrackPro.Infrastructure.UnitTests.Services;

public class TelegramNotificationChannelTests
{
    private readonly ITelegramBotClient _botClient = Substitute.For<ITelegramBotClient>();
    private readonly TelegramNotificationChannel _channel;

    public TelegramNotificationChannelTests()
    {
        _channel = new TelegramNotificationChannel(
            _botClient, NullLogger<TelegramNotificationChannel>.Instance);
    }

    [Fact]
    public async Task SendAsync_WhenCanceled_RethrowsOperationCanceledException()
    {
        _botClient
            .SendRequest(Arg.Any<IRequest<Message>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var act = async () => await _channel.SendAsync("123456789", "title", "body", CancellationToken.None);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SendAsync_WhenBotThrowsNonCancellation_LogsWarningAndSuppresses()
    {
        _botClient
            .SendRequest(Arg.Any<IRequest<Message>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("bot API error"));

        var act = async () => await _channel.SendAsync("123456789", "title", "body", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
