using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Finance.Commands.DeleteBudget;

public class DeleteBudgetCommandHandler(
    IApplicationDbContext context,
    IBudgetRepository budgetRepository,
    ICurrentUserService currentUser,
    IUserRepository userRepository) : IRequestHandler<DeleteBudgetCommand>
{
    public async Task Handle(DeleteBudgetCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByExternalIdAsync(
            currentUser.ExternalUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.ExternalUserId!);

        var budget = await budgetRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Budget), request.Id);

        if (budget.UserId != user.Id)
            throw new AuthorizationException("You are not authorized to delete this budget.");

        budgetRepository.Remove(budget);
        await context.SaveChangesAsync(cancellationToken);
    }
}
