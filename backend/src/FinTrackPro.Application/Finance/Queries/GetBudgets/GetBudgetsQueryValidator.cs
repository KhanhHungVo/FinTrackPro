using FluentValidation;

namespace FinTrackPro.Application.Finance.Queries.GetBudgets;

public class GetBudgetsQueryValidator : AbstractValidator<GetBudgetsQuery>
{
    public GetBudgetsQueryValidator()
    {
        RuleFor(v => v.Month)
            .NotEmpty()
            .MaximumLength(7).WithMessage("Month must not exceed 7 characters.")
            .Matches(@"^\d{4}-\d{2}$")
            .WithMessage("Month must be in YYYY-MM format.");
    }
}
