using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Finance.Commands.CreateBudget;

public class CreateBudgetCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser,
    IUserRepository userRepository,
    IBudgetRepository budgetRepository) : IRequestHandler<CreateBudgetCommand, Guid>
{
    public async Task<Guid> Handle(CreateBudgetCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.UserId);

        if (await budgetRepository.ExistsAsync(user.Id, request.Category, request.Month, cancellationToken))
            throw new ConflictException($"A budget for category '{request.Category}' in {request.Month} already exists.");

        var budget = Budget.Create(user.Id, request.Category, request.LimitAmount, request.Month);

        context.Budgets.Add(budget);
        await context.SaveChangesAsync(cancellationToken);

        return budget.Id;
    }
}
