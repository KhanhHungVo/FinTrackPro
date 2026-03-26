using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Finance.Commands.UpdateBudget;

public class UpdateBudgetCommandHandler(
    IApplicationDbContext context,
    IBudgetRepository budgetRepository,
    ICurrentUser currentUser,
    IUserRepository userRepository) : IRequestHandler<UpdateBudgetCommand>
{
    public async Task Handle(UpdateBudgetCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        var budget = await budgetRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Budget), request.Id);

        if (budget.UserId != user.Id)
            throw new AuthorizationException("You are not authorized to update this budget.");

        budget.UpdateLimit(request.LimitAmount);
        await context.SaveChangesAsync(cancellationToken);
    }
}
