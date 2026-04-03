using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.TransactionCategories.Commands.DeleteTransactionCategory;

public class DeleteTransactionCategoryCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    ITransactionCategoryRepository categoryRepository)
    : IRequestHandler<DeleteTransactionCategoryCommand>
{
    public async Task Handle(DeleteTransactionCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.TransactionCategory), request.Id);

        if (category.UserId != currentUser.UserId)
            throw new AuthorizationException("You do not own this category.");

        category.SoftDelete();
        await context.SaveChangesAsync(cancellationToken);
    }
}
