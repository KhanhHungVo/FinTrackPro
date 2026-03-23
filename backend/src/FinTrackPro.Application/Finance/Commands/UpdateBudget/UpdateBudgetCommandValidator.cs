using FluentValidation;

namespace FinTrackPro.Application.Finance.Commands.UpdateBudget;

public class UpdateBudgetCommandValidator : AbstractValidator<UpdateBudgetCommand>
{
    public UpdateBudgetCommandValidator()
    {
        RuleFor(v => v.Id).NotEmpty().WithMessage("Budget ID is required.");
        RuleFor(v => v.LimitAmount).GreaterThan(0).WithMessage("Limit amount must be greater than zero.");
    }
}
