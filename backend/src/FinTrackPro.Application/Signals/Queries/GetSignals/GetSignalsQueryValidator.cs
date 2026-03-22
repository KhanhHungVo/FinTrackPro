using FluentValidation;

namespace FinTrackPro.Application.Signals.Queries.GetSignals;

public class GetSignalsQueryValidator : AbstractValidator<GetSignalsQuery>
{
    public GetSignalsQueryValidator()
    {
        RuleFor(v => v.Count)
            .GreaterThan(0).WithMessage("Count must be greater than zero.")
            .LessThanOrEqualTo(1000).WithMessage("Count must not exceed 1000.");
    }
}
