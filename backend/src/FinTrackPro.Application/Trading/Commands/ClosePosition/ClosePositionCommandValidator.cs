using FluentValidation;

namespace FinTrackPro.Application.Trading.Commands.ClosePosition;

public class ClosePositionCommandValidator : AbstractValidator<ClosePositionCommand>
{
    public ClosePositionCommandValidator()
    {
        RuleFor(v => v.ExitPrice)
            .GreaterThan(0).WithMessage("Exit price is required to close a position.");
        RuleFor(v => v.Fees)
            .GreaterThanOrEqualTo(0).WithMessage("Fees cannot be negative.");
    }
}
