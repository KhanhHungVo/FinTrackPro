using FluentValidation;

namespace FinTrackPro.Application.Finance.Commands.CreateBudget;

public class CreateBudgetCommandValidator : AbstractValidator<CreateBudgetCommand>
{
    public CreateBudgetCommandValidator()
    {
        RuleFor(v => v.Category).NotEmpty().WithMessage("Category is required.");
        RuleFor(v => v.LimitAmount).GreaterThan(0).WithMessage("Limit amount must be greater than zero.");
        RuleFor(v => v.Month)
            .NotEmpty()
            .Matches(@"^\d{4}-\d{2}$")
            .WithMessage("Month must be in YYYY-MM format.");
    }
}
