using FluentValidation;

namespace FinTrackPro.Application.Trading.Commands.CreateTrade;

public class CreateTradeCommandValidator : AbstractValidator<CreateTradeCommand>
{
    public CreateTradeCommandValidator()
    {
        RuleFor(v => v.Symbol).NotEmpty().WithMessage("Symbol is required.");
        RuleFor(v => v.Direction).IsInEnum().WithMessage("Direction must be a valid trade direction.");
        RuleFor(v => v.EntryPrice).GreaterThan(0).WithMessage("Entry price must be greater than zero.");
        RuleFor(v => v.ExitPrice).GreaterThan(0).WithMessage("Exit price must be greater than zero.");
        RuleFor(v => v.PositionSize).GreaterThan(0).WithMessage("Position size must be greater than zero.");
        RuleFor(v => v.Fees).GreaterThanOrEqualTo(0).WithMessage("Fees cannot be negative.");
    }
}
