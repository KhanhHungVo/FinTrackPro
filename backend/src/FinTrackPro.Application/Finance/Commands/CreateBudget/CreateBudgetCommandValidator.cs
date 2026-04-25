using FluentValidation;

namespace FinTrackPro.Application.Finance.Commands.CreateBudget;

public class CreateBudgetCommandValidator : AbstractValidator<CreateBudgetCommand>
{
    public CreateBudgetCommandValidator()
    {
        RuleFor(v => v.Category)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(100).WithMessage("Category must not exceed 100 characters.")
            .Matches(@"^[\p{L}\p{N}\s\-\/\.\&\(\)_]+$")
            .WithMessage("Category must contain only letters, numbers, spaces, and common punctuation (- / . & ( ) _).");
        RuleFor(v => v.LimitAmount).GreaterThan(0).WithMessage("Limit amount must be greater than zero.");
        RuleFor(v => v.Currency).NotEmpty().MaximumLength(3).WithMessage("Currency is required and must be at most 3 characters.");
        RuleFor(v => v.Month)
            .NotEmpty()
            .MaximumLength(7).WithMessage("Month must not exceed 7 characters.")
            .Matches(@"^\d{4}-\d{2}$")
            .WithMessage("Month must be in YYYY-MM format.");
    }
}
