using FluentValidation;

namespace FinTrackPro.Application.Finance.Commands.CreateTransaction;

public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
{
    public CreateTransactionCommandValidator()
    {
        RuleFor(v => v.Type).IsInEnum().WithMessage("Type must be a valid transaction type.");
        RuleFor(v => v.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
        RuleFor(v => v.Currency).NotEmpty().MaximumLength(3).WithMessage("Currency is required and must be at most 3 characters.");
        RuleFor(v => v.CategoryId).NotEmpty().WithMessage("CategoryId is required.");
        RuleFor(v => v.Note)
            .MaximumLength(500).WithMessage("Note must not exceed 500 characters.")
            .Matches(@"^[^<>]*$").WithMessage("Note must not contain angle brackets (< >).")
            .When(v => !string.IsNullOrEmpty(v.Note));
        RuleFor(v => v.BudgetMonth)
            .NotEmpty()
            .MaximumLength(7).WithMessage("BudgetMonth must not exceed 7 characters.")
            .Matches(@"^\d{4}-\d{2}$")
            .WithMessage("BudgetMonth must be in YYYY-MM format.");
    }
}
