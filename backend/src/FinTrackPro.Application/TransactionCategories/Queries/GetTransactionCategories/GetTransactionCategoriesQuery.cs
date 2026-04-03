using FinTrackPro.Domain.Enums;
using MediatR;

namespace FinTrackPro.Application.TransactionCategories.Queries.GetTransactionCategories;

public record GetTransactionCategoriesQuery(TransactionType? Type = null) : IRequest<IEnumerable<TransactionCategoryDto>>;
