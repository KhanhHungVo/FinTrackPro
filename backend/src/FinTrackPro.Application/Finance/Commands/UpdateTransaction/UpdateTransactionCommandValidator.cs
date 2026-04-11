using FluentValidation;

namespace FinTrackPro.Application.Finance.Commands.UpdateTransaction;

public class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionCommandValidator()
    {
        RuleFor(v => v.Id).NotEmpty();
        RuleFor(v => v.Amount).GreaterThan(0);
        RuleFor(v => v.Currency).NotEmpty().MaximumLength(3);
        RuleFor(v => v.Category).NotEmpty().MaximumLength(100);
        RuleFor(v => v.Note).MaximumLength(500).When(v => v.Note != null);
    }
}
