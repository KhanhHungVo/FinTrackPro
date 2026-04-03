using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.TransactionCategories.Commands.UpdateTransactionCategory;

public class UpdateTransactionCategoryCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    ITransactionCategoryRepository categoryRepository)
    : IRequestHandler<UpdateTransactionCategoryCommand>
{
    public async Task Handle(UpdateTransactionCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.TransactionCategory), request.Id);

        if (category.UserId != currentUser.UserId)
            throw new AuthorizationException("You do not own this category.");

        category.UpdateLabels(request.LabelEn, request.LabelVi, request.Icon);
        await context.SaveChangesAsync(cancellationToken);
    }
}
