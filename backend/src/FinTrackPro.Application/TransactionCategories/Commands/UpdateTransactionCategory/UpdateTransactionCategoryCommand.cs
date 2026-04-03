using MediatR;

namespace FinTrackPro.Application.TransactionCategories.Commands.UpdateTransactionCategory;

public record UpdateTransactionCategoryCommand(
    Guid Id,
    string LabelEn,
    string LabelVi,
    string Icon
) : IRequest;
