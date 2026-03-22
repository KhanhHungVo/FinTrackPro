using FluentValidation;

namespace FinTrackPro.Application.Trading.Commands.AddWatchedSymbol;

public class AddWatchedSymbolCommandValidator : AbstractValidator<AddWatchedSymbolCommand>
{
    public AddWatchedSymbolCommandValidator()
    {
        RuleFor(v => v.Symbol).NotEmpty().WithMessage("Symbol is required.");
    }
}
