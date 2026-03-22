using FluentValidation;

namespace FinTrackPro.Application.Notifications.Commands.SaveNotificationPreference;

public class SaveNotificationPreferenceCommandValidator : AbstractValidator<SaveNotificationPreferenceCommand>
{
    public SaveNotificationPreferenceCommandValidator()
    {
        RuleFor(v => v.TelegramChatId).NotEmpty().WithMessage("Telegram chat ID is required.");
    }
}
