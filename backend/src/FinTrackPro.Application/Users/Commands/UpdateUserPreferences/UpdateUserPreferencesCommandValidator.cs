using FluentValidation;

namespace FinTrackPro.Application.Users.Commands.UpdateUserPreferences;

public class UpdateUserPreferencesCommandValidator : AbstractValidator<UpdateUserPreferencesCommand>
{
    private static readonly string[] AllowedLanguages = ["en", "vi"];

    public UpdateUserPreferencesCommandValidator()
    {
        RuleFor(v => v.Language)
            .NotEmpty()
            .Must(lang => AllowedLanguages.Contains(lang))
            .WithMessage($"Language must be one of: {string.Join(", ", AllowedLanguages)}.");

        RuleFor(v => v.Currency)
            .NotEmpty()
            .MaximumLength(3)
            .WithMessage("Currency is required and must be at most 3 characters.");
    }
}
