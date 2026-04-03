using MediatR;

namespace FinTrackPro.Application.TransactionCategories.Commands.DeleteTransactionCategory;

public record DeleteTransactionCategoryCommand(Guid Id) : IRequest;
