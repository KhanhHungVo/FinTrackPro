using FinTrackPro.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace FinTrackPro.Infrastructure.Services;

public class TelegramNotificationChannel(
    ITelegramBotClient botClient,
    ILogger<TelegramNotificationChannel> logger) : INotificationChannel
{
    public async Task SendAsync(string recipient, string title, string body, CancellationToken cancellationToken = default)
    {
        var message = $"*{EscapeMarkdown(title)}*\n{EscapeMarkdown(body)}";

        await botClient.SendMessage(
            chatId: recipient,
            text: message,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.MarkdownV2,
            cancellationToken: cancellationToken);

        logger.LogInformation("Telegram notification sent to chat {ChatId}", recipient);
    }

    private static string EscapeMarkdown(string text) =>
        text.Replace(".", @"\.")
            .Replace("-", @"\-")
            .Replace("(", @"\(")
            .Replace(")", @"\)")
            .Replace("!", @"\!");
}
