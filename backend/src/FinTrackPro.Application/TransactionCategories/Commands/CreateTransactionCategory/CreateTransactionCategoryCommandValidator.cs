using FluentValidation;

namespace FinTrackPro.Application.TransactionCategories.Commands.CreateTransactionCategory;

public class CreateTransactionCategoryCommandValidator : AbstractValidator<CreateTransactionCategoryCommand>
{
    public CreateTransactionCategoryCommandValidator()
    {
        RuleFor(v => v.Type).IsInEnum().WithMessage("Type must be a valid transaction type.");
        RuleFor(v => v.Slug)
            .NotEmpty()
            .MaximumLength(100).WithMessage("Slug must not exceed 100 characters.")
            .Matches(@"^[a-z0-9][a-z0-9_]{1,98}$")
            .WithMessage("Slug must start with a lowercase letter or digit and contain only lowercase letters, digits, and underscores (2–99 characters).");
        RuleFor(v => v.LabelEn).NotEmpty().MaximumLength(100).WithMessage("English label is required and must be at most 100 characters.");
        RuleFor(v => v.LabelVi).NotEmpty().MaximumLength(100).WithMessage("Vietnamese label is required and must be at most 100 characters.");
        RuleFor(v => v.Icon).NotEmpty().MaximumLength(50).WithMessage("Icon is required.");
    }
}
