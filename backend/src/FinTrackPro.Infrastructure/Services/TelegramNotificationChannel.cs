using FinTrackPro.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace FinTrackPro.Infrastructure.Services;

public class TelegramNotificationChannel : INotificationChannel
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<TelegramNotificationChannel> _logger;

    public TelegramNotificationChannel(
        ITelegramBotClient botClient,
        ILogger<TelegramNotificationChannel> logger)
    {
        _botClient = botClient;
        _logger = logger;
    }

    public async Task SendAsync(string recipient, string title, string body, CancellationToken cancellationToken = default)
    {
        var message = $"*{EscapeMarkdown(title)}*\n{EscapeMarkdown(body)}";

        await _botClient.SendMessage(
            chatId: recipient,
            text: message,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Telegram notification sent to chat {ChatId}", recipient);
    }

    private static string EscapeMarkdown(string text) =>
        text.Replace(".", @"\.")
            .Replace("-", @"\-")
            .Replace("(", @"\(")
            .Replace(")", @"\)")
            .Replace("!", @"\!");
}
