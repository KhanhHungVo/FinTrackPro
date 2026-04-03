using FluentValidation;

namespace FinTrackPro.Application.TransactionCategories.Commands.UpdateTransactionCategory;

public class UpdateTransactionCategoryCommandValidator : AbstractValidator<UpdateTransactionCategoryCommand>
{
    public UpdateTransactionCategoryCommandValidator()
    {
        RuleFor(v => v.Id).NotEmpty();
        RuleFor(v => v.LabelEn).NotEmpty().MaximumLength(100);
        RuleFor(v => v.LabelVi).NotEmpty().MaximumLength(100);
        RuleFor(v => v.Icon).NotEmpty().MaximumLength(50);
    }
}
