using FluentValidation;

namespace FinTrackPro.Application.Trading.Commands.AddWatchedSymbol;

public class AddWatchedSymbolCommandValidator : AbstractValidator<AddWatchedSymbolCommand>
{
    public AddWatchedSymbolCommandValidator()
    {
        RuleFor(v => v.Symbol)
            .NotEmpty().WithMessage("Symbol is required.");

        RuleFor(v => v.Symbol)
            .MaximumLength(20).WithMessage("Symbol must not exceed 20 characters.")
            .Matches(@"^[A-Z0-9]{1,10}([/\-][A-Z0-9]{1,10})?$")
            .WithMessage("Symbol must be uppercase letters and digits, with an optional / or - separator (e.g. BTCUSDT, AAPL, EUR/USD, VIC).")
            .When(v => !string.IsNullOrEmpty(v.Symbol));
    }
}
