using FinTrackPro.Application.Common.Interfaces;
using FinTrackPro.Domain.Entities;
using FinTrackPro.Domain.Exceptions;
using FinTrackPro.Domain.Repositories;
using MediatR;

namespace FinTrackPro.Application.Finance.Commands.CreateBudget;

public class CreateBudgetCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IUserRepository userRepository) : IRequestHandler<CreateBudgetCommand, Guid>
{
    public async Task<Guid> Handle(CreateBudgetCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByExternalIdAsync(
            currentUser.ExternalUserId!, cancellationToken)
            ?? throw new NotFoundException(nameof(AppUser), currentUser.ExternalUserId!);

        var budget = Budget.Create(user.Id, request.Category, request.LimitAmount, request.Month);

        context.Budgets.Add(budget);
        await context.SaveChangesAsync(cancellationToken);

        return budget.Id;
    }
}
