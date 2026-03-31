using FinTrackPro.Domain.Enums;
using FluentValidation;

namespace FinTrackPro.Application.Trading.Commands.UpdateTrade;

public class UpdateTradeCommandValidator : AbstractValidator<UpdateTradeCommand>
{
    public UpdateTradeCommandValidator()
    {
        RuleFor(v => v.Symbol)
            .NotEmpty().WithMessage("Symbol is required.");

        RuleFor(v => v.Symbol)
            .MaximumLength(20).WithMessage("Symbol must not exceed 20 characters.")
            .Matches(@"^[A-Z0-9]{1,10}([/\-][A-Z0-9]{1,10})?$")
            .WithMessage("Symbol must be uppercase letters and digits, with an optional / or - separator (e.g. BTCUSDT, AAPL, EUR/USD, VIC).")
            .When(v => !string.IsNullOrEmpty(v.Symbol));

        RuleFor(v => v.Direction).IsInEnum().WithMessage("Direction must be a valid trade direction.");
        RuleFor(v => v.Status).IsInEnum().WithMessage("Status must be a valid trade status.");
        RuleFor(v => v.EntryPrice).GreaterThan(0).WithMessage("Entry price must be greater than zero.");

        RuleFor(v => v.ExitPrice)
            .NotNull().WithMessage("Exit price is required for a closed trade.")
            .GreaterThan(0).WithMessage("Exit price must be greater than zero.")
            .When(v => v.Status == TradeStatus.Closed);

        RuleFor(v => v.CurrentPrice)
            .GreaterThan(0).WithMessage("Current price must be greater than zero.")
            .When(v => v.CurrentPrice.HasValue);

        RuleFor(v => v.PositionSize).GreaterThan(0).WithMessage("Position size must be greater than zero.");
        RuleFor(v => v.Fees).GreaterThanOrEqualTo(0).WithMessage("Fees cannot be negative.");
        RuleFor(v => v.Currency).NotEmpty().MaximumLength(3).WithMessage("Currency is required and must be at most 3 characters.");
        RuleFor(v => v.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.")
            .When(v => v.Notes is not null);
    }
}
