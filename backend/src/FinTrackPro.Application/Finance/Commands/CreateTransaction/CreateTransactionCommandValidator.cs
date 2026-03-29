using FluentValidation;

namespace FinTrackPro.Application.Finance.Commands.CreateTransaction;

public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(v => v.Type).IsInEnum().WithMessage("Type must be a valid transaction type.");
        RuleFor(v => v.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
        RuleFor(v => v.Currency).NotEmpty().MaximumLength(3).WithMessage("Currency is required and must be at most 3 characters.");
        RuleFor(v => v.Category).NotEmpty().WithMessage("Category is required.");
        RuleFor(v => v.BudgetMonth)
            .NotEmpty()
            .Matches(@"^\d{4}-\d{2}$")
            .WithMessage("BudgetMonth must be in YYYY-MM format.");
    }
}
