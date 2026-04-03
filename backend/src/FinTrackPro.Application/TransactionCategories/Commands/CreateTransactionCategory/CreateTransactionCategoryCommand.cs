using FinTrackPro.Domain.Enums;
using MediatR;

namespace FinTrackPro.Application.TransactionCategories.Commands.CreateTransactionCategory;

public record CreateTransactionCategoryCommand(
    TransactionType Type,
    string Slug,
    string LabelEn,
    string LabelVi,
    string Icon
) : IRequest<Guid>;
